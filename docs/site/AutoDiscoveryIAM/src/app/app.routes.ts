import type { Routes } from '@angular/router';
import { AppShell } from './shared/app-shell/app-shell';
import { DocsShell } from './bounded-contexts/content-parser-md/shared/docs-shell/docs-shell';
import { DocPage } from './bounded-contexts/content-parser-md/features/doc-page/doc-page';
import { HomePage } from './shared/home-page/home-page';
import { StatusPage } from './shared/status-page/status-page';

export const routes: Routes = [
  {
    path: '',
    component: AppShell,
    children: [
      // La raiz del sitio ("/") es HomePage, nunca dentro de DocsShell -- solo la barra 1.
      { path: '', component: HomePage, pathMatch: 'full' },
      // Ejemplo de ruta propia del equipo dev, ajena al sistema de contenido -- ver StatusPage.
      { path: 'status', component: StatusPage },
      // Wildcard generico: DocPage resuelve el path COMPLETO (route.url) contra el manifiesto --
      // sirve tanto para una pagina hoja real como para el "content_dir.md" de cualquier
      // seccion/grupo, sin ninguna ruta hardcodeada por nombre (ver core/nav.ts). Va AL FINAL para
      // no ganarle a las rutas de arriba.
      {
        path: '**',
        component: DocsShell,
        children: [{ path: '', component: DocPage }],
      },
    ],
  },
];
