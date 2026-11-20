import type { DocPageHeading } from '@ngx-docs-markdown-kit/parser-md';
// Import RELATIVO con extension (mismo motivo documentado en content-manifest.ts: este archivo, via
// nav.ts, lo carga generate-seo-files.mjs con "node" plano, que no resuelve el alias
// "@frugocorp/*") -- pero SIN repetir la ruta real al archivo generado, eso vive en UN solo lugar
// (content-manifest.ts, hermano de este archivo).
import type { GeneratedPage } from './content-manifest.ts';

/** Generico y reusable, sin saber nada de "barras" -- una pagina navegable (real o el
 *  "___content_dir.md" de un grupo, ver nav-bar-group.ts) por su PATH completo (segmentos de URL). */
export interface NavItem {
  readonly path: readonly string[];
  readonly title: string;
  readonly headings: readonly DocPageHeading[];
}

/** Un grupo es TAMBIEN un NavItem (su propio "___content_dir.md" es una pagina real y clickeable) que
 *  ademas agrupa a sus hijos directos. */
export interface NavTreeNode extends NavItem {
  readonly items: readonly NavItem[];
  readonly children: readonly NavTreeNode[];
}

export interface NavTree {
  readonly items: readonly NavItem[];
  readonly groups: readonly NavTreeNode[];
}

function toNavItem(entry: GeneratedPage): NavItem {
  return { path: [...entry.group, entry.slug], title: entry.title, headings: entry.headings };
}

/** Construye el arbol de "entries" (ya filtradas a las que pertenecen a "parentPath") relativo a
 *  ese path -- la entrada CUYO path completo coincide EXACTO con "parentPath" es el "___content_dir.md"
 *  del propio nivel (no es un hijo, se omite del arbol); el resto se agrupa por su primer segmento
 *  relativo. Recursivo: cada grupo hijo se resuelve exactamente igual. El orden sigue "order" del
 *  manifiesto (ya resuelto por generate-content-manifest.mjs, alfabetico por default u "___order.md"). */
export function buildNavTree(entries: readonly GeneratedPage[], parentPath: readonly string[]): NavTree {
  const byChildSegment = new Map<string, GeneratedPage[]>();
  // La entrada de un hijo directo (relative.length === 1) puede ser DOS cosas distintas, que no se
  // pueden distinguir todavia en esta primera pasada: una pagina suelta real, O la pagina PROPIA de
  // un grupo (su "___content_dir.md") -- solo se sabe cual es una vez que se ve si ESE MISMO segmento
  // tambien tiene hijos mas profundos (relative.length > 1) en la segunda pasada, mas abajo.
  const ownEntryByChildSegment = new Map<string, GeneratedPage>();

  for (const entry of entries) {
    const path = [...entry.group, entry.slug];
    if (path.length <= parentPath.length) {
      continue; // el propio "parentPath" (su "___content_dir.md") -- no es hijo de si mismo
    }
    const relative = path.slice(parentPath.length);
    const [childSegment] = relative;
    if (relative.length === 1) {
      ownEntryByChildSegment.set(childSegment, entry);
    } else {
      const bucket = byChildSegment.get(childSegment) ?? [];
      bucket.push(entry);
      byChildSegment.set(childSegment, bucket);
    }
  }

  // Ahora si se sabe: un childSegment con hijos mas profundos (esta en byChildSegment) es un grupo
  // real -- su entrada de "relative.length === 1" (si existe) NO es una pagina suelta aparte, es la
  // pagina propia de ese grupo (se usa mas abajo, nunca se duplica como item). Los que NO tienen
  // hijos mas profundos si son paginas sueltas de verdad.
  const items: (NavItem & { order: number })[] = [];
  for (const [childSegment, entry] of ownEntryByChildSegment) {
    if (!byChildSegment.has(childSegment)) {
      items.push({ ...toNavItem(entry), order: entry.order });
    }
  }

  const groups: (NavTreeNode & { order: number })[] = [];
  for (const [childSegment, childEntries] of byChildSegment) {
    const childPath = [...parentPath, childSegment];
    const ownEntry = ownEntryByChildSegment.get(childSegment);
    const subtree = buildNavTree(childEntries, childPath);
    groups.push({
      path: childPath,
      title: ownEntry?.title ?? childSegment,
      headings: ownEntry?.headings ?? [],
      items: subtree.items,
      children: subtree.groups,
      order: ownEntry?.order ?? 0,
    });
  }

  return {
    items: items.sort((a, b) => a.order - b.order),
    groups: groups.sort((a, b) => a.order - b.order),
  };
}
