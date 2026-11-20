import { Injectable, signal } from '@angular/core';
import { SITE_NAME } from './site.config';

export interface ResourcesConfig {
  /** Nombre mostrado en la barra 1 y el header movil -- si no se especifica, cae a SITE_NAME
   *  (ver site.config.ts, el placeholder AutoDiscoveryIAM que el CLI sustituye al generar el sitio). */
  readonly siteName: string;
  /** Ruta a un logo para tema claro (ej. "/images/logo.svg", dentro de public/) -- si es null, se
   *  muestra el nombre como texto (ver AppShell) en vez de una imagen. Separado de logoDark porque
   *  un logo con texto/trazo oscuro se pierde contra un fondo oscuro y viceversa -- el sitio ya
   *  trae de ejemplo el logo de FrugoCorp en sus 2 variantes (ver public/images/), reemplazalos o
   *  pon ambos en null para volver al nombre como texto. */
  readonly logoLight: string | null;
  /** Ruta a un logo para tema oscuro -- ver logoLight. */
  readonly logoDark: string | null;
}

/** Recursos visuales del sitio (nombre mostrado, logos) -- edita estos valores directamente
 *  (requiere recompilar), NO un JSON de public/ -- mismo motivo que DESIGN_CONFIG/SEO_CONFIG: el
 *  SSR debe tener el valor real desde el primer render, sin depender de un fetch en runtime. */
export const RESOURCES_CONFIG: ResourcesConfig = {
  siteName: SITE_NAME,
  logoLight: '/images/frugocorp-logo-black.svg',
  logoDark: '/images/frugocorp-logo-white.svg',
};

@Injectable({ providedIn: 'root' })
export class ResourcesConfigService {
  readonly config = signal(RESOURCES_CONFIG).asReadonly();
}
