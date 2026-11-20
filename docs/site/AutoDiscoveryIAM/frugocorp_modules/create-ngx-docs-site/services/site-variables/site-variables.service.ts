import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { SITE_NAME } from '@site/core/site.config';
import { SITE_VARIABLE_DEFAULTS } from './site-variable-defaults.config.js';
import { ALL_SITE_VARIABLE_GROUPS } from './generated/all-site-variables.generated.js';
import type { SiteVariableGroup } from './site-variables.js';

// Namespaced por SITE_NAME -- redundante con el aislamiento por origen que ya da el navegador
// (2 sitios en dominios distintos JAMAS comparten localStorage), pero gratis de agregar y evita
// cualquier duda si algun dia 2 sitios terminaran sirviendose bajo el mismo dominio.
const STORAGE_KEY = `ndmk-site-vars:${SITE_NAME}`;
const STORAGE_KEY_SHOW_ALL = `ndmk-site-vars-show-all:${SITE_NAME}`;

/** Valores que el lector cargo para las variables `(--CLAVE--)`/`(--CLAVE:casing--)` que el
 *  contenido de este sitio use -- vive SOLO en localStorage de ESTE navegador, nunca se manda a
 *  ningun servidor (mismo criterio que el viejo ProjectNameService al que reemplaza). El render de
 *  SSR siempre ve un Map vacio (isBrowser=false) y muestra los placeholders crudos -- el
 *  reemplazo real pasa recien en el navegador, al hidratar. */
@Injectable({ providedIn: 'root' })
export class SiteVariablesService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  private readonly values = signal<ReadonlyMap<string, string>>(this.readInitial());
  readonly valuesMap = computed(() => this.values());

  /** "valuesMap" + los defaults de ejemplo (ver SITE_VARIABLE_DEFAULTS) para las claves que el
   *  lector todavia no completo -- lo que efectivamente se sustituye en el contenido y se muestra
   *  precargado en cada campo. Un valor real en localStorage SIEMPRE gana sobre su default. */
  readonly effectiveValuesMap = computed(() => {
    const merged = new Map(SITE_VARIABLE_DEFAULTS);
    for (const [base, value] of this.values()) {
      merged.set(base, value);
    }
    return merged;
  });

  /** Grupos detectados en la pagina actualmente renderizada -- lo llena ContentService en cada
   *  carga de pagina (ver applySiteVars ahi), ANTES de sustituir. Vacio = esta pagina no tiene
   *  ninguna variable -- la barra 3 no muestra el panel en absoluto (nada que personalizar aca). */
  readonly discoveredOnCurrentPage = signal<readonly SiteVariableGroup[]>([]);

  /** Toggle "mostrar todas" del panel -- persistido, para no tener que ir pagina por pagina
   *  completando cada variable. ALL_SITE_VARIABLE_GROUPS sale de validate-site-variables.mjs
   *  (mismo escaneo de TODO el sitio que ya corre para la validacion de conflictos, ver ese
   *  script), asi que nunca puede desincronizarse del contenido real. */
  readonly showAllVariables = signal(this.readShowAllInitial());

  /** Grupos que el panel efectivamente dibuja -- los de esta pagina, o los de TODO el sitio si el
   *  toggle esta prendido. Unica fuente que docs-shell necesita leer. */
  readonly displayedGroups = computed(() => (this.showAllVariables() ? ALL_SITE_VARIABLE_GROUPS : this.discoveredOnCurrentPage()));

  setShowAllVariables(value: boolean): void {
    this.showAllVariables.set(value);
    if (!this.isBrowser) {
      return;
    }
    try {
      localStorage.setItem(STORAGE_KEY_SHOW_ALL, value ? '1' : '0');
    } catch {
      // Sin storage disponible -- la preferencia solo dura la sesion.
    }
  }

  private readShowAllInitial(): boolean {
    if (!this.isBrowser) {
      return false;
    }
    try {
      return localStorage.getItem(STORAGE_KEY_SHOW_ALL) === '1';
    } catch {
      return false;
    }
  }

  /** Valor REAL guardado por el lector -- vacio si todavia no escribio nada para esta clave (aunque
   *  exista un default, ver getDefault()). Es lo que se muestra en el "value" del campo: si
   *  devolviera el default aca, "Limpiar" nunca se veria hacer nada (el campo quedaria igual). */
  get(base: string): string {
    return this.values().get(base) ?? '';
  }

  /** Valor de ejemplo (ver SITE_VARIABLE_DEFAULTS) para mostrar como placeholder "fantasma" en un
   *  campo todavia vacio -- nunca se guarda, nunca cuenta como que el lector "ya completo" la
   *  clave. */
  getDefault(base: string): string {
    return SITE_VARIABLE_DEFAULTS.get(base) ?? '';
  }

  set(base: string, value: string): void {
    const trimmed = value.trim();
    const next = new Map(this.values());
    if (trimmed) {
      next.set(base, trimmed);
    } else {
      next.delete(base);
    }
    this.values.set(next);
    this.persist(next);
  }

  /** Limpia TODOS los valores guardados para este sitio -- vuelve a mostrar el contenido con las
   *  claves crudas sin reemplazar, en cualquier pagina. */
  clearAll(): void {
    this.values.set(new Map());
    this.persist(new Map());
  }

  private persist(map: ReadonlyMap<string, string>): void {
    if (!this.isBrowser) {
      return;
    }
    try {
      if (map.size === 0) {
        localStorage.removeItem(STORAGE_KEY);
      } else {
        localStorage.setItem(STORAGE_KEY, JSON.stringify([...map.entries()]));
      }
    } catch {
      // Sin storage disponible (modo privado estricto, etc.) -- el valor solo dura la sesion.
    }
  }

  private readInitial(): ReadonlyMap<string, string> {
    if (!this.isBrowser) {
      return new Map();
    }
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return new Map();
      }
      return new Map(JSON.parse(raw) as [string, string][]);
    } catch {
      return new Map();
    }
  }
}
