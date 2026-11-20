// Import relativo con extension -- ver content-manifest.ts.
import { CONTENT_MANIFEST } from './content-manifest.ts';
import { buildNavTree, type NavTree } from './nav-tree.util.ts';

/** Arbol de la barra 2 para la seccion "sectionKey" (ej. "docs") -- todas las entradas del
 *  manifiesto cuyo "section" coincide, mas la entrada de la seccion misma (su propio
 *  "___content_dir.md"), pasadas por buildNavTree() relativas al path ["sectionKey"]. */
export function navTreeForSection(sectionKey: string): NavTree {
  const entries = CONTENT_MANIFEST.filter((page) => page.section === sectionKey);
  return buildNavTree(entries, [sectionKey]);
}
