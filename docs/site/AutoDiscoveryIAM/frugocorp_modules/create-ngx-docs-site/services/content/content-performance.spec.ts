import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { createParser } from '@ngx-docs-markdown-kit/parser-md';
import { seoExtension } from '@ngx-docs-markdown-kit/parser-md-seo';
import { codeBlockExtension } from '@ngx-docs-markdown-kit/parser-md-code-block';
import { CONTENT_MANIFEST } from '@frugocorp/create-ngx-docs-site/core';
import { PARSER_MD_PATHS } from '@frugocorp/parser-md/config/paths.config';

/** Presupuesto generoso a proposito -- el objetivo es atrapar una regresion accidental (ej. un
 *  bucle O(n^2) en una extension nueva), no perseguir microsegundos. */
const CONTENT_ROOT = resolve(process.cwd(), PARSER_MD_PATHS.contentDir);
const PER_PAGE_BUDGET_MS = 200;

describe('Performance del parseo de contenido', () => {
  const parser = createParser().use(seoExtension()).use(codeBlockExtension());

  it.each(CONTENT_MANIFEST.map((page) => ({ path: [...page.group, page.slug].join('/'), contentFile: page.contentFile })))('parsea "$path" en menos de 200ms', ({ contentFile }) => {
    const markdown = readFileSync(resolve(CONTENT_ROOT, contentFile), 'utf-8');

    const start = performance.now();
    parser.parse(markdown);
    const elapsed = performance.now() - start;

    expect(elapsed).toBeLessThan(PER_PAGE_BUDGET_MS);
  });
});
