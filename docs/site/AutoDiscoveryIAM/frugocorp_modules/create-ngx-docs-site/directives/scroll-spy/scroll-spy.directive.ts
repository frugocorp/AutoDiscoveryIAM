import { Directive, ElementRef, Injector, PLATFORM_ID, afterNextRender, effect, inject, input, output } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export interface ScrollSpySection {
  /** Id global de la seccion ("{pagina}-{seccion}", el mismo que "<section id>") -- para un
   *  consumidor que necesite distinguir la misma seccion de paginas distintas (ej. un menu de
   *  todo el sitio). */
  readonly id: string;
  /** Id local (el mismo que ya usan los saltos por fragmento "#id"). */
  readonly headingId: string;
}

/**
 * Aplicada sobre el <article> de una pagina de docs -- observa sus ".page-section" (ver
 * DocSegments) y emite (sectionChange) con la que esta activa segun scroll: la ULTIMA cuyo borde
 * superior ya cruzo la linea de activacion (altura real de ".site-header" + altura real de
 * "<nav aria-label='Miga de pan'>" + 10px, medida en vivo -- se adapta sola si un equipo cambia el
 * alto del header). La directiva no sabe ni le importa que hace el consumidor con el evento.
 * `ndmkScrollSpy` (el valor del binding) es la "llave" que fuerza un reescaneo -- tiene que ser el
 * CONTENIDO real ya calculado (ver DocPage.segments()), no el slug de la ruta: el slug cambia
 * apenas arranca la navegacion, ANTES de que el fetch async del cuerpo termine.
 */
@Directive({ selector: '[ndmkScrollSpy]' })
export class ScrollSpyDirective {
  private readonly host = inject(ElementRef).nativeElement as HTMLElement;
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly injector = inject(Injector);

  readonly ndmkScrollSpy = input<unknown>();
  readonly sectionChange = output<ScrollSpySection>();

  private cleanup?: () => void;

  constructor() {
    effect(() => {
      this.ndmkScrollSpy();
      afterNextRender(() => this.reset(), { injector: this.injector });
    });
  }

  private reset(): void {
    this.cleanup?.();
    this.cleanup = undefined;

    if (!this.isBrowser) {
      return;
    }

    const sections = Array.from(this.host.querySelectorAll<HTMLElement>('.page-section'));

    const fragmentId = location.hash ? decodeURIComponent(location.hash.slice(1)) : '';
    const target = fragmentId ? this.host.querySelector<HTMLElement>(`[id="${fragmentId}"]`) : null;
    const scrollRoot = this.host.closest<HTMLElement>('.content-canvas');

    if (target) {
      target.scrollIntoView({ block: 'start' });
      this.emit(target.closest<HTMLElement>('.page-section') ?? target);
    } else {
      scrollRoot?.scrollTo({ top: 0 });
      if (sections.length > 0) {
        this.emit(sections[0]);
      }
    }

    if (sections.length === 0) {
      return;
    }

    const updateActive = (): void => {
      const line = this.activationLine();
      let active = sections[0];
      for (const el of sections) {
        if (el.getBoundingClientRect().top <= line) {
          active = el;
        }
      }
      this.emit(active);
    };

    // "scroll" no burbujea -- capture:true en document lo captura igual sin depender de acertar
    // cual elemento en particular recibe el evento.
    document.addEventListener('scroll', updateActive, { capture: true, passive: true });
    this.cleanup = () => document.removeEventListener('scroll', updateActive, { capture: true });
  }

  private activationLine(): number {
    const header = document.querySelector<HTMLElement>('.site-header');
    const breadcrumb = document.querySelector<HTMLElement>('[aria-label="Miga de pan"]');
    const headerHeight = header?.getBoundingClientRect().height ?? 0;
    const breadcrumbHeight = breadcrumb?.getBoundingClientRect().height ?? 0;
    return headerHeight + breadcrumbHeight + 10;
  }

  private emit(active: HTMLElement): void {
    this.sectionChange.emit({ id: active.id, headingId: active.dataset['headingId'] ?? active.id });
  }
}
