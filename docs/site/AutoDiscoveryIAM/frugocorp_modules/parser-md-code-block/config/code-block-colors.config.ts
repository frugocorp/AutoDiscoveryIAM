import { Injectable, signal } from '@angular/core';

export interface CodeBlockColorTheme {
  readonly backgroundColor: string;
  readonly borderColor: string;
  /** Color de la etiqueta (prefijo "Sintaxis:"/"Resultado", muted/uppercase) -- opcional, sin
   *  especificar sigue `--color-ink-muted` (automatico con el tema del sitio, no con
   *  `backgroundColor`; un fondo inusual puede volver ese automatismo illegible). */
  readonly sintaxisName?: string;
  /** Texto mas destacado (nombre del lenguaje, descripcion de la tarjeta) -- sigue `--color-ink`
   *  si no se especifica. */
  readonly textColor?: string;
}

export interface CodeBlockColorPair {
  readonly light: CodeBlockColorTheme;
  readonly dark: CodeBlockColorTheme;
}

export interface CodeBlockColorsConfig {
  /** Bloque de codigo (```code-block```) -- tambien el comando EMBEBIDO dentro de una tarjeta
   *  (```card-code-block```), que es un <ndmk-code-block> real con su propio fondo/borde. El
   *  "resultado esperado" de la tarjeta es la excepcion: visualmente es parte del cuerpo de la
   *  tarjeta, usa cardCodeBlock. */
  readonly codeBlock: CodeBlockColorPair;
  /** Shell de una tarjeta de comando (```card-code-block```) -- el comando que trae adentro usa
   *  codeBlock, no este. */
  readonly cardCodeBlock: CodeBlockColorPair;
}

// Cada componente tiene su propia pareja fondo/borde (incluso un code-block EMBEBIDO en una
// tarjeta), cada una con su propia variante claro/oscuro. Edita directamente (requiere
// recompilar), no un JSON de public/ -- mismo patron sincrono que el resto del config del sitio.
export const CODE_BLOCK_COLORS_CONFIG: CodeBlockColorsConfig = {
  codeBlock: {
    dark: { backgroundColor: '#303030', borderColor: '#1f387e', sintaxisName: '#9fb3c8', textColor: '#ffffff' },
    light: { backgroundColor: '#303030', borderColor: '#1f387e', sintaxisName: '#9fb3c8', textColor: '#ffffff' },
  },
  cardCodeBlock: {
    dark: { backgroundColor: '#202020', borderColor: '#1f387e', sintaxisName: '#9fb3c8', textColor: '#ffffff' },
    light: { backgroundColor: '#202020', borderColor: '#1f387e', sintaxisName: '#9fb3c8', textColor: '#ffffff' },
  },
};

@Injectable({ providedIn: 'root' })
export class CodeBlockColorsConfigService {
  readonly config = signal(CODE_BLOCK_COLORS_CONFIG).asReadonly();
}
