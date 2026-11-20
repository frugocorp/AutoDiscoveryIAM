import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, effect, inject, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

const STORAGE_KEY = 'ndmk-theme';

function readInitialMode(document: Document, isBrowser: boolean): ThemeMode {
  if (!isBrowser) {
    // SSR/prerender no tiene localStorage ni matchMedia para saber la preferencia real del
    // visitante -- "dark" es el default elegido del sitio (no una deteccion), consistente con lo
    // que la mayoria de los visitantes termina viendo tras el script inline de index.html. Sin
    // relacion con SEO real (data-theme no es contenido indexable), solo evita que la marca
    // "light" quede pisada en el HTML servido/prerenderizado antes de que el JS del cliente corrija.
    return 'dark';
  }
  try {
    const stored = document.defaultView?.localStorage.getItem(STORAGE_KEY);
    if (stored === 'light' || stored === 'dark') {
      return stored;
    }
  } catch {
    // localStorage bloqueado (modo privado estricto, etc.) -- cae al matchMedia de abajo.
  }
  return document.defaultView?.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

/** Tema claro/oscuro del sitio (independiente del tema de resaltado de codigo por lenguaje, ver
 *  CodeThemeService en @ngx-docs-markdown-kit/parser-md-code-block) -- persistido en localStorage,
 *  aplicado como "data-theme" en <html> (ver styles.css, `:root[data-theme='dark']`). El script
 *  inline de index.html duplica esta misma logica para evitar un parpadeo de tema incorrecto antes
 *  de que Angular arranque -- si cambias STORAGE_KEY aqui, cambialo tambien ahi. */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly mode = signal<ThemeMode>(readInitialMode(this.document, this.isBrowser));

  constructor() {
    effect(() => {
      const mode = this.mode();
      this.document.documentElement.setAttribute('data-theme', mode);
      if (this.isBrowser) {
        try {
          this.document.defaultView?.localStorage.setItem(STORAGE_KEY, mode);
        } catch {
          // Igual que arriba -- sin storage disponible, el modo solo dura la sesion actual.
        }
      }
    });
  }

  toggle(): void {
    this.mode.update((current) => (current === 'light' ? 'dark' : 'light'));
  }
}
