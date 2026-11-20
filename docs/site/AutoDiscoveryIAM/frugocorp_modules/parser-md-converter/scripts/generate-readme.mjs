#!/usr/bin/env node
// Genera README.md multi-flavor (--github/--npmjs/--nuget, todos por default) a partir del
// manifiesto de contenido YA GENERADO (nunca vuelve a escanear el filesystem por su cuenta, ver
// generate-content-manifest.mjs) -- se corre a mano cuando se quiere publicar (no forma parte de
// "npm run build"/"npm start", a diferencia de sitemap/manifiesto). Documentacion completa
// (marcadores, [readme_ignored]/[readme_flavors], badges, "levels"/"typeReadme"/"siteUrl") en
// @ngx-docs-markdown-kit/parser-md-converter.
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve, dirname, join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseFrontmatter } from '@ngx-docs-markdown-kit/parser-md';
import {
  assetPrefixFor,
  buildLanguageStub,
  ensureIndexBackLink,
  extractHeaderAndBody,
  generateFlavorDocument,
  githubAnchor,
  markersFor,
  pagesForReadme,
  rewriteAssetPaths,
  spliceFullBlock,
  spliceGranular,
  spliceIndexEntry,
  writeWithSizeGuard,
} from '@ngx-docs-markdown-kit/parser-md-converter';
import { CONTENT_MANIFEST } from '../../create-ngx-docs-site/core.ts';
import { PARSER_MD_PATHS } from '../../parser-md/config/paths.config.ts';
import { SITE_VARIABLE_DEFAULTS } from '../../create-ngx-docs-site/services/site-variables/site-variable-defaults.config.ts';
import { CONVERTER_CONFIG } from '../config/converter.config.ts';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const CONTENT_ROOT = resolve(ROOT, PARSER_MD_PATHS.contentDir);
const GENERATED_DIR = resolve(ROOT, 'frugocorp_modules/parser-md-converter/generated');
const PROJECT_NAME = CONVERTER_CONFIG.title.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '') || 'site';

function requestedFlavors() {
  const args = process.argv.slice(2);
  const requested = args.filter((arg) => arg.startsWith('--')).map((arg) => arg.slice(2));
  const known = ['github', 'npmjs', 'nuget'];
  const flavors = requested.filter((flavor) => known.includes(flavor));
  return flavors.length > 0 ? flavors : known;
}

const GROUP_CONTENT_FILENAME = '___content_dir.md';

// `spliceFullBlock` ya antepone su propio par doc_start/doc_end (ver markers.ts) -- el contenido
// que recibe tiene que ser SOLO lo de adentro (header+body con sus propios sub-marcadores), nunca
// el resultado de `wrapFullDocument` (que trae su PROPIO doc_start/doc_end) o el marcador queda
// duplicado/anidado.
function innerBlock(markers, header, body) {
  return [markers.headerStart, header.trim(), markers.headerEnd, markers.bodyStart, body.trim(), markers.bodyEnd].join('\n');
}

function loadPages() {
  return CONTENT_MANIFEST.map((page) => {
    const raw = readFileSync(resolve(CONTENT_ROOT, page.contentFile), 'utf-8');
    const { body } = parseFrontmatter(raw);
    const isGroup = page.contentFile.endsWith(GROUP_CONTENT_FILENAME);
    return { slug: page.slug, group: page.group, title: page.title, order: page.order, meta: page.meta, body, isGroup };
  });
}

function writeLanguageStubs() {
  if (CONVERTER_CONFIG.languages.length <= 1) {
    return;
  }
  const [primary, ...rest] = CONVERTER_CONFIG.languages;
  const siteLevel = CONVERTER_CONFIG.levels[0];
  for (const language of rest) {
    // "file" es relativo al nivel 0 (el propio sitio, donde vive el README real), no a ROOT (raiz
    // del SITIO en si) -- coinciden para los sitios normales (levels[0].path === '.') pero no
    // cuando ese nivel apunta afuera del sitio (ver el sitio de ngx-docs-markdown-kit,
    // levels[0].path: '../../..').
    const stubPath = resolve(ROOT, siteLevel.path, language.file);
    if (existsSync(stubPath)) {
      continue;
    }
    writeFileSync(stubPath, buildLanguageStub(CONVERTER_CONFIG.title, primary.label, primary.file));
    console.log(`[generate-readme] ${language.file} creado (placeholder "en construccion") -- traduccion real pendiente, a mano.`);
  }
}

// "imageBaseUrl" (para absolutizar imagenes en npmjs/nuget, y de fallback para links internos a
// paginas excluidas via readme_flavors) prioriza "CONVERTER_CONFIG.siteUrl" EXPLICITO -- pensado
// para proyectos sin ningun package.json en los niveles configurados (ej. una libreria .NET/NuGet,
// ver master-plan). Sin eso, cae al mecanismo viejo: el PRIMER package.json entre los niveles que
// declare "homepage" (typico: el del paquete de npm, projects/<paquete>/package.json).
// "packageDir" (para el badge de version/npm, ver generate-document.ts) es INDEPENDIENTE de
// "siteUrl" -- SIEMPRE sale de esa misma busqueda por "homepage" (ese package.json es el que tiene
// la version REAL publicada, a diferencia del package.json del sitio -- levels[0] -- que queda
// fijo en "0.0.0" para siempre, bug real encontrado por el usuario -- ver master-plan). Ningun
// nivel declara "homepage" -> "packageDir" queda null (ver CONVERTER_CONFIG.autoBadges para
// apagar el badge automatico en ese caso).
function resolvePackageInfo() {
  let packageDir = null;
  let homepage = null;
  for (const level of CONVERTER_CONFIG.levels) {
    const dir = resolve(ROOT, level.path);
    const pkgJsonPath = resolve(dir, 'package.json');
    if (!existsSync(pkgJsonPath)) {
      continue;
    }
    const pkgHomepage = JSON.parse(readFileSync(pkgJsonPath, 'utf-8')).homepage;
    if (pkgHomepage) {
      packageDir = dir;
      homepage = pkgHomepage;
      break;
    }
  }
  return { imageBaseUrl: CONVERTER_CONFIG.siteUrl ?? homepage ?? null, packageDir };
}

function run() {
  mkdirSync(GENERATED_DIR, { recursive: true });
  const pages = pagesForReadme(loadPages());
  const flavors = requestedFlavors();
  const { imageBaseUrl, packageDir } = resolvePackageInfo();

  const documents = {};
  for (const flavor of flavors) {
    // Ultimo argumento: los defaults de "Mis Variables" del propio sitio -- un README publicado
    // (GitHub/npm/NuGet) nunca tiene el panel interactivo que resuelve "(--CLAVE--)" en el
    // navegador, asi que el converter reemplaza cualquier marcador por su valor default (ver
    // resolveSiteVariables en @ngx-docs-markdown-kit/parser-md-converter). Una clave sin default
    // declarado queda intacta, nunca un string vacio silencioso.
    const document = generateFlavorDocument(PROJECT_NAME, CONVERTER_CONFIG, ROOT, pages, flavor, imageBaseUrl, packageDir, SITE_VARIABLE_DEFAULTS);
    documents[flavor] = document;
    const outputPath = join(GENERATED_DIR, `README.${flavor}.md`);
    const maxBytes = flavor === 'nuget' ? CONVERTER_CONFIG.nuget.maxBytes : null;
    writeWithSizeGuard(outputPath, document, maxBytes);
    console.log(`[generate-readme] generated/README.${flavor}.md escrito (${document.length} bytes).`);

    // "nuget.outputPath" -- ademas de la copia de siempre en generated/, escribe el documento en la
    // ruta real que empaqueta el .csproj (ej. "../../../docs/nuget/README.md") -- SIN los
    // comentarios HTML de marcadores (doc_start/doc_header_start/...): ese archivo es terminal,
    // nadie vuelve a hacerle splice (a diferencia de "levels", nunca se preserva nada afuera de un
    // marcador ahi -- el equipo nunca lo edita a mano, ver master-plan/reparto de responsabilidades).
    if (flavor === 'nuget' && CONVERTER_CONFIG.nuget.outputPath) {
      const packagedPath = resolve(ROOT, CONVERTER_CONFIG.nuget.outputPath);
      const extracted = extractHeaderAndBody(document, markersFor(PROJECT_NAME));
      if (extracted) {
        const packaged = `${extracted.header.trim()}\n\n${extracted.body.trim()}\n`;
        mkdirSync(dirname(packagedPath), { recursive: true });
        writeWithSizeGuard(packagedPath, packaged, maxBytes);
        console.log(`[generate-readme] ${relative(ROOT, packagedPath)} escrito (${packaged.length} bytes) -- archivo real que empaqueta el .csproj.`);
      } else {
        console.warn('[generate-readme] nuget.outputPath configurado, pero no se pudo extraer header/body del documento generado -- se omite.');
      }
    }
  }

  if (!flavors.includes('github')) {
    return;
  }

  const markers = markersFor(PROJECT_NAME);
  const headerAndBody = extractHeaderAndBody(documents.github, markers);
  if (!headerAndBody) {
    console.warn('[generate-readme] no se pudo leer header/body del documento generado -- se omite el splice.');
    return;
  }

  // Pasada 1 -- construye TODO lo que vive FUERA de los marcadores, en cascada, un nivel a la vez,
  // siempre en flavor "github". Nivel 0 (el sitio): splice GRANULAR con el contenido recien
  // generado. Nivel 1..N-1 (lo que este configurado despues -- tipicamente el paquete de npm,
  // despues el repo): splice FULL-BLOCK, copiando el bloque que quedo escrito en el nivel ANTERIOR.
  // Alcance acotado a proposito: lo que un equipo agregue a mano AFUERA de los marcadores en un
  // nivel se preserva EN ESE nivel, pero no se combina ni sube a los niveles de arriba.
  let previousPath = null;
  let previousPrefix = '';

  for (const [index, level] of CONVERTER_CONFIG.levels.entries()) {
    const destPath = resolve(ROOT, level.path, level.outputPath);
    if (destPath === previousPath) {
      continue;
    }

    const prefix = assetPrefixFor(dirname(destPath), ROOT);
    const source = index === 0 ? headerAndBody : extractHeaderAndBody(readFileSync(previousPath, 'utf-8'), markers);
    if (!source) {
      console.warn(`[generate-readme] ${previousPath} todavia no tiene el bloque de marcadores -- se omite ${destPath}.`);
      break;
    }
    const header = rewriteAssetPaths(source.header, previousPrefix, prefix);
    const body = rewriteAssetPaths(source.body, previousPrefix, prefix);

    const existing = existsSync(destPath) ? readFileSync(destPath, 'utf-8') : '';
    const next = index === 0 ? spliceGranular(existing, markers, header, body) : spliceFullBlock(existing, markers, innerBlock(markers, header, body));
    writeWithSizeGuard(destPath, next, level.maxBytes);
    console.log(`[generate-readme] ${destPath} actualizado (nivel ${index}, ${index === 0 ? 'granular' : 'bloque completo'}).`);

    previousPath = destPath;
    previousPrefix = prefix;
  }

  // El indice cross-proyecto (spliceIndexEntry) es un mecanismo APARTE, sin relacion con el loop de
  // arriba -- el listado automatico de TODOS los proyectos de un monorepo (indice_auto:start/end).
  // Se aplica en el ULTIMO nivel configurado (el mas externo -- mismo criterio que "el repo si esta
  // habilitado, si no el sitio" de antes de esta ronda), una sola vez: es no-op si ese nivel no
  // tiene el contenedor bootstrapeado.
  const lastLevel = CONVERTER_CONFIG.levels.at(-1);
  const lastPath = resolve(ROOT, lastLevel.path, lastLevel.outputPath);
  if (existsSync(lastPath)) {
    const anchor = githubAnchor(CONVERTER_CONFIG.title, new Map());
    const homePage = pages.find((page) => page.slug === 'home' && page.group.length === 0);
    const description = homePage?.meta['description'] ?? '';
    const entryLine = `- [${CONVERTER_CONFIG.title}](#${anchor})${description ? ` -- ${description}` : ''}`;
    const existingIndexDoc = readFileSync(lastPath, 'utf-8');
    const nextIndexDoc = spliceIndexEntry(existingIndexDoc, PROJECT_NAME, entryLine);
    if (nextIndexDoc !== existingIndexDoc) {
      writeFileSync(lastPath, nextIndexDoc);
      console.log(`[generate-readme] indice de ${lastPath} actualizado.`);
    }
  }

  // Pasada 2 -- decide QUE CONTENIDO va DENTRO de los marcadores de cada nivel, sin importar su
  // posicion. Por cada nivel, busca su propio package.json (el unico archivo que CUALQUIER nivel
  // garantiza tener) y lee "parser-md-converter.typeReadme". Si pide "npmjs"/"nuget", vuelve a
  // hacer el MISMO tipo de splice que ese nivel ya usa en la Pasada 1, pero con el header/body de
  // ESE flavor -- nunca un reemplazo total del archivo. La Pasada 2 no vuelve a mirar (ni le
  // importa) lo que hay fuera de los marcadores -- responsabilidad del equipo dev, no de este script.
  for (const [index, level] of CONVERTER_CONFIG.levels.entries()) {
    const destPath = resolve(ROOT, level.path, level.outputPath);
    const pkgJsonPath = join(dirname(destPath), 'package.json');
    if (!existsSync(pkgJsonPath)) {
      continue;
    }
    const typeReadme = JSON.parse(readFileSync(pkgJsonPath, 'utf-8'))['parser-md-converter']?.typeReadme;
    if (!typeReadme || typeReadme === 'github') {
      continue;
    }
    if (!documents[typeReadme]) {
      console.warn(`[generate-readme] ${destPath}: pide "${typeReadme}" pero ese flavor no se genero (correr sin --github, o agregar --${typeReadme}).`);
      continue;
    }
    const { header, body } = extractHeaderAndBody(documents[typeReadme], markers);
    const maxBytes = typeReadme === 'nuget' ? CONVERTER_CONFIG.nuget.maxBytes : level.maxBytes;
    const existing = existsSync(destPath) ? readFileSync(destPath, 'utf-8') : '';
    const next = index === 0 ? spliceGranular(existing, markers, header, body) : spliceFullBlock(existing, markers, innerBlock(markers, header, body));
    writeWithSizeGuard(destPath, next, maxBytes);
    console.log(`[generate-readme] ${destPath}: header/body reemplazados con el flavor "${typeReadme}" (via package.json).`);
  }

  // Link "volver al indice general" debajo del titulo de ESTE proyecto -- solo tiene sentido (y
  // solo hace algo) en el archivo que ademas trae el contenedor indice_auto (el README combinado de
  // varios proyectos): no-op automatico en site/paquete (documentos de 1 solo proyecto). Va DESPUES
  // de las 2 pasadas de arriba porque el header se reescribe completo en cada una -- insertarlo
  // antes se perderia.
  if (existsSync(lastPath)) {
    const generalIndexAnchor = githubAnchor('Índice General', new Map());
    const withBackLink = ensureIndexBackLink(readFileSync(lastPath, 'utf-8'), markers, generalIndexAnchor, 'Volver al Índice General');
    writeFileSync(lastPath, withBackLink);
  }

  writeLanguageStubs();
}

run();
