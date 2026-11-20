import { existsSync, readFileSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { Marked, type Token, type Tokens } from 'marked';
import { findPage } from './nav';
import { CONTENT_MANIFEST } from '@frugocorp/create-ngx-docs-site/core';
import { PARSER_MD_PATHS } from '@frugocorp/parser-md/config/paths.config';

/**
 * Valida los links del contenido real (content-parser-md/**\/*.md) contra el manifiesto generado --
 * sin navegador ni servidor levantado. El bloque de links externos es opt-in (depende de red real,
 * ver CHECK_EXTERNAL_LINKS).
 */
const CONTENT_ROOT = resolve(process.cwd(), PARSER_MD_PATHS.contentDir);
const marked = new Marked();
const RESERVED_FILENAMES = new Set(['___meta.md', '___content_dir.md', '___order.md']);

interface ExtractedLink {
  href: string;
  fromPath: string;
}

function pathOf(page: (typeof CONTENT_MANIFEST)[number]): string {
  return [...page.group, page.slug].join('/');
}

function collectLinks(tokens: Token[], fromPath: string, out: ExtractedLink[]): void {
  for (const token of tokens) {
    if (token.type === 'link') {
      out.push({ href: (token as Tokens.Link).href, fromPath });
    }
    if ('tokens' in token && token.tokens) {
      collectLinks(token.tokens as Token[], fromPath, out);
    }
    if ('items' in token && token.items) {
      for (const item of token.items as Tokens.ListItem[]) {
        collectLinks(item.tokens, fromPath, out);
      }
    }
    if (token.type === 'table') {
      const table = token as Tokens.Table;
      for (const cell of table.header) {
        collectLinks(cell.tokens, fromPath, out);
      }
      for (const row of table.rows) {
        for (const cell of row) {
          collectLinks(cell.tokens, fromPath, out);
        }
      }
    }
  }
}

// Solo paginas con archivo REAL en disco (no las de grupo -- su "___content_dir.md" es generado,
// sus links son los que este mismo generador ya escribe, no contenido de autor a validar aca).
const realPages = CONTENT_MANIFEST.filter((page) => !page.contentFile.endsWith('___content_dir.md'));
const knownPaths = new Set(CONTENT_MANIFEST.map(pathOf));

const allLinks: ExtractedLink[] = [];
for (const page of realPages) {
  const markdown = readFileSync(resolve(CONTENT_ROOT, page.contentFile), 'utf-8');
  collectLinks(marked.lexer(markdown), pathOf(page), allLinks);
}

function walkRealFiles(dir: string): string[] {
  const files: string[] = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = resolve(dir, entry.name);
    if (entry.isDirectory()) {
      files.push(...walkRealFiles(path));
    } else if (entry.name.endsWith('.md') && !RESERVED_FILENAMES.has(entry.name)) {
      files.push(path);
    }
  }
  return files;
}

describe('Integridad de links internos del contenido (content-parser-md/**/*.md)', () => {
  it('cada pagina real del manifiesto tiene su archivo .md real en content-parser-md/', () => {
    const missing = realPages.filter((page) => !existsSync(resolve(CONTENT_ROOT, page.contentFile)));
    expect(missing.map(pathOf)).toEqual([]);
  });

  it('todo archivo .md real en content-parser-md/ esta registrado en el manifiesto (nada huerfano)', () => {
    const onDisk = new Set(walkRealFiles(CONTENT_ROOT));
    const registered = new Set(realPages.map((page) => resolve(CONTENT_ROOT, page.contentFile)));
    const orphans = [...onDisk].filter((path) => !registered.has(path));
    expect(orphans).toEqual([]);
  });

  it('ningun link interno apunta directo a un .md/README -- debe usar la ruta del sitio', () => {
    const offenders = allLinks.filter((link) => /\.md(#|$)/i.test(link.href));
    expect(offenders).toEqual([]);
  });

  it('ningun link es un fragmento suelto sin ruta ("#ancla" rompe por <base href="/">)', () => {
    const offenders = allLinks.filter((link) => link.href.startsWith('#'));
    expect(offenders).toEqual([]);
  });

  it('todo link interno (/path) apunta a una pagina que existe', () => {
    const offenders = allLinks.filter((link) => {
      if (/^(https?|mailto|tel):/.test(link.href)) {
        return false;
      }
      const path = link.href.split('#')[0].replace(/^\//, '').replace(/\/$/, '');
      if (!path) {
        return false;
      }
      return !knownPaths.has(path);
    });
    expect(offenders).toEqual([]);
  });

  it('todo fragmento de un link interno corresponde a un encabezado real de la pagina destino', () => {
    const offenders = allLinks.filter((link) => {
      const match = /^\/([^#]+)#(.+)$/.exec(link.href);
      if (!match) {
        return false;
      }
      const [, path, fragment] = match;
      const headingIds = new Set(findPage(path.split('/'))?.headings.map((heading) => heading.id) ?? []);
      return !headingIds.has(fragment);
    });
    expect(offenders).toEqual([]);
  });
});

async function fetchWithTimeout(href: string, method: 'HEAD' | 'GET'): Promise<Response> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 10_000);
  try {
    return await fetch(href, { method, redirect: 'follow', signal: controller.signal });
  } finally {
    clearTimeout(timeout);
  }
}

// Opt-in: CHECK_EXTERNAL_LINKS=1 ng test (bash) o $env:CHECK_EXTERNAL_LINKS=1; ng test (PowerShell).
describe.skipIf(!process.env['CHECK_EXTERNAL_LINKS'])('Integridad de links externos (opt-in, requiere red)', () => {
  it(
    'todo link http(s) responde sin error',
    async () => {
      const uniqueHrefs = [...new Set(allLinks.map((link) => link.href).filter((href) => /^https?:\/\//.test(href)))];

      const results = await Promise.all(
        uniqueHrefs.map(async (href) => {
          try {
            let response = await fetchWithTimeout(href, 'HEAD');
            if (!response.ok && (response.status === 405 || response.status === 501)) {
              response = await fetchWithTimeout(href, 'GET');
            }
            return response.ok ? null : { href, problem: `HTTP ${response.status}` };
          } catch (error) {
            return { href, problem: (error as Error).message };
          }
        }),
      );

      const offenders = results.filter((result): result is { href: string; problem: string } => result !== null);
      expect(offenders).toEqual([]);
    },
    30_000,
  );
});
