// Fachada del modulo -- UNICO punto de entrada para lo que vive bajo utils/components/services/
// directives/ de create-ngx-docs-site. Import relativo con extension: generate-seo-files.mjs llega
// hasta aca via nav.ts con "node" plano, sin bundler que resuelva el alias "@frugocorp/*".
export { CONTENT_MANIFEST, type GeneratedHeading, type GeneratedPage } from './utils/content-manifest.ts';
export { NAV, activeSectionFor, slugFromUrl, type NavSection } from './utils/nav-bar-primary.ts';
export { navTreeForSection } from './utils/nav-bar-group.ts';
export { headingsForPath } from './utils/nav-bar-on-this-page.ts';
export { buildNavTree, type NavItem, type NavTree, type NavTreeNode } from './utils/nav-tree.util.ts';
