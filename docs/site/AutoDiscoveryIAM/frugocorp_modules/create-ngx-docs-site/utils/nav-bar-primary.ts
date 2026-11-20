// Import relativo con extension -- ver content-manifest.ts (mismo motivo: generate-seo-files.mjs
// carga este archivo via nav.ts con "node" plano, sin resolver el alias "@frugocorp/*").
import { CONTENT_MANIFEST } from './content-manifest.ts';

/** Resuelve la barra 1 (rail de iconos) -- una seccion por cada carpeta de NIVEL SUPERIOR de
 *  content-parser-md/ que tenga contenido navegable (frontmatter "nav: false" la excluye igual que
 *  a cualquier pagina). Nada hardcodeado: agregar una carpeta nueva ("references/") alcanza para
 *  que aparezca aca, sin tocar ningun archivo de codigo. */
export interface NavSection {
  readonly key: string;
  readonly label: string;
  readonly icon: string;
}

export const NAV: readonly NavSection[] = CONTENT_MANIFEST.filter((page) => page.group.length === 0 && page.section === page.slug)
  .sort((a, b) => a.order - b.order)
  .map((page) => ({ key: page.slug, label: page.title, icon: page.icon ?? 'folder' }));

/** "/docs/instalacion/primeros-pasos#id" -> "docs" -- primer segmento de la URL, sin el fragmento
 *  (router.url en el cliente, a diferencia del HTML prerenderizado, puede traer un "#id" que deja
 *  un salto por fragmento -- ver jumpToHeading()). */
export function slugFromUrl(url: string): string | null {
  const path = url.split(/[?#]/)[0];
  return path.split('/').filter((part) => part.length > 0)[0] ?? null;
}

/** null en la raiz del sitio (HomePage, fuera de NAV) o en cualquier URL sin seccion (ej. 404, o
 *  una ruta personalizada del equipo dev como "/status" -- ver app.routes.ts). */
export function activeSectionFor(url: string, sections: readonly NavSection[] = NAV): NavSection | null {
  const key = slugFromUrl(url);
  return sections.find((section) => section.key === key) ?? null;
}
