/** Valores de ejemplo mostrados ANTES de que el lector complete "Mis Variables" -- puramente
 *  cosmetico, nunca reemplaza un valor real ya guardado en localStorage (ver
 *  SiteVariablesService.get(), que solo cae aca si no hay nada guardado para esa clave). Vacio por
 *  default: cada sitio generado por el CLI llena el suyo segun sus propias variables reales, sin
 *  tocar ningun otro archivo del mecanismo. */
export const SITE_VARIABLE_DEFAULTS: ReadonlyMap<string, string> = new Map([
  ["PROYECTO", "TuProyecto"],
]);
