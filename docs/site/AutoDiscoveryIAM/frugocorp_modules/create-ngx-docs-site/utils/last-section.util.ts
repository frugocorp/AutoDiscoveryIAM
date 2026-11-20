import type { AppSegment } from './segment-types';

/** El id de la ULTIMA seccion en orden de documento, sin importar cuan anidada este -- una pagina
 *  real casi siempre tiene un solo `SectionSegment` de nivel superior (el H1 envuelve todo lo
 *  demas), asi que ".doc-content > .page-section:last-child" en CSS puro solo agarra ESE wrapper
 *  exterior, nunca la subseccion visualmente ultima. Bajar recursivamente por el ULTIMO segmento de
 *  cada nivel (mientras siga siendo una seccion) encuentra la hoja real. */
export function findLastSectionId(segments: readonly AppSegment[]): string | null {
  for (let i = segments.length - 1; i >= 0; i--) {
    const segment = segments[i];
    if (segment.type === 'section') {
      return findLastSectionId(segment.segments as readonly AppSegment[]) ?? segment.id;
    }
  }
  return null;
}
