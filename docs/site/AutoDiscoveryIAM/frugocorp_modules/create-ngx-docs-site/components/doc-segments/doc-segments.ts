import { ChangeDetectionStrategy, Component, input, inject } from '@angular/core';
import { DomSanitizer, type SafeHtml } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { CodeBlockComponent, CardCodeBlockComponent } from '@ngx-docs-markdown-kit/parser-md-code-block';
import { ImageComponent } from '@ngx-docs-markdown-kit/parser-md-image';
import { ImageConfigService } from '@frugocorp/parser-md-image/config/image-config.service';
import { ContentCardComponent } from '@ngx-docs-markdown-kit/parser-md-card';
import { FileEmbedComponent } from '@ngx-docs-markdown-kit/parser-md-file-embed';
import type { AppSegment } from '@frugocorp/create-ngx-docs-site/utils/segment-types';

/** Renderiza un arreglo de segmentos con un @switch por "type" -- "list" es recursivo (un
 *  DocSegments dentro de otro), igual que una lista con codigo anidado que parser-md ya resolvio
 *  en segments por item. */
@Component({
  selector: 'app-doc-segments',
  imports: [
    RouterLink,
    MatCardModule,
    CodeBlockComponent,
    CardCodeBlockComponent,
    ImageComponent,
    ContentCardComponent,
    FileEmbedComponent,
    DocSegments,
  ],
  templateUrl: './doc-segments.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DocSegments {
  readonly segments = input.required<readonly AppSegment[]>();
  /** Path COMPLETO de la pagina (ej. "docs-examples-sintaxis", ya unido con "-") -- se propaga sin
   *  cambios a cada nivel recursivo, lo necesita el "section" (id/clase con el prefijo de pagina,
   *  ver doc-segments.html) para que el id sea GLOBALMENTE unico en todo el sitio, no lo calcula
   *  este componente. */
  readonly pagePath = input.required<string>();
  /** Id de la seccion visualmente ULTIMA de la pagina (ver last-section.util.ts) -- se propaga sin
   *  cambios a cada nivel recursivo, para poder marcarla con ".page-section-last" sin importar cuan
   *  anidada este. "null" si la pagina no tiene ninguna seccion. */
  readonly lastSectionId = input.required<string | null>();

  private readonly sanitizer = inject(DomSanitizer);
  protected readonly imageConfig = inject(ImageConfigService).config;

  protected trust(html: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }

  protected asSegments(items: unknown): readonly AppSegment[] {
    return items as readonly AppSegment[];
  }
}
