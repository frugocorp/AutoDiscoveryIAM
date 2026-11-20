#!/usr/bin/env node
// Escanea content-parser-md/ (via @ngx-docs-markdown-kit/parser-md/manifest) y genera una constante
// TS con nav + headings + meta de cada pagina -- corre ANTES de "ng build"/"ng serve" (ver
// package.json), y en modo watch (--watch) EN PARALELO a "ng serve" durante desarrollo (ver
// PARSER_MD_PATHS/rutas en frugocorp_modules/parser-md/config/paths.config.ts). La logica de
// ESCANEO es de parser-md (reusable por cualquier consumidor, ej. parser-md-seo); lo que sigue aca
// es 100% especifico de ESTE sitio: como se combinan title/keywords/description de una pagina con
// los de su cadena de grupos, y como se generan las Cards de "___content_dir.md" (la pagina propia
// de cada carpeta -- evita rutas huerfanas/vacias, ver README).
import { mkdirSync, readdirSync, readFileSync, rmSync, statSync, writeFileSync } from 'node:fs';
import { resolve, dirname, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { buildContentManifest } from '@ngx-docs-markdown-kit/parser-md/manifest';
import { PARSER_MD_PATHS } from '../config/paths.config.ts';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const CONTENT_ROOT = resolve(ROOT, PARSER_MD_PATHS.contentDir);
const OUTPUT_PATH = resolve(ROOT, PARSER_MD_PATHS.manifestOutput);
const CONTENT_DIR_FILENAME = '___content_dir.md';

function splitKeywords(raw) {
  return raw
    ? raw
        .split(',')
        .map((keyword) => keyword.trim())
        .filter(Boolean)
    : [];
}

// Del mas cercano (ultimo segmento de la cadena) al mas lejano -- el primero que declare un campo
// dado es el mas especifico. Sirve tanto para la cadena de una PAGINA (sus grupos ancestros) como
// para la de un GRUPO en si (sus grupos ancestros, sin incluirse a si mismo).
function ancestorChain(groupPath, groupsByPath) {
  const chain = [];
  for (let depth = groupPath.length; depth > 0; depth--) {
    const group = groupsByPath.get(groupPath.slice(0, depth).join('/'));
    if (group) {
      chain.push(group);
    }
  }
  return chain;
}

// Titulo mas cercano en la cadena, SIN concatenar -- concatenar todos los niveles de una jerarquia
// profunda repite palabras (ej. 3 niveles de "Animales") y es contraproducente para SEO. Sirve para
// paginas Y para grupos por igual: "self" es el frontmatter propio (de la pagina, o del meta.md del
// grupo), "chain" es la cadena de ANCESTROS (sin incluirse a si mismo).
function nearestTitle(selfFrontmatter, chain, fallbackName) {
  if (selfFrontmatter['title']) {
    return selfFrontmatter['title'];
  }
  for (const ancestor of chain) {
    if (ancestor.frontmatter['title']) {
      return ancestor.frontmatter['title'];
    }
  }
  return fallbackName;
}

function nearestDescription(selfFrontmatter, chain) {
  if (selfFrontmatter['description']) {
    return selfFrontmatter['description'];
  }
  for (const ancestor of chain) {
    if (ancestor.frontmatter['description']) {
      return ancestor.frontmatter['description'];
    }
  }
  return undefined;
}

// A diferencia del titulo, las keywords SI se combinan -- acumular las de categorias mas generales
// es una practica de SEO real y valida (no se lee, no se repite visualmente).
function unionKeywords(selfFrontmatter, chain) {
  const merged = new Set(splitKeywords(selfFrontmatter['keywords']));
  for (const ancestor of chain) {
    for (const keyword of splitKeywords(ancestor.frontmatter['keywords'])) {
      merged.add(keyword);
    }
  }
  return [...merged];
}

function fullPath(groupPath, slug) {
  return [...groupPath, slug];
}

function hrefFor(pathSegments) {
  return `/${pathSegments.join('/')}`;
}

// "nav: false" (paginas fuera de cualquier barra, ej. legales) sigue siendo un frontmatter valido
// SOLO para paginas reales -- un grupo (su "___content_dir.md" generado) siempre es navegable, no
// tiene sentido un grupo invisible con paginas visibles adentro.
function sectionFor(pathSegments, frontmatter) {
  if (frontmatter['nav'] === 'false') {
    return null;
  }
  return pathSegments[0] ?? null;
}

function buildRealPageEntry(page, groupsByPath) {
  const chain = ancestorChain(page.group, groupsByPath);
  const description = nearestDescription(page.frontmatter, chain);
  const keywords = unionKeywords(page.frontmatter, chain);
  const path = fullPath(page.group, page.slug);
  const meta = { ...page.frontmatter };
  if (description) {
    meta.description = description;
  }
  if (keywords.length > 0) {
    meta.keywords = keywords.join(', ');
  }

  // Orden de prioridad: title de frontmatter -> H1 propio de la pagina (una pagina, a diferencia
  // de un grupo, siempre tiene contenido propio) -> titulo mas cercano en la cadena de grupos ->
  // el slug como ultimo recurso.
  const h1 = page.headings.find((heading) => heading.level === 1);
  const title = page.frontmatter['title'] ?? h1?.text ?? nearestTitle({}, chain, page.slug);

  return {
    slug: page.slug,
    group: page.group,
    section: sectionFor(path, page.frontmatter),
    order: page.order,
    title,
    icon: null,
    contentFile: page.sourcePath,
    headings: page.headings,
    meta,
    lastmod: page.lastmod,
  };
}

// La pagina PROPIA de un grupo (su "___content_dir.md", ver generateContentDirFiles) -- mismo
// tratamiento que una pagina real (title/description/keywords con la misma cadena de fallback,
// section/href con la misma formula), la diferencia es de donde sale "contentFile" y que sus
// "headings" quedan vacios (Cards generadas, sin jerarquia real de encabezados que mostrar en la
// barra 3).
function buildGroupIndexEntry(group, groupsByPath) {
  const parentPath = group.path.slice(0, -1);
  const slug = group.path[group.path.length - 1];
  const chain = ancestorChain(parentPath, groupsByPath);
  const description = nearestDescription(group.frontmatter, chain);
  const keywords = unionKeywords(group.frontmatter, chain);
  const path = fullPath(parentPath, slug);
  const meta = { ...group.frontmatter };
  if (description) {
    meta.description = description;
  }
  if (keywords.length > 0) {
    meta.keywords = keywords.join(', ');
  }

  return {
    slug,
    group: parentPath,
    section: sectionFor(path, group.frontmatter),
    order: group.order,
    title: nearestTitle(group.frontmatter, chain, slug),
    icon: group.frontmatter['icon'] ?? null,
    contentFile: `${group.sourcePath}/${CONTENT_DIR_FILENAME}`,
    headings: [],
    meta,
    lastmod: new Date().toISOString().slice(0, 10),
  };
}

// Cards de un grupo = sus hijos DIRECTOS (subgrupos + paginas reales), en su mismo orden entre
// hermanos -- reusa el titulo/descripcion YA resueltos de cada entrada final (misma logica de
// fallback que cualquier otra pagina, ninguna duplicada aca).
function cardsFenceFor(groupPath, entries) {
  const ownDepth = groupPath.length;
  const children = entries
    .filter((entry) => entry.group.length === ownDepth && entry.group.join('/') === groupPath.join('/'))
    .sort((a, b) => a.order - b.order);
  const lines = children.map((entry) => `${hrefFor(fullPath(entry.group, entry.slug))}|${entry.title}|${entry.meta.description ?? ''}`);
  return `\`\`\`cards\n${lines.join('\n')}\n\`\`\`\n`;
}

function removeGeneratedContentDirFiles(dir) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = resolve(dir, entry.name);
    if (entry.isDirectory()) {
      removeGeneratedContentDirFiles(path);
    } else if (entry.name === CONTENT_DIR_FILENAME) {
      rmSync(path, { force: true });
    }
  }
}

// Escribe SOLO si el contenido cambio -- en modo watch, "run()" se re-ejecuta en cada poll (ver
// abajo) aunque nada haya cambiado; escribir sin condicion ahi haria que "ng serve" (que tambien
// vigila content-parser-md/) detecte una modificacion falsa en CADA poll y dispare un reload del
// navegador sin sentido.
function writeIfChanged(path, content) {
  try {
    if (readFileSync(path, 'utf-8') === content) {
      return;
    }
  } catch {
    // el archivo no existe todavia -- se escribe igual, abajo.
  }
  writeFileSync(path, content);
}

function writeContentDirFiles(manifest, entries) {
  for (const group of manifest.groups) {
    const dir = resolve(CONTENT_ROOT, group.sourcePath);
    writeIfChanged(resolve(dir, CONTENT_DIR_FILENAME), cardsFenceFor(group.path, entries));
  }
}

function renderManifest(pages) {
  const sortedPages = [...pages].sort((a, b) => hrefFor(fullPath(a.group, a.slug)).localeCompare(hrefFor(fullPath(b.group, b.slug))));
  return `// GENERADO por frugocorp_modules/parser-md/scripts/generate-content-manifest.mjs -- no
// editar a mano, se sobreescribe en cada build/serve.
import type { DocPageHeading } from '@ngx-docs-markdown-kit/parser-md';

export interface GeneratedHeading extends DocPageHeading {
  readonly parentId: string | null;
}

export interface GeneratedPage {
  readonly slug: string;
  readonly group: readonly string[];
  /** null = pagina fuera de cualquier seccion (frontmatter "nav: false") -- se sigue prerenderizando/sitemapeando, pero no aparece en las barras 1/2. */
  readonly section: string | null;
  readonly order: number;
  /** Nombre de icono de Material Symbols -- solo lo declaran los grupos de nivel superior (su propio "___meta.md"), null en cualquier otro caso. */
  readonly icon: string | null;
  /** Ruta real en disco (relativa a content-parser-md/) del .md a pedirle a ContentService -- puede ser un archivo real o el "___content_dir.md" autogenerado de un grupo. */
  readonly contentFile: string;
  readonly title: string;
  readonly headings: readonly GeneratedHeading[];
  readonly meta: Readonly<Record<string, string>>;
  readonly lastmod: string;
}

export const CONTENT_MANIFEST: readonly GeneratedPage[] = ${JSON.stringify(sortedPages, null, 2)};
`;
}

function run() {
  removeGeneratedContentDirFiles(CONTENT_ROOT);
  const manifest = buildContentManifest(CONTENT_ROOT);
  const groupsByPath = new Map(manifest.groups.map((group) => [group.path.join('/'), group]));

  const realPages = manifest.pages.map((page) => buildRealPageEntry(page, groupsByPath));
  const groupIndexPages = manifest.groups.map((group) => buildGroupIndexEntry(group, groupsByPath));
  const allPages = [...realPages, ...groupIndexPages];

  const byPath = new Map();
  for (const entry of allPages) {
    const key = hrefFor(fullPath(entry.group, entry.slug));
    const existing = byPath.get(key) ?? [];
    existing.push(entry);
    byPath.set(key, existing);
  }
  const collisions = [...byPath.entries()].filter(([, entries]) => entries.length > 1).map(([path]) => path);
  if (collisions.length > 0) {
    throw new Error(`[generate-content-manifest] ruta(s) duplicada(s) -- una carpeta y un archivo (o 2 archivos) resuelven a la misma URL: ${collisions.join(', ')}`);
  }

  writeContentDirFiles(manifest, allPages);
  mkdirSync(dirname(OUTPUT_PATH), { recursive: true });
  writeIfChanged(OUTPUT_PATH, renderManifest(allPages));
  console.log(`[generate-content-manifest] ${allPages.length} paginas (${realPages.length} reales + ${groupIndexPages.length} de grupo) -> ${relative(ROOT, OUTPUT_PATH)}`);
}

// Firma barata de "algo cambio" -- mtime+tamaño de cada .md real (sin ___content_dir.md, que ES la
// salida). Comparar esto en vez de escuchar eventos de filesystem es DELIBERADO: "fs.watch" nativo
// no es confiable sobre el mismo bind-mount Windows -> WSL2 -> contenedor que ya obliga a "ng
// serve" a usar "--poll" (ver dev-entrypoint.sh) -- confirmado en la practica: un cambio real no
// disparaba ningun evento. Sondear cada 1000ms es el mismo mecanismo, mismo intervalo, que ya se
// sabe que funciona en este entorno.
function contentSignature(dir) {
  let signature = '';
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = resolve(dir, entry.name);
    if (entry.isDirectory()) {
      signature += contentSignature(path);
    } else if (entry.name.endsWith('.md') && entry.name !== CONTENT_DIR_FILENAME) {
      const stats = statSync(path);
      signature += `${path}:${stats.mtimeMs}:${stats.size};`;
    }
  }
  return signature;
}

if (process.argv.includes('--watch')) {
  run();
  console.log('[generate-content-manifest] modo watch (poll 1000ms) -- observando cambios en', relative(ROOT, CONTENT_ROOT));
  let lastSignature = contentSignature(CONTENT_ROOT);
  setInterval(() => {
    const signature = contentSignature(CONTENT_ROOT);
    if (signature === lastSignature) {
      return;
    }
    lastSignature = signature;
    try {
      run();
    } catch (error) {
      console.error('[generate-content-manifest]', error.message);
    }
  }, 1000);
} else {
  run();
}
