export interface CasingVariants {
  readonly pascal: string;
  readonly camel: string;
  readonly kebab: string;
  readonly snake: string;
}

// Separa por "_"/"-"/espacios Y por un limite de camelCase/PascalCase -- asi el input queda
// robusto aunque el lector no escriba snake_case exacto, sin necesitar validar/rechazar nada.
function wordsFrom(raw: string): string[] {
  return raw
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .split(/[\s_-]+/)
    .map((word) => word.toLowerCase())
    .filter(Boolean);
}

function capitalize(word: string): string {
  return word.charAt(0).toUpperCase() + word.slice(1);
}

/** Deriva las 4 variantes de casing de CUALQUIER valor de texto -- usada por cualquier variable
 *  `(--CLAVE:casing--)` de site-variables.ts (ver ese archivo), no solo el nombre de proyecto de
 *  antes. No importa con que casing haya escrito el lector el valor real (snake_case, kebab-case,
 *  PascalCase, camelCase) -- siempre derivamos las 4 desde la misma lista de palabras normalizada. */
export function deriveCasings(raw: string): CasingVariants {
  const words = wordsFrom(raw);
  return {
    pascal: words.map(capitalize).join(''),
    camel: words.length > 0 ? words[0] + words.slice(1).map(capitalize).join('') : '',
    kebab: words.join('-'),
    snake: words.join('_'),
  };
}
