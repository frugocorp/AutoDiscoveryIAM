import type { AppSegment } from './segment-types';

/** Recorre el arbol de segmentos (los "section" anidan los suyos, ver last-section.util.ts) y
 *  devuelve true apenas encuentra un bloque de codigo real -- "code" (` ```code-block `) o
 *  "card-code-block" (el fence de comando+resultado). Usado para decidir si la barra 3 ("En esta
 *  pagina") se muestra en una pagina con menos de 2 encabezados: sin esto, una pagina con 1 sola
 *  seccion pero llena de comandos/variables `(--CLAVE--)` nunca mostraba el panel "Mis Variables"
 *  (ver showOnThisPage() en docs-shell.ts). */
export function hasCodeBlock(segments: readonly AppSegment[]): boolean {
  return segments.some((segment) => {
    if (segment.type === 'code' || segment.type === 'card-code-block') {
      return true;
    }
    if (segment.type === 'section') {
      return hasCodeBlock(segment.segments as readonly AppSegment[]);
    }
    return false;
  });
}
