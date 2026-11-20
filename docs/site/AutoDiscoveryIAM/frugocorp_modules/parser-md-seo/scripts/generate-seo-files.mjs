#!/usr/bin/env node
// Genera public/robots.txt y public/sitemap.xml a partir del manifiesto real de contenido -- corre
// ANTES del build real (ver "build" en package.json).
import { writeFileSync } from 'node:fs';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { buildSitemapXml, buildRobotsTxt } from '@ngx-docs-markdown-kit/parser-md-seo/sitemap';
import { CONTENT_MANIFEST } from '../../create-ngx-docs-site/core.ts';
import { SEO_CONFIG } from '../config/seo.config.ts';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const PUBLIC_ROOT = resolve(ROOT, 'public');

function readSiteUrl() {
  return typeof SEO_CONFIG.siteUrl === 'string' ? SEO_CONFIG.siteUrl.replace(/\/$/, '') : null;
}

/** @returns {{ loc: string, lastmod?: string }[]} */
function collectUrls(siteUrl) {
  const urls = [{ loc: siteUrl }];
  // "lastmod" ya viene resuelto en el manifiesto (parser-md lo calcula al escanear cada .md real,
  // o la fecha de generacion para el "___content_dir.md" de un grupo) -- no hace falta volver a
  // tocar el filesystem aca.
  for (const page of CONTENT_MANIFEST) {
    const path = [...page.group, page.slug].join('/');
    urls.push({ loc: `${siteUrl}/${path}`, lastmod: page.lastmod });
  }
  return urls;
}

const siteUrl = readSiteUrl();
if (!siteUrl) {
  console.warn(
    '[generate-seo-files] SEO_CONFIG.siteUrl (frugocorp_modules/parser-md-seo/config/seo.config.ts) no esta configurado -- se omite ' +
      'sitemap.xml/robots.txt (no hay forma de armar URLs absolutas validas sin un dominio). Configuralo cuando tengas el dominio real.',
  );
} else {
  const urls = collectUrls(siteUrl);
  writeFileSync(resolve(PUBLIC_ROOT, 'sitemap.xml'), buildSitemapXml(urls));
  writeFileSync(resolve(PUBLIC_ROOT, 'robots.txt'), buildRobotsTxt(siteUrl));
  console.log(`[generate-seo-files] sitemap.xml generado con ${urls.length} URLs, robots.txt generado -- ambos en public/.`);
}
