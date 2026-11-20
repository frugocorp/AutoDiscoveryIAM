import { Signal, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { BreakpointObserver } from '@angular/cdk/layout';
import { filter, map, startWith } from 'rxjs';

const MOBILE_BREAKPOINT = '(max-width: 900px)';

export interface ResponsiveShellState {
  readonly currentUrl: Signal<string>;
  readonly isMobile: Signal<boolean>;
  readonly sidenavMode: Signal<'over' | 'side'>;
  readonly sidenavOpened: Signal<boolean>;
}

/**
 * Estado responsivo compartido por las barras 1 y 2 (AppShell/DocsShell) -- URL actual, si
 * estamos en movil, modo/apertura del drawer. Se llama desde el constructor/inicializador de
 * campo del componente dueño (usa inject()).
 */
export function createResponsiveShellState(): ResponsiveShellState {
  const router = inject(Router);
  const breakpointObserver = inject(BreakpointObserver);

  const currentUrl = toSignal(
    router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
      startWith(router.url),
    ),
    { initialValue: router.url },
  );

  const isMobile = toSignal(
    breakpointObserver.observe(MOBILE_BREAKPOINT).pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  return {
    currentUrl,
    isMobile,
    sidenavMode: computed(() => (isMobile() ? 'over' : 'side')),
    sidenavOpened: computed(() => !isMobile()),
  };
}
