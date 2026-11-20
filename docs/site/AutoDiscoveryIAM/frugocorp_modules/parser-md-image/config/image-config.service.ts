import { Injectable, signal } from '@angular/core';

export interface ImageConfig {
  /** Alineacion cuando un ```image no trae "align:" en el fence (ver ImageSegment.align,
   *  opcional a proposito -- parser-md-image ya no impone 'center' fijo). */
  readonly defaultAlign: 'left' | 'center' | 'right';
  /** Ancho en px cuando un ```image no trae "width:" -- null es "sin limite" (el default). */
  readonly defaultWidth: string | null;
}

/** Defaults para el ```image de parser-md-image -- edita estos valores directamente (requiere
 *  recompilar), NO un JSON de public/: el <img> real de cada imagen SIEMPRE se renderiza de forma
 *  sincrona (viene del propio contenido Markdown, no de este config), pero align/width SI dependen
 *  de esto cuando el fence no los especifica -- si ese valor llegara tarde (fetch async), Google
 *  Imagenes podria indexar la pagina con la imagen ya presente pero mal alineada/dimensionada en
 *  el primer render. Mismo motivo que SEO_CONFIG (ver seo.config.ts). */
export const IMAGE_CONFIG: ImageConfig = { defaultAlign: 'center', defaultWidth: null };

@Injectable({ providedIn: 'root' })
export class ImageConfigService {
  readonly config = signal(IMAGE_CONFIG).asReadonly();
}
