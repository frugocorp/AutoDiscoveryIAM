#!/usr/bin/env node
// Despachador generico de "perfiles" npm -- arbol de archivos JSON de PROFUNDIDAD LIBRE (no hay un
// numero fijo de niveles). Cada nodo del arbol es, para cada nombre que contiene, o bien:
//   - un "puente": { "description": "...", "file": "ruta/al/siguiente.json" } -- sigue bajando.
//   - un "perfil" (hoja): { "description": "...", "include": [...], ... } -- no tiene "file",
//     se ejecuta directo.
// "file" siempre se resuelve relativo a la carpeta donde vive el archivo puente que lo contiene --
// asi un nivel entero (o varios) se puede mover de carpeta sin romper nada. Agregar un nivel mas o
// menos de anidamiento (ej. tests/unit/auth/login/...) es solo JSON, nunca toca este script.
//
// Uso: npm run profile -- <categoria> [subgrupo...] <perfil> [flags extra]
// Ej.: npm run profile -- tests links external
// Ej. (mas hondo): npm run profile -- tests unit auth login
import { readFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const PROFILES_ROOT = dirname(fileURLToPath(import.meta.url));
const ROOT = resolve(PROFILES_ROOT, '..');
const ROOT_REGISTRY_PATH = resolve(PROFILES_ROOT, 'npm-profiles.json');

function readJson(absPath) {
  return JSON.parse(readFileSync(absPath, 'utf-8'));
}

function listEntries(title, entries) {
  console.error(`\n${title}:`);
  for (const [name, entry] of Object.entries(entries)) {
    console.error(`  ${name.padEnd(20)} ${entry.description ?? ''}`);
  }
}

// Todo lo que viene antes del primer "--..." es el camino (categoria/subgrupo/.../perfil); lo que
// sigue son flags que se le pasan tal cual a "ng test" (ej. --watch).
const rawArgs = process.argv.slice(2);
const flagStart = rawArgs.findIndex((arg) => arg.startsWith('--'));
const pathSegments = flagStart === -1 ? rawArgs : rawArgs.slice(0, flagStart);
const extraArgs = flagStart === -1 ? [] : rawArgs.slice(flagStart);

if (pathSegments.length === 0) {
  console.error('Uso: npm run profile -- <categoria> [subgrupo...] <perfil> [flags extra]');
  listEntries('Categorias disponibles (ver npm-profiles/npm-profiles.json)', readJson(ROOT_REGISTRY_PATH));
  process.exit(1);
}

// Recorre el arbol paso a paso, un segmento de argv por nivel, sin limite de profundidad.
function resolveProfile(bridgePath, node, remainingSegments, consumed) {
  if (remainingSegments.length === 0) {
    listEntries(`Falta indicar un nivel mas -- opciones en "${consumed.join(' ') || '(raiz)'}" (ver ${bridgePath})`, node);
    process.exit(1);
  }

  const [segment, ...rest] = remainingSegments;
  const entry = node[segment];
  if (!entry) {
    listEntries(`Opciones disponibles en "${consumed.join(' ') || '(raiz)'}" (ver ${bridgePath})`, node);
    process.exit(1);
  }

  const nextConsumed = [...consumed, segment];

  if ('file' in entry) {
    const nextPath = resolve(dirname(bridgePath), entry.file);
    return resolveProfile(nextPath, readJson(nextPath), rest, nextConsumed);
  }

  // Sin "file" -> esta entrada YA es el perfil ejecutable. No deberia sobrar ningun segmento.
  if (rest.length > 0) {
    console.error(`"${nextConsumed.join(' ')}" ya es un perfil ejecutable -- sobran argumentos: ${rest.join(' ')}`);
    process.exit(1);
  }

  return { profile: entry, categoryName: consumed[0] ?? segment, path: nextConsumed };
}

const { profile, categoryName, path } = resolveProfile(ROOT_REGISTRY_PATH, readJson(ROOT_REGISTRY_PATH), pathSegments, []);

// A partir de aqui, como se arma el comando real depende SOLO de la categoria (primer segmento del
// camino) -- "tests" siempre corre "ng test" con include/exclude/filter/env/coverage declarativos.
// Una categoria futura con una forma de comando distinta (ej. "build") necesita su propia rama aqui
// -- a proposito no se generaliza de mas sin un segundo caso real que confirme como abstraerlo.
if (categoryName === 'tests') {
  const args = ['test'];
  for (const pattern of profile.include ?? []) {
    args.push(`--include=${pattern}`);
  }
  for (const pattern of profile.exclude ?? []) {
    args.push(`--exclude=${pattern}`);
  }
  if (profile.filter) {
    args.push(`--filter=${profile.filter}`);
  }
  if (profile.coverage) {
    args.push('--coverage');
    for (const reporter of profile.coverageReporters ?? ['text']) {
      args.push(`--coverage-reporters=${reporter}`);
    }
    // Sin --coverage-include explicito, el reporte solo cuenta los archivos que los specs llegaron
    // a importar -- cualquier archivo sin NINGUN test ni siquiera aparece en la tabla (ni como 0%).
    for (const pattern of profile.coverageInclude ?? []) {
      args.push(`--coverage-include=${pattern}`);
    }
    for (const pattern of profile.coverageExclude ?? []) {
      args.push(`--coverage-exclude=${pattern}`);
    }
  }
  // "ng test" corre en modo watch por default en una terminal real (TTY) -- se fuerza
  // --watch=false salvo que quien invoca ya haya pasado su propio --watch a mano.
  if (!extraArgs.some((arg) => arg.startsWith('--watch'))) {
    args.push('--watch=false');
  }
  args.push(...extraArgs);

  const result = spawnSync('ng', args, {
    cwd: ROOT,
    stdio: 'inherit',
    shell: true, // resuelve "ng" -> node_modules/.bin/ng(.cmd) igual en Windows/POSIX
    env: { ...process.env, ...(profile.env ?? {}) },
  });
  process.exit(result.status ?? 1);
}

console.error(`La categoria "${categoryName}" (camino: ${path.join(' ')}) no tiene logica de ejecucion todavia.`);
process.exit(1);
