import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { CODE_BLOCK_LANGUAGES, CODE_THEME_CATALOG, CodeThemeService } from '@ngx-docs-markdown-kit/parser-md-code-block';
import { SearchableSelectComponent, type SearchableSelectItem } from '@ngx-docs-markdown-kit/ui';
import { NAV, activeSectionFor, navTreeForSection } from '../../../../core/nav';
import { DesignConfigService } from '../../../../core/design-config.service';
import {
  ALL_LANGUAGES_THEME_ID,
  CodeBlockThemesConfigService,
  buildCodeThemeItems,
  commonThemeFor,
  filterVisibleLanguages,
} from '@frugocorp/parser-md-code-block-themes/config/code-block-themes-config.service';
import { BreadcrumbService } from '@frugocorp/create-ngx-docs-site/services/breadcrumb/breadcrumb.service';
import { OnThisPageService } from '@frugocorp/create-ngx-docs-site/services/on-this-page/on-this-page.service';
import { createCollapsibleBar } from '@frugocorp/create-ngx-docs-site/utils/collapsible-bar';
import { createResponsiveShellState } from '@frugocorp/create-ngx-docs-site/utils/responsive-shell-state';
import { stripInlineCode } from '@frugocorp/create-ngx-docs-site/utils/strip-inline-code.util';
import { NavTreeView } from '@frugocorp/create-ngx-docs-site/components/nav-tree/nav-tree';
import { SiteFooter } from '../../../../shared/site-footer/site-footer';
import { SiteHeader } from '../../../../shared/site-header/site-header';
import { SiteVariablesService } from '@frugocorp/create-ngx-docs-site/services/site-variables/site-variables.service';
import type { SiteVariableGroup } from '@frugocorp/create-ngx-docs-site/services/site-variables/site-variables.js';

// Barra 3 vive aca (no dentro de DocPage, que se recicla al navegar entre paginas de una misma
// seccion) para que mostrarla/ocultarla nunca afecte el ancho del articulo/footer -- recibe sus
// encabezados via OnThisPageService, que DocPage llena en cada navegacion.
@Component({
  selector: 'app-docs-shell',
  imports: [RouterLink, RouterOutlet, MatSidenavModule, MatFormFieldModule, MatInputModule, MatSlideToggleModule, NavTreeView, SiteFooter, SiteHeader, SearchableSelectComponent],
  templateUrl: './docs-shell.html',
  styleUrl: './docs-shell.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DocsShell {
  protected readonly onThisPage = inject(OnThisPageService);
  // "heading.text" es markdown crudo (ver parser-md/src/parser.ts) -- "`texto`" en un encabezado
  // original llega con backtick literal, visible aca porque esta barra NO pasa por el HTML ya
  // renderizado de parser-md (bug real encontrado probando el CLI contra un proyecto externo).
  protected readonly stripInlineCode = stripInlineCode;
  protected readonly breadcrumb = inject(BreadcrumbService).crumb;
  protected readonly siteVariables = inject(SiteVariablesService);
  protected readonly codeTheme = inject(CodeThemeService);
  private readonly allCodeLanguages = inject(CODE_BLOCK_LANGUAGES);
  private readonly codeBlockThemesConfig = inject(CodeBlockThemesConfigService).config;
  protected readonly codeLanguages = computed(() => filterVisibleLanguages(this.allCodeLanguages, this.codeBlockThemesConfig()));
  private readonly codeThemeCatalog = inject(CODE_THEME_CATALOG);

  protected codeThemeItems(): readonly SearchableSelectItem[] {
    return buildCodeThemeItems(this.codeThemeCatalog);
  }

  // null = ningun selector inline abierto -- un solo signal, no un Set, porque solo puede haber
  // UNO a la vez (igual que un <mat-menu>). Tambien acepta el sentinel ALL_LANGUAGES_THEME_ID.
  protected readonly expandedThemeLang = signal<string | null>(null);
  protected readonly allLanguagesThemeId = ALL_LANGUAGES_THEME_ID;

  protected readonly allCodeTheme = computed(() => commonThemeFor(this.codeLanguages(), (lang) => this.codeTheme.themeFor(lang)));
  protected readonly allCodeThemeLabel = computed(() => {
    const theme = this.allCodeTheme();
    return theme === null ? 'Mixto' : this.codeTheme.labelFor(theme);
  });

  protected toggleThemeInline(lang: string): void {
    this.expandedThemeLang.update((current) => (current === lang ? null : lang));
  }

  protected setCodeTheme(lang: string, theme: string): void {
    if (lang === ALL_LANGUAGES_THEME_ID) {
      this.codeTheme.setThemeForLanguages(
        this.codeLanguages().map((entry) => entry.lang),
        theme,
      );
      return;
    }
    this.codeTheme.setTheme(lang, theme);
  }

  protected resetAllCodeThemes(): void {
    this.codeTheme.resetAll();
  }

  protected readonly design = inject(DesignConfigService).config;
  protected readonly sections = NAV;

  private readonly shell = createResponsiveShellState();
  protected readonly isMobile = this.shell.isMobile;
  protected readonly sidenavMode = this.shell.sidenavMode;
  protected readonly sidenavOpened = this.shell.sidenavOpened;

  // null en una URL sin seccion (ej. 404) -- la raiz del sitio ya no llega aca, vive en HomePage.
  protected readonly activeSection = computed(() => activeSectionFor(this.shell.currentUrl(), this.sections));
  protected readonly navTree = computed(() => {
    const section = this.activeSection();
    return section ? navTreeForSection(section.key) : { items: [], groups: [] };
  });

  // Menos de 2 items de nivel superior seria un menu vacio o inutil (ej. /privacy-es, sin seccion
  // activa) -- DESIGN_CONFIG sigue pudiendo apagarla del todo independientemente de esto.
  protected readonly showSecondaryBar = computed(() => {
    const tree = this.navTree();
    return this.design().showSecondaryBar && tree.items.length + tree.groups.length >= 2;
  });

  protected readonly bar = createCollapsibleBar('ndmk-secondary-collapsed', () => this.shell.currentUrl());

  // Menos de 2 encabezados YA NO alcanza para ocultarla del todo: una pagina con 1 sola seccion
  // pero con un bloque de codigo real (selector de tema) o una variable "(--CLAVE--)" (panel "Mis
  // Variables") igual necesita la barra 3 -- si no, esas paginas nunca podrian mostrar ninguna de
  // las 2 cosas. hasSiteVariables se declara mas abajo en esta clase; referenciarla aca funciona
  // igual porque este computed() la lee de forma perezosa, recien cuando efectivamente corre.
  protected readonly showOnThisPage = computed(
    () => this.design().showOnThisPage && (this.onThisPage.headings().length >= 2 || this.onThisPage.hasCodeBlocks() || this.hasSiteVariables()),
  );

  // Relativo al heading MINIMO de la pagina (no al nivel absoluto), para que una pagina que
  // arranca en ### no quede con sangria inicial desperdiciada.
  private readonly minHeadingLevel = computed(() => Math.min(...this.onThisPage.headings().map((heading) => heading.level)));

  protected headingIndentPx(level: number): number {
    return (level - this.minHeadingLevel()) * 12;
  }

  // Grupos de esta pagina (ver ContentService.applySiteVars), o de TODO el sitio si el toggle
  // "mostrar todas" esta prendido -- vacio = ni esta pagina ni (si aplica) el sitio entero usan
  // ninguna variable "(--CLAVE--)", el panel entero no se muestra (mismo criterio que
  // showSecondaryBar/showOnThisPage: nada de mostrar un panel vacio o sin sentido).
  protected readonly siteVariableGroups = this.siteVariables.displayedGroups;
  protected readonly hasSiteVariables = computed(() => this.siteVariableGroups().length > 0);
  protected readonly showAllSiteVariables = this.siteVariables.showAllVariables;

  protected trackSiteVariableGroup(_index: number, group: SiteVariableGroup): string {
    return group.base;
  }

  protected siteVariableValue(base: string): string {
    return this.siteVariables.get(base);
  }

  protected siteVariableDefault(base: string): string {
    return this.siteVariables.getDefault(base);
  }

  protected setShowAllSiteVariables(value: boolean): void {
    this.siteVariables.setShowAllVariables(value);
  }

  protected setSiteVariable(base: string, value: string): void {
    this.siteVariables.set(base, value);
  }

  protected clearAllSiteVariables(): void {
    this.siteVariables.clearAll();
  }
}
