// Import relativo con extension -- generate-seo-files.mjs importa este archivo directo con "node"
// plano (sin bundler), un especificador con alias ("@frugocorp/*") no resuelve ahi.
import {
  CONTENT_MANIFEST,
  NAV,
  type GeneratedPage,
  type NavSection,
} from '../../../frugocorp_modules/create-ngx-docs-site/core.ts';

// Fachada: el equipo dev agrega sus propios navlinks/rutas aca (o en otro archivo que importe
// aca). La logica de cada barra vive en frugocorp_modules/create-ngx-docs-site/, sin editar a mano.
export {
  NAV,
  activeSectionFor,
  slugFromUrl,
  navTreeForSection,
  headingsForPath,
  buildNavTree,
  type NavSection,
  type NavItem,
  type NavTree,
  type NavTreeNode,
  type GeneratedPage,
} from '../../../frugocorp_modules/create-ngx-docs-site/core.ts';

export function findPage(pathSegments: readonly string[]): GeneratedPage | undefined {
  const key = pathSegments.join('/');
  return CONTENT_MANIFEST.find((page) => [...page.group, page.slug].join('/') === key);
}

export function resolveActiveItem(pathSegments: readonly string[]): { section: NavSection; item: GeneratedPage } | null {
  const item = findPage(pathSegments);
  if (!item || item.section === null) {
    return null;
  }
  const section = NAV.find((candidate) => candidate.key === item.section);
  return section ? { section, item } : null;
}

export function allPaths(): readonly (readonly string[])[] {
  return CONTENT_MANIFEST.map((page) => [...page.group, page.slug]);
}

export interface BreadcrumbStep {
  readonly title: string;
  readonly path: readonly string[];
}

/** Cada nivel ancestro de "pathSegments" que exista como pagina real (siempre incluye la actual) --
 *  fuente compartida de la miga de pan visual y del BreadcrumbList de JSON-LD (breadcrumb-path.ts),
 *  para que nunca se desincronicen entre si. */
export function breadcrumbChain(pathSegments: readonly string[]): readonly BreadcrumbStep[] {
  const steps: BreadcrumbStep[] = [];
  for (let depth = 1; depth <= pathSegments.length; depth++) {
    const path = pathSegments.slice(0, depth);
    const page = findPage(path);
    if (page) {
      steps.push({ title: page.title, path });
    }
  }
  return steps;
}
