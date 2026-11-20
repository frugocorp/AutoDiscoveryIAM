#!/usr/bin/env node
// Frena "ng build" (nunca "ng serve"/"npm start" -- un sitio recien scaffoldeado arranca en dev
// con estos placeholders sin tocar todavia, a proposito) si "privacy-en.md"/"privacy-es.md"
// siguen con el placeholder de fabrica sin reemplazar -- mismo criterio que ya usa este CLI para
// un "[slug]" invalido o un conflicto de "Mis Variables": nunca algo roto (en este caso, un dato
// legal falso) en silencio en produccion.
//
// A proposito NO se resuelve con "Mis Variables" (--CLAVE--) -- esa sintaxis vive en localStorage,
// POR NAVEGADOR: cada visitante veria un valor distinto (o el crudo, si nunca lo completo) para
// algo que tiene que ser el MISMO dato legal para todos. Tampoco se resuelve con una constante de
// config nueva (ver el comentario de site.config.ts: "no hay mas sustitucion automatica que
// [SITE_NAME]") -- se edita el .md directamente, como la licencia.
import { existsSync, readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { PARSER_MD_PATHS } from '../../parser-md/config/paths.config.ts';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const CONTENT_DIR = resolve(ROOT, PARSER_MD_PATHS.contentDir);

// Placeholder EXACTO que trae la plantilla de fabrica -- un sitio que lo edito a su propio dato
// real (aunque sea distinto texto) nunca matchea esto, el chequeo es deliberadamente literal.
const PLACEHOLDER_FILES = [
  { file: 'privacy-en.md', markers: ['[DATE]', '[contact@your-site.com]'] },
  { file: 'privacy-es.md', markers: ['[FECHA]', '[correo@tu-sitio.com]'] },
];

const offenders = [];
for (const { file, markers } of PLACEHOLDER_FILES) {
  const path = resolve(CONTENT_DIR, file);
  if (!existsSync(path)) {
    continue; // La pagina se borro a mano (sitio sin politica de privacidad) -- valido, se saltea.
  }
  const content = readFileSync(path, 'utf-8');
  const foundMarkers = markers.filter((marker) => content.includes(marker));
  if (foundMarkers.length > 0) {
    offenders.push({ file, foundMarkers });
  }
}

if (offenders.length > 0) {
  console.error('[validate-privacy-placeholders] Hay placeholders de fabrica sin reemplazar en la politica de privacidad:\n');
  for (const { file, foundMarkers } of offenders) {
    console.error(`  public/content-parser-md/${file}: ${foundMarkers.join(', ')}`);
  }
  console.error('\n  -> edita esos archivos a mano con el dato real de tu sitio antes de publicar en produccion.');
  process.exitCode = 1;
}
