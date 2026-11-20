// UNICO lugar del sitio que conoce la ruta real del manifiesto generado -- cualquier otro archivo
// (Angular o script Node plano) importa "CONTENT_MANIFEST"/sus tipos DESDE ACA, nunca repitiendo la
// ruta relativa al archivo generado por su cuenta. Si esa ruta cambia (ver
// frugocorp_modules/parser-md/config/paths.config.ts), solo este archivo necesita actualizarse.
//
// Import RELATIVO con extension ".ts" (no el alias "@frugocorp/*") a proposito: este archivo es
// alcanzable, a traves de "nav.ts", desde "generate-seo-files.mjs" -- un script que corre con
// "node" plano (sin bundler) y no resuelve el alias (solo TypeScript/Angular lo entienden, via
// tsconfig "paths").
export { CONTENT_MANIFEST } from '../../parser-md/generated/content-manifest.generated.ts';
export type { GeneratedHeading, GeneratedPage } from '../../parser-md/generated/content-manifest.generated.ts';
