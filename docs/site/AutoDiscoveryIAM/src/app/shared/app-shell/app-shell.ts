import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatMenuModule } from '@angular/material/menu';
import { CODE_BLOCK_LANGUAGES, CODE_THEME_CATALOG, CodeThemeService } from '@ngx-docs-markdown-kit/parser-md-code-block';
import { SearchableSelectComponent, type SearchableSelectItem } from '@ngx-docs-markdown-kit/ui';
import { NAV, activeSectionFor } from '../../core/nav';
import { ThemeService } from '../../core/theme/theme.service';
import { DesignConfigService } from '../../core/design-config.service';
import { ResourcesConfigService } from '../../core/resources-config.service';
import {
  ALL_LANGUAGES_THEME_ID,
  CodeBlockThemesConfigService,
  buildCodeThemeItems,
  commonThemeFor,
  filterVisibleLanguages,
} from '@frugocorp/parser-md-code-block-themes/config/code-block-themes-config.service';
import { CodeBlockColorsConfigService } from '@frugocorp/parser-md-code-block/config/code-block-colors.config';
import { createResponsiveShellState } from '@frugocorp/create-ngx-docs-site/utils/responsive-shell-state';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, MatSidenavModule, MatMenuModule, SearchableSelectComponent],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // CSS custom properties en la RAIZ de la app -- AppShell envuelve el <router-outlet> completo,
  // asi que heredan a cualquier descendiente (ndmk-code-block/ndmk-card-code-block) sin
  // reinyectarlas en cada componente.
  host: {
    '[style.--code-block-bg]': 'activeCodeBlockColors().codeBlock.backgroundColor',
    '[style.--code-block-border]': 'activeCodeBlockColors().codeBlock.borderColor',
    // ?? null (no "?? undefined"): Angular remueve la propiedad inline por completo con null, asi
    // que sin "sintaxisName"/"textColor" configurados la variable CSS queda SIN DEFINIR en vez de
    // definida-mas-vacia -- el fallback var(--code-block-label-color, var(--color-ink-muted, ...))
    // de code-block.scss solo funciona si la variable no existe en absoluto.
    '[style.--code-block-label-color]': 'activeCodeBlockColors().codeBlock.sintaxisName ?? null',
    '[style.--code-block-label-strong-color]': 'activeCodeBlockColors().codeBlock.textColor ?? null',
    '[style.--card-code-block-bg]': 'activeCodeBlockColors().cardCodeBlock.backgroundColor',
    '[style.--card-code-block-border]': 'activeCodeBlockColors().cardCodeBlock.borderColor',
    '[style.--card-code-block-label-color]': 'activeCodeBlockColors().cardCodeBlock.sintaxisName ?? null',
    '[style.--card-code-block-text-color]': 'activeCodeBlockColors().cardCodeBlock.textColor ?? null',
  },
})
export class AppShell {
  protected readonly theme = inject(ThemeService);
  protected readonly codeTheme = inject(CodeThemeService);
  private readonly codeBlockColors = inject(CodeBlockColorsConfigService).config;
  protected readonly activeCodeBlockColors = computed(() => {
    const mode = this.theme.mode();
    const colors = this.codeBlockColors();
    return { codeBlock: colors.codeBlock[mode], cardCodeBlock: colors.cardCodeBlock[mode] };
  });
  protected readonly resources = inject(ResourcesConfigService).config;
  protected readonly design = inject(DesignConfigService).config;
  protected readonly logo = computed(() => (this.theme.mode() === 'dark' ? this.resources().logoDark : this.resources().logoLight));

  protected readonly sections = NAV;

  // Un solo <ng-template matMenuContent> reutilizado para los N lenguajes (via
  // [matMenuTriggerData], ver app-shell.html) -- cada trigger pasa su lenguaje explicito, sin
  // estado compartido mutable entre "fijar el lenguaje activo" y "abrir el submenu".
  private readonly allCodeLanguages = inject(CODE_BLOCK_LANGUAGES);
  private readonly codeBlockThemesConfig = inject(CodeBlockThemesConfigService).config;
  protected readonly codeLanguages = computed(() => filterVisibleLanguages(this.allCodeLanguages, this.codeBlockThemesConfig()));
  private readonly codeThemeCatalog = inject(CODE_THEME_CATALOG);

  protected codeThemeItems(): readonly SearchableSelectItem[] {
    return buildCodeThemeItems(this.codeThemeCatalog);
  }

  protected readonly allLanguagesThemeId = ALL_LANGUAGES_THEME_ID;

  // Mismo calculo que DocsShell (barra 3), duplicado a proposito: AppShell no depende de DocsShell
  // (capas distintas), cada uno resuelve esto por su cuenta desde la misma funcion pura.
  protected readonly allCodeTheme = computed(() => commonThemeFor(this.codeLanguages(), (lang) => this.codeTheme.themeFor(lang)));
  protected readonly allCodeThemeLabel = computed(() => {
    const theme = this.allCodeTheme();
    return theme === null ? 'Mixto' : this.codeTheme.labelFor(theme);
  });

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

  private readonly shell = createResponsiveShellState();
  protected readonly isMobile = this.shell.isMobile;
  protected readonly sidenavMode = this.shell.sidenavMode;
  protected readonly sidenavOpened = this.shell.sidenavOpened;

  // null en la raiz del sitio (HomePage) -- no "pertenece" a ninguna seccion, ningun icono se resalta.
  protected readonly activeSectionKey = computed(() => activeSectionFor(this.shell.currentUrl(), this.sections)?.key ?? null);
}
