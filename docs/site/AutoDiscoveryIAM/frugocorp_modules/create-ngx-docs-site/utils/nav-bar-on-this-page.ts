// Import relativo con extension -- ver content-manifest.ts.
import { CONTENT_MANIFEST, type GeneratedHeading } from './content-manifest.ts';

/** Lo que la barra 3 ("En esta pagina") necesita de una pagina dada -- el resto de su estado
 *  (que heading esta activo ahora, el scroll en si) vive en OnThisPageService/ScrollSpyDirective,
 *  este archivo solo resuelve los headings DE ORIGEN a partir del manifiesto. */
export function headingsForPath(pathSegments: readonly string[]): readonly GeneratedHeading[] {
  const key = pathSegments.join('/');
  return CONTENT_MANIFEST.find((page) => [...page.group, page.slug].join('/') === key)?.headings ?? [];
}
