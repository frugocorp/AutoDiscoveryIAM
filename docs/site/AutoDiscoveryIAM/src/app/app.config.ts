import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { provideClientHydration } from '@angular/platform-browser';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideSeoConfig, provideSeoSiteName } from '@ngx-docs-markdown-kit/parser-md-seo';
import { provideCodeThemeCatalog } from '@ngx-docs-markdown-kit/parser-md-code-block';
import { THEME_CATALOG } from '@ngx-docs-markdown-kit/parser-md-code-block-themes';
import { routes } from './app.routes';
import { SEO_CONFIG } from '@frugocorp/parser-md-seo/config/seo.config';
import { SITE_NAME } from './core/site.config';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    // withFetch(): usa la Fetch API en vez de XHR -- obligatorio en SSR (Node no tiene
    // XMLHttpRequest), necesario para que ContentService pueda pedir los .md de public/content/
    // tambien durante el render del servidor, no solo en el navegador.
    provideHttpClient(withFetch()),
    provideClientHydration(),
    // Requerido por MatSidenav (drawer movil de las barras 1 y 2, ver shared/app-shell y
    // bounded-contexts/content-parser-md/shared/docs-shell) para su transicion de apertura/cierre -- "Async" carga el modulo de
    // animaciones en un chunk aparte, sin inflar el bundle inicial para el resto del sitio.
    provideAnimationsAsync(),
    // Nombre del sitio para @ngx-docs-markdown-kit/parser-md-seo (titulo de pagina, og:site_name,
    // JSON-LD) -- la libreria no tiene forma de adivinarlo sola, siempre hay que proveerlo (ver
    // SEO_SITE_NAME, sin default a proposito). No esta envuelto en NDMK:OPTIONAL: SEO no es una
    // extension opcional del CLI, es parte del nucleo.
    provideSeoSiteName(SITE_NAME),
    // Config de SEO (description/keywords/ogImage/siteUrl/etc, ver frugocorp_modules/parser-md-seo/config/seo.config.ts) --
    // provista aqui como VALOR COMPILADO, nunca pedida por HTTP en runtime: los meta tags/JSON-LD
    // deben quedar en el HTML servido desde el primer render para que un crawler los indexe. Edita
    // frugocorp_modules/parser-md-seo/config/seo.config.ts (requiere recompilar) para personalizarla, no un JSON en public/.
    provideSeoConfig(SEO_CONFIG),
    // Sin esto, el engrane de "Estilo de codigo" (ver AppShell) muestra el catalogo vacio (solo
    // "Default" seleccionable) aunque el paquete este instalado -- CODE_THEME_CATALOG no se llena
    // solo, hay que proveerlo explicitamente una vez, aqui.
    provideCodeThemeCatalog(THEME_CATALOG),
  ],
};
