import { ChangeDetectionStrategy, Component, ViewEncapsulation, computed, effect, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { combineLatest, switchMap } from 'rxjs';
import type { DocPageContent } from '@ngx-docs-markdown-kit/parser-md';
import { JsonLdService, PageSeoService, SeoConfigService } from '@ngx-docs-markdown-kit/parser-md-seo';
import { ContentService } from '@frugocorp/create-ngx-docs-site/services/content/content.service';
import { DocSegments } from '@frugocorp/create-ngx-docs-site/components/doc-segments/doc-segments';
import { ScrollSpyDirective } from '@frugocorp/create-ngx-docs-site/directives/scroll-spy/scroll-spy.directive';
import { buildJsonLdBreadcrumb } from '@frugocorp/create-ngx-docs-site/utils/breadcrumb-path';
import { breadcrumbChain, findPage } from '../../../../core/nav';
import { BreadcrumbService } from '@frugocorp/create-ngx-docs-site/services/breadcrumb/breadcrumb.service';
import { OnThisPageService } from '@frugocorp/create-ngx-docs-site/services/on-this-page/on-this-page.service';
import { SiteVariablesService } from '@frugocorp/create-ngx-docs-site/services/site-variables/site-variables.service';
import type { AppSegment } from '@frugocorp/create-ngx-docs-site/utils/segment-types';
import { findLastSectionId } from '@frugocorp/create-ngx-docs-site/utils/last-section.util';
import { hasCodeBlock } from '@frugocorp/create-ngx-docs-site/utils/has-code-block.util';

const EMPTY_CONTENT: DocPageContent = { segments: [], headings: [], meta: {} };

/**
 * Renderiza CUALQUIER pagina de contenido -- una pagina hoja real, o la propia (via
 * "content_dir.md") de un grupo/seccion (ver GroupIndexPage, que ya NO existe como componente
 * aparte: content_dir.md hace que sea exactamente el mismo caso que cualquier otra pagina). Resuelve
 * el path COMPLETO de la ruta (route.url, no un ":slug" plano -- ver app.routes.ts), pide el HTML
 * del cuerpo a ContentService, y delega el pintado a <app-doc-segments>. Nav/headings/meta de SEO NO
 * esperan ese fetch -- salen sync del manifiesto generado (ver core/nav.ts). ViewEncapsulation.None:
 * DocSegments inyecta HTML crudo via [innerHTML], la encapsulacion emulada (el default) no le
 * aplicaria.
 */
@Component({
  selector: 'app-doc-page',
  imports: [DocSegments, ScrollSpyDirective],
  templateUrl: './doc-page.html',
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DocPage {
  private readonly route = inject(ActivatedRoute);
  private readonly content = inject(ContentService);
  private readonly onThisPage = inject(OnThisPageService);
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly siteVariables = inject(SiteVariablesService);
  private readonly pageSeo = inject(PageSeoService);
  private readonly seoConfig = inject(SeoConfigService).config;
  private readonly jsonLd = inject(JsonLdService);

  // El "**" de app.routes.ts vive en la ruta de DocsShell (el padre) -- DocPage cuelga de un hijo
  // "path: ''" que no matchea ningun segmento propio, asi que route.url (el de ESTE componente)
  // siempre viene vacio. Los segmentos reales del wildcard estan en route.parent.url.
  private readonly parentUrl = this.route.parent?.url ?? this.route.url;
  private readonly urlSegments = toSignal(this.parentUrl, { initialValue: [] });

  // El "**" de app.routes.ts entrega el path COMPLETO via route.parent.url -- nunca un ":slug" plano.
  protected readonly currentPath = computed(() => this.urlSegments().map((segment) => segment.path));
  protected readonly pagePath = computed(() => this.currentPath().join('-'));
  private readonly currentPage = computed(() => findPage(this.currentPath()));

  // Un paso por cada nivel ancestro del path (siempre incluye la pagina actual, la ultima) --
  // misma fuente que el BreadcrumbList de JSON-LD, ver breadcrumb-path.ts.
  protected readonly breadcrumb = computed(() => breadcrumbChain(this.currentPath()));

  // Se recalcula al navegar y al guardar/limpiar cualquier variable "(--CLAVE--)" -- el
  // placeholder -> valor real se ve reflejado sin recargar la pagina.
  protected readonly page = toSignal(
    combineLatest([this.parentUrl, toObservable(this.siteVariables.valuesMap)]).pipe(
      switchMap(([urlSegments]) => this.content.getPage(urlSegments.map((segment) => segment.path))),
    ),
    { initialValue: EMPTY_CONTENT },
  );

  protected readonly segments = computed(() => this.page().segments as unknown as readonly AppSegment[]);
  // Id de la seccion visualmente ULTIMA (la mas anidada, no solo el H1 exterior) -- ver
  // last-section.util.ts sobre por que hace falta calcularlo en TS en vez de CSS puro.
  protected readonly lastSectionId = computed(() => findLastSectionId(this.segments()));

  constructor() {
    effect(() => this.onThisPage.headings.set(this.currentPage()?.headings ?? []));
    effect(() => this.onThisPage.hasCodeBlocks.set(hasCodeBlock(this.segments())));
    effect(() => this.breadcrumbService.crumb.set(this.breadcrumb()));

    effect(() => {
      const config = this.seoConfig();
      const path = this.currentPath();
      const meta = this.currentPage()?.meta ?? {};
      const asString = (key: string): string | undefined => meta[key] || undefined;

      const title = asString('title') ?? this.breadcrumb().at(-1)?.title ?? path.at(-1) ?? '';
      const description = asString('description') ?? (config.description || undefined);
      const keywords = asString('keywords') ?? (config.keywords || undefined);

      const applied = this.pageSeo.apply({
        title,
        description,
        keywords,
        image: asString('image'),
        ogTitle: asString('ogTitle'),
        ogDescription: asString('ogDescription'),
        twitterCard: asString('twitterCard'),
        canonical: asString('canonical'),
        robots: asString('robots'),
        siteUrl: config.siteUrl,
        slug: path.join('/'),
      });

      this.jsonLd.apply({
        title,
        description,
        canonicalUrl: applied.canonicalUrl,
        image: applied.image,
        breadcrumb: buildJsonLdBreadcrumb(config.siteUrl, path),
      });
    });
  }

  protected onSectionChange(section: { readonly id: string; readonly headingId: string }): void {
    this.onThisPage.activeHeadingId.set(section.headingId);
  }
}
