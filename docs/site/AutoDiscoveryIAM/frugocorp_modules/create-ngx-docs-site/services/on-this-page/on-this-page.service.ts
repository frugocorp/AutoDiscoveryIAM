import { Injectable, signal } from '@angular/core';
import type { DocPageHeading } from '@ngx-docs-markdown-kit/parser-md';
import { createCollapsibleBar } from '../../utils/collapsible-bar';

/**
 * Estado del panel "En esta pagina" (barra 3), compartido entre DocPage (que lo llena, uno por
 * cada navegacion) y DocsShell (que lo dibuja) -- DocPage se recicla al navegar entre paginas de
 * una misma seccion (mismo componente, cambian los parametros de ruta), asi que un singleton
 * compartido es mas simple que pasar los datos por @Input/@Output a traves del <router-outlet>.
 */
@Injectable({ providedIn: 'root' })
export class OnThisPageService {
  readonly headings = signal<readonly DocPageHeading[]>([]);
  readonly activeHeadingId = signal<string | null>(null);

  /** Lo llena DocPage (ver hasCodeBlock() en utils/has-code-block.util.ts) -- una pagina con menos
   *  de 2 encabezados pero con al menos un bloque de codigo real igual necesita la barra 3 (ahi
   *  vive el selector de tema de codigo, y el panel "Mis Variables" si el contenido usa alguna). */
  readonly hasCodeBlocks = signal(false);

  readonly bar = createCollapsibleBar('ndmk-onthispage-collapsed', () => this.activeHeadingId());

  /** <base href="/"> hace que <a href="#id"> navegue a "/#id" en vez de "ruta-actual#id"; se
   *  intercepta y navega a mano (scrollIntoView + replaceState) para evitar eso. */
  jumpToHeading(event: MouseEvent, id: string): void {
    if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
      return;
    }

    event.preventDefault();
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    history.replaceState(null, '', `#${id}`);
  }
}
