#!/usr/bin/env node
// Valida, a nivel SITIO (todos los .md juntos, no uno por uno), que ninguna variable
// "(--CLAVE--)" se use a la vez SUELTA y como grupo de casing "(--CLAVE:casing--)" -- ese es el
// unico caso realmente ambiguo (¿el lector deberia ver 1 campo o 4 variantes derivadas?). Corre
// ANTES de "ng build" (ver package.json) y frena el build con un error real y accionable si
// encuentra un conflicto -- mismo criterio que ya usa este CLI para un "[slug]" invalido o
// faltante: nunca un placeholder roto en silencio en produccion.
//
// Logica de escaneo/agrupamiento DUPLICADA a proposito (no importada) de
// frugocorp_modules/create-ngx-docs-site/services/site-variables/site-variables.ts -- ese archivo
// usa imports "./case-variants.js" (correcto para el build de Angular, que SI resuelve ".js" a
// ".ts"), pero un script plano corrido con "node" no hace esa redireccion (confirmado
// empiricamente) y fallaria al cargarlo. Si cambias la sintaxis/regex alla, cambiala tambien aca.
import { mkdirSync, readFileSync, readdirSync, statSync, writeFileSync } from 'node:fs';
import { dirname, extname, join, relative, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { PARSER_MD_PATHS } from '../../parser-md/config/paths.config.ts';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const CONTENT_DIR = resolve(ROOT, PARSER_MD_PATHS.contentDir);
const GENERATED_FILE = resolve(ROOT, 'frugocorp_modules/create-ngx-docs-site/services/site-variables/generated/all-site-variables.generated.ts');

const PLACEHOLDER_RE = /\(--([A-Z0-9_]+)(?::(pascal|camel|kebab|snake))?--\)/g;

function protectEscapes(markdown) {
  return markdown.replaceAll('(-\\-', ' ');
}

function scanPlaceholders(markdown) {
  const protectedText = protectEscapes(markdown);
  return [...protectedText.matchAll(PLACEHOLDER_RE)].map((match) => ({
    base: match[1],
    casing: match[2] ?? null,
  }));
}

function walkMarkdownFiles(dir) {
  const files = [];
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) {
      files.push(...walkMarkdownFiles(full));
    } else if (extname(entry) === '.md') {
      files.push(full);
    }
  }
  return files;
}

// Mismo criterio de agrupamiento que groupPlaceholders() en site-variables.ts (ver comentario de
// arriba sobre la duplicacion) -- un grupo es "de casing" si ALGUNA ocurrencia en TODO el sitio
// (no solo un archivo) trae sufijo ":casing".
function groupAllOccurrences(occurrencesByFile) {
  const hasCasingByBase = new Map();
  for (const occurrences of occurrencesByFile.values()) {
    for (const occurrence of occurrences) {
      const current = hasCasingByBase.get(occurrence.base) ?? false;
      hasCasingByBase.set(occurrence.base, current || occurrence.casing !== null);
    }
  }
  return [...hasCasingByBase.entries()].sort(([a], [b]) => a.localeCompare(b)).map(([base, casing]) => ({ base, casing }));
}

// Escribe la lista COMPLETA de variables del sitio (todas las paginas juntas, no una sola) --
// consumida por SiteVariablesService para el toggle "mostrar todas" del panel "Mis Variables"
// (ver docs-shell.html/.ts). Se regenera en cada "npm start"/"npm run build", igual que el
// manifiesto de contenido -- nunca se edita a mano, por eso vive gitignoreado.
function writeGeneratedGroupsFile(groups) {
  mkdirSync(dirname(GENERATED_FILE), { recursive: true });
  const entries = groups.map((group) => `  { base: '${group.base}', casing: ${group.casing} },`).join('\n');
  const content = `// GENERADO por frugocorp_modules/create-ngx-docs-site/scripts/validate-site-variables.mjs --
// no editar a mano, se sobreescribe en cada "npm start"/"npm run build".
import type { SiteVariableGroup } from '../site-variables.js';

export const ALL_SITE_VARIABLE_GROUPS: readonly SiteVariableGroup[] = [
${entries}
];
`;
  writeFileSync(GENERATED_FILE, content);
}

function findConflicts(occurrencesByFile) {
  const plainFilesByBase = new Map();
  const casingFilesByBase = new Map();

  for (const [file, occurrences] of occurrencesByFile) {
    for (const occurrence of occurrences) {
      const bucket = occurrence.casing ? casingFilesByBase : plainFilesByBase;
      const files = bucket.get(occurrence.base) ?? new Set();
      files.add(file);
      bucket.set(occurrence.base, files);
    }
  }

  const conflicts = [];
  for (const [base, plainFiles] of plainFilesByBase) {
    const casingFiles = casingFilesByBase.get(base);
    if (casingFiles && casingFiles.size > 0) {
      conflicts.push({ base, plainFiles: [...plainFiles].sort(), casingFiles: [...casingFiles].sort() });
    }
  }
  return conflicts.sort((a, b) => a.base.localeCompare(b.base));
}

const occurrencesByFile = new Map();
for (const file of walkMarkdownFiles(CONTENT_DIR)) {
  const markdown = readFileSync(file, 'utf-8');
  const occurrences = scanPlaceholders(markdown);
  if (occurrences.length > 0) {
    occurrencesByFile.set(relative(ROOT, file).replaceAll('\\', '/'), occurrences);
  }
}

writeGeneratedGroupsFile(groupAllOccurrences(occurrencesByFile));

const conflicts = findConflicts(occurrencesByFile);
if (conflicts.length > 0) {
  console.error('[validate-site-variables] Se encontraron variables con uso ambiguo (suelta Y con casing a la vez):\n');
  for (const conflict of conflicts) {
    console.error(`  "${conflict.base}":`);
    console.error(`    - sin sufijo, en: ${conflict.plainFiles.join(', ')}`);
    console.error(`    - con ":casing", en: ${conflict.casingFiles.join(', ')}`);
    console.error(`    -> agrega ":casing" tambien en el/los archivo(s) de arriba, o usa un nombre distinto para el uso suelto.\n`);
  }
  process.exitCode = 1;
} else if (occurrencesByFile.size > 0) {
  const totalKeys = new Set([...occurrencesByFile.values()].flat().map((o) => o.base)).size;
  console.log(`[validate-site-variables] ${totalKeys} variable(s) encontradas en ${occurrencesByFile.size} archivo(s), sin conflictos.`);
}
