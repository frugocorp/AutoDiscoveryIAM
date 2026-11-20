// Fuente unica de verdad de DONDE vive el contenido y DONDE se escribe el manifiesto generado --
// rutas relativas a la raiz del sitio (cada script/extension ya calcula esa raiz por su cuenta, ver
// generate-content-manifest.mjs/generate-seo-files.mjs). Cualquier extension que necesite leer
// contenido o el manifiesto generado (ej. parser-md-seo) importa ESTE archivo en vez de
// recalcular/hardcodear las rutas por su cuenta.
export const PARSER_MD_PATHS = {
  /** Carpeta con el contenido real (.md) del sitio -- "content-parser-md" (no "content" a secas)
   *  para que un equipo dev pueda usar esa palabra libremente en su propia logica sin chocar con
   *  esta convencion. Vive DENTRO de "public/" (no en la raiz del sitio, junto a
   *  frugocorp_modules/) por una razon funcional, no solo de orden: ContentService pide el cuerpo
   *  de cada pagina por HTTP (tanto en SSR como en navegacion SPA del lado del cliente, que no
   *  tiene acceso al filesystem) -- Angular solo sirve como asset estatico lo que esta bajo
   *  "public/", cualquier otra ubicacion seria invisible para un fetch del navegador. */
  contentDir: 'public/content-parser-md',
  /** El manifiesto generado le pertenece conceptualmente a "parser-md" (la libreria que ESCANEA
   *  contentDir), no al CLI que lo consume -- vive bajo su propia carpeta en frugocorp_modules/. */
  manifestOutput: 'frugocorp_modules/parser-md/generated/content-manifest.generated.ts',
} as const;
