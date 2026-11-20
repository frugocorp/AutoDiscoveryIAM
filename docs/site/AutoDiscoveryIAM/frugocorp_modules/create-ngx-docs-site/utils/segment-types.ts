import type { CardsSegment, HtmlSegment, ListSegment, SectionSegment } from '@ngx-docs-markdown-kit/parser-md';
import type { CardCodeBlockSegment, CodeSegment } from '@ngx-docs-markdown-kit/parser-md-code-block';
import type { ImageSegment } from '@ngx-docs-markdown-kit/parser-md-image';
import type { ContentCardSegment } from '@ngx-docs-markdown-kit/parser-md-card';
import type { FileEmbedSegment } from '@ngx-docs-markdown-kit/parser-md-file-embed';

/** Union concreta de los tipos de segmento que este sitio sabe renderizar -- permite que @switch
 *  en doc-segments.html angoste el tipo por "type", cosa que no pasa con el DocPageSegment
 *  generico (solo declara "type: string"). Si agregas otra extension con su propio tipo de
 *  segmento, sumalo aqui tambien. */
export type AppSegment =
  | HtmlSegment
  | ListSegment
  | SectionSegment
  | CardsSegment
  | CodeSegment
  | CardCodeBlockSegment
  | ImageSegment
  | ContentCardSegment
  | FileEmbedSegment
  ;
