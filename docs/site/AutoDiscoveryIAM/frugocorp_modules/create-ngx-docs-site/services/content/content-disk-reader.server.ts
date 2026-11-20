import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { PARSER_MD_PATHS } from '@frugocorp/parser-md/config/paths.config';

// Sufijo ".server.ts" + solo importado desde app.config.server.ts -- nunca alcanzable desde
// main.ts (entrada de browser), asi que "node:fs"/"node:path" nunca llegan al bundle de browser.
export function readContentFromDisk(contentFile: string): string {
  return readFileSync(resolve(process.cwd(), PARSER_MD_PATHS.contentDir, contentFile), 'utf-8');
}
