import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

// Sufijo ".server.ts" + solo importado desde app.config.server.ts -- nunca alcanzable desde
// main.ts (entrada de browser), asi que "node:fs"/"node:path" nunca llegan al bundle de browser.
// A diferencia de content-disk-reader.server.ts (que lee DENTRO de public/content-parser-md/), este
// lee relativo a "public/" directo -- el alcance real de parser-md-file-embed (ver su propio
// README): solo archivos que YA estan dentro de public/ del sitio, nunca de otra parte del repo.
export function readFileEmbedFromDisk(publicPath: string): string {
  return readFileSync(resolve(process.cwd(), 'public', publicPath), 'utf-8');
}
