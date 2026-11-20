import type { JsonLdBreadcrumbItem } from '@ngx-docs-markdown-kit/parser-md-seo';
import { breadcrumbChain } from '@site/core/nav';
import { SITE_NAME } from '@site/core/site.config';

/** Home > cada nivel ancestro (CON url real -- cada carpeta de contenido tiene su propia pagina via
 *  "content_dir.md") > Pagina -- mismo camino y misma fuente (breadcrumbChain, nav.ts) que la miga
 *  de pan visual de DocsShell, nunca pueden desincronizarse entre si. */
export function buildJsonLdBreadcrumb(siteUrl: string | null, path: readonly string[]): readonly JsonLdBreadcrumbItem[] {
  if (!siteUrl || path.length === 0) {
    return [];
  }
  return [
    { name: SITE_NAME, url: siteUrl },
    ...breadcrumbChain(path).map((step) => ({ name: step.title, url: `${siteUrl}/${step.path.join('/')}` })),
  ];
}
