import { Injectable, signal } from '@angular/core';
import {
  DEFAULT_THEME,
  type CodeBlockLanguage,
  type CodeTheme,
  type CodeThemeCatalogEntry,
} from '@ngx-docs-markdown-kit/parser-md-code-block';
import type { SearchableSelectItem } from '@ngx-docs-markdown-kit/ui';

export interface CodeBlockThemesConfig {
  /** Lenguajes a mostrar en el engrane de "Estilo de codigo" (AppShell) y en el resumen "Temas de
   *  codigo" (barra 3, DocsShell) -- null muestra todos los que CODE_BLOCK_LANGUAGES trae
   *  registrados (el default). Recortala si tu sitio solo usa un par de lenguajes y el resto solo
   *  ensucia el menu. */
  readonly visibleLanguages: readonly string[] | null;
}

export const CODE_BLOCK_THEMES_CONFIG: CodeBlockThemesConfig = { visibleLanguages: ["csharp","bash","powershell","json"] };

@Injectable({ providedIn: 'root' })
export class CodeBlockThemesConfigService {
  readonly config = signal(CODE_BLOCK_THEMES_CONFIG).asReadonly();
}

/** Aplica "visibleLanguages" del config a la lista completa de CODE_BLOCK_LANGUAGES -- compartida
 *  entre AppShell (engrane) y DocsShell (resumen "Temas de codigo") para que ambas nunca puedan
 *  mostrar listas distintas por una copia del filtro desincronizada. */
export function filterVisibleLanguages(
  languages: readonly CodeBlockLanguage[],
  config: CodeBlockThemesConfig,
): readonly CodeBlockLanguage[] {
  if (!config.visibleLanguages) {
    return languages;
  }
  const visible = new Set(config.visibleLanguages);
  return languages.filter((entry) => visible.has(entry.lang));
}

/** "Default" + el catalogo completo, en el formato que <ndmk-searchable-select> espera --
 *  compartida entre AppShell (engrane) y DocsShell (resumen "Temas de codigo" en barra 3) para que
 *  ambas muestren exactamente las mismas opciones. */
export function buildCodeThemeItems(catalog: readonly CodeThemeCatalogEntry[]): readonly SearchableSelectItem[] {
  return [{ id: DEFAULT_THEME, label: 'Default' }, ...catalog.map((entry) => ({ id: entry.id, label: entry.label }))];
}

/** Id de la fila pseudo-lenguaje "All" (elegir un tema para TODOS los lenguajes visibles de un
 *  solo golpe) -- nunca es un lenguaje real de CODE_BLOCK_LANGUAGES, asi que sirve como sentinel
 *  para distinguirla de un lenguaje autentico en el mismo selector inline (ver docs-shell.ts/
 *  app-shell.ts, que reusan el mismo mat-menu/fila para ambos casos). */
export const ALL_LANGUAGES_THEME_ID = '__all__';

/** Tema comun a TODOS los lenguajes visibles, o null si no coinciden -- "All" no guarda su propio
 *  estado, se DERIVA de CodeThemeService.themes() en cada lectura: si el usuario cambia un
 *  lenguaje individual despues de elegir "All", esa lectura deja de coincidir para todos y "All"
 *  automaticamente vuelve a null (sin valor resaltado), que es justo el comportamiento pedido. */
export function commonThemeFor(languages: readonly CodeBlockLanguage[], themeFor: (lang: string) => CodeTheme): CodeTheme | null {
  if (languages.length === 0) {
    return null;
  }
  const first = themeFor(languages[0].lang);
  return languages.every((entry) => themeFor(entry.lang) === first) ? first : null;
}
