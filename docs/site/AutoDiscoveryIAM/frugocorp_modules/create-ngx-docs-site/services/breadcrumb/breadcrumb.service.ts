import { Injectable, signal } from '@angular/core';
import type { BreadcrumbStep } from '@site/core/nav';

/**
 * Estado de la miga de pan, compartido entre DocPage (que lo llena) y DocsShell (que lo dibuja
 * como fila real ANTES de ".content-canvas", nunca superpuesta al scroll). Mismo motivo que
 * OnThisPageService: DocPage se recicla al navegar, DocsShell no puede leer sus datos directo.
 * Array vacio = nada que mostrar (ver DocPage.breadcrumb, que ya usa breadcrumbChain() de nav.ts).
 */
@Injectable({ providedIn: 'root' })
export class BreadcrumbService {
  readonly crumb = signal<readonly BreadcrumbStep[]>([]);
}
