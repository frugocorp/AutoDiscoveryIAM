import { Injectable, signal } from '@angular/core';

export interface DesignConfig {
  /** Barra 1 (icon rail + engrane + tema) -- ver AppShell. */
  readonly showPrimaryBar: boolean;
  /** Barra 2 (arbol de navegacion) -- ver DocsShell. */
  readonly showSecondaryBar: boolean;
  /** Barra 3 (en esta pagina) -- ver DocsShell. */
  readonly showOnThisPage: boolean;
}

/** Comportamiento de las 3 barras -- edita estos valores directamente (requiere recompilar) para
 *  apagar alguna. Vive en TS fuente, NO en un JSON de public/: ningun config del sitio depende de
 *  una peticion HTTP en runtime, para que el SSR tenga siempre el valor real (sincrono) desde el
 *  primer render -- ver seo.config.ts para el detalle completo de por que (aplica igual aqui
 *  aunque este config en particular no sea SEO-critico, por consistencia con el resto).
 *
 *  El panel "Mis Variables" (dentro de la barra 3, ver SiteVariablesService/DocsShell) NO tiene
 *  flag aca a proposito -- se autodescubre por pagina (aparece solo si esa pagina usa alguna
 *  "(--CLAVE--)"), asi que nunca hace falta prenderlo/apagarlo a mano. */
export const DESIGN_CONFIG: DesignConfig = {
  showPrimaryBar: true,
  showSecondaryBar: true,
  showOnThisPage: true,
};

@Injectable({ providedIn: 'root' })
export class DesignConfigService {
  readonly config = signal(DESIGN_CONFIG).asReadonly();
}
