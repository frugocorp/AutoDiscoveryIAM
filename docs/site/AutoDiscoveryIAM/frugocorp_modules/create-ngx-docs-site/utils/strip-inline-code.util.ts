/** "heading.text" (parser-md, ver DocPageHeading) es el markdown CRUDO del encabezado, nunca
 *  pasado por un renderer -- si el encabezado original tenia codigo en linea (` `appsettings.json` `)
 *  el backtick llega literal. Usado SOLO para texto mostrado fuera del HTML ya renderizado por
 *  parser-md (la barra "En esta pagina", ver docs-shell.html) -- el <article> con el contenido real
 *  no lo necesita, ese codigo en linea ya sale como <code> real. */
export function stripInlineCode(text: string): string {
  return text.replace(/`/g, '');
}
