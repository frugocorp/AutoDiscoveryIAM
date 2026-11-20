import { RenderMode, ServerRoute } from '@angular/ssr';
import { allPaths } from './core/nav';

export const serverRoutes: ServerRoute[] = [
  {
    path: 'status',
    renderMode: RenderMode.Prerender,
  },
  {
    // Cada path real del manifiesto (con seccion o sin ella, ej. paginas "nav: false") se vuelve
    // HTML estatico en build -- el crawler recibe la pagina ya renderizada, nunca el shell vacio
    // de un SPA. Un .md nuevo (o una carpeta nueva) entra solo con el proximo build, sin tocar
    // este archivo. "**" como parametro: su valor es el path COMPLETO ya unido con "/".
    path: '**',
    renderMode: RenderMode.Prerender,
    async getPrerenderParams() {
      return allPaths().map((path) => ({ '**': path.join('/') }));
    },
  },
];
