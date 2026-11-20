import { ChangeDetectionStrategy, Component, ViewEncapsulation, computed, effect, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import type { DocPageContent } from '@ngx-docs-markdown-kit/parser-md';
import { JsonLdService, PageSeoService, SeoConfigService } from '@ngx-docs-markdown-kit/parser-md-seo';
import { ContentService } from '@frugocorp/create-ngx-docs-site/services/content/content.service';
import { DocSegments } from '@frugocorp/create-ngx-docs-site/components/doc-segments/doc-segments';
import { findLastSectionId } from '@frugocorp/create-ngx-docs-site/utils/last-section.util';
import type { AppSegment } from '@frugocorp/create-ngx-docs-site/utils/segment-types';
import { NAV, findPage } from '../../core/nav';
import { ResourcesConfigService } from '../../core/resources-config.service';
import { ThemeService } from '../../core/theme/theme.service';
import { SITE_NAME } from '../../core/site.config';

const EMPTY_CONTENT: DocPageContent = { segments: [], headings: [], meta: {} };

/**
 * Pagina raiz del sitio ("/") -- NO vive dentro de DocsShell (ver app.routes.ts): sin barra 2 ni
 * barra 3, solo la barra 1 (de AppShell, un nivel arriba) + esta portada. El cuerpo (logo aparte)
 * sale de "public/content-parser-md/home.md" -- misma pagina real del manifiesto que copia
 * generate-explore-pages.mjs hacia "explore/<pkg>.md" en los demas sitios del ecosistema, por eso
 * el contenido tiene que quedar bien tanto solo como citado desde otro sitio.
 */
@Component({
  selector: 'app-home-page',
  imports: [RouterLink, DocSegments],
  templateUrl: './home-page.html',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage {
  private readonly content = inject(ContentService);
  private readonly pageSeo = inject(PageSeoService);
  private readonly jsonLd = inject(JsonLdService);
  private readonly seoConfig = inject(SeoConfigService).config;
  protected readonly theme = inject(ThemeService);
  protected readonly resources = inject(ResourcesConfigService).config;
  protected readonly siteName = SITE_NAME;

  // logoLight/logoDark (ver ResourcesConfig) -- mismo criterio que AppShell: un logo pensado para
  // fondo claro se pierde sobre uno oscuro y viceversa.
  protected readonly logo = computed(() => (this.theme.mode() === 'dark' ? this.resources().logoDark : this.resources().logoLight));

  // Primera seccion real del sitio -- destino del boton "Ver documentacion" (su propia pagina via
  // "content_dir.md", con Cards a sus paginas/subgrupos -- ya no un "primer item" adivinado). NAV
  // siempre trae al menos 1 seccion (ver el propio manifiesto en nav.ts).
  protected readonly firstSectionKey = NAV[0].key;

  private readonly currentPage = computed(() => findPage(['home']));
  private readonly page = toSignal(this.content.getPage(['home']), { initialValue: EMPTY_CONTENT });
  protected readonly segments = computed(() => this.page().segments as unknown as readonly AppSegment[]);
  protected readonly lastSectionId = computed(() => findLastSectionId(this.segments()));

  constructor() {
    effect(() => {
      const config = this.seoConfig();
      const meta = this.currentPage()?.meta ?? {};
      const asString = (key: string): string | undefined => meta[key] || undefined;

      const title = asString('title') ?? SITE_NAME;
      const description = asString('description') ?? (config.description || undefined);

      // "slug: 'index'" (nunca "home") -- PageSeoService lo trata especial, canonical queda en la
      // raiz del sitio ("/"), no en "/index".
      const applied = this.pageSeo.apply({
        title,
        description,
        keywords: asString('keywords') ?? (config.keywords || undefined),
        image: asString('image'),
        canonical: asString('canonical'),
        robots: asString('robots'),
        siteUrl: config.siteUrl,
        slug: 'index',
      });

      this.jsonLd.apply({
        title,
        description,
        canonicalUrl: applied.canonicalUrl,
        image: applied.image,
        breadcrumb: applied.canonicalUrl ? [{ name: title, url: applied.canonicalUrl }] : [],
      });
    });
  }
}
