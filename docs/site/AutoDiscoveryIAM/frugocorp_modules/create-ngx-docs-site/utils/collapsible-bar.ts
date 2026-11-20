import { PLATFORM_ID, Signal, afterNextRender, effect, inject, signal, untracked } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export interface CollapsibleBar {
  readonly collapsed: Signal<boolean>;
  /** Pulso de 1s cuando cambia el item activo MIENTRAS la barra esta colapsada -- recuerda que
   *  sigue ahi (ver la animacion "bar-glow" en cada .css que la consume). */
  readonly glow: Signal<boolean>;
  toggle(): void;
}

/**
 * Estado de una barra colapsable (secundaria o "En esta pagina"): colapsado/expandido persistido
 * en localStorage por separado para cada una, mas el pulso de brillo. "watch" marca "cambio el
 * item activo" (la URL para la barra 2, activeHeadingId para la barra 3) -- se llama desde el
 * constructor/inicializador de campo del componente dueño (usa inject()/effect()).
 *
 * "collapsed" arranca SIEMPRE en false (nunca lee localStorage en la construccion): ese valor
 * controla que rama de un @if/@else se pinta (arbol de navegacion completo vs. un solo boton), y
 * el prerenderizado no puede saber la preferencia real -- si el cliente arrancara ya en "true"
 * (con la barra colapsada guardada de una sesion anterior), quedaria en conflicto directo con el
 * HTML ya hidratado del servidor (que siempre asume expandido), algo que Angular no puede
 * reconciliar con un parche: fuerza un reemplazo completo de esa rama, visible como que el menu
 * "correcto" aparece un instante y luego desaparece. La preferencia real se aplica recien despues
 * del primer render (afterNextRender), cuando ya no hay nada que hidratar.
 */
export function createCollapsibleBar(storageKey: string, watch: () => unknown): CollapsibleBar {
  const isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  const collapsed = signal(false);
  const glow = signal(false);
  let isFirstRun = true;

  if (isBrowser) {
    afterNextRender(() => {
      if (localStorage.getItem(storageKey) === '1') {
        collapsed.set(true);
      }
    });
  }

  effect(() => {
    watch();

    if (isFirstRun) {
      isFirstRun = false;
      return;
    }

    // untracked: el pulso es solo por cambio de item activo, no por colapsar/expandir a mano --
    // leer "collapsed" sin untracked() haria que el propio toggle() re-disparara el efecto.
    if (!untracked(collapsed)) {
      return;
    }

    glow.set(true);
    setTimeout(() => glow.set(false), 1000);
  });

  return {
    collapsed,
    glow,
    toggle(): void {
      collapsed.update((value) => !value);
      if (isBrowser) {
        localStorage.setItem(storageKey, collapsed() ? '1' : '0');
      }
    },
  };
}
