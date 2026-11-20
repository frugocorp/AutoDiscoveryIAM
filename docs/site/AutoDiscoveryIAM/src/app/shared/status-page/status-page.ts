import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Ejemplo de ruta 100% del equipo dev, fuera del sistema de contenido -- demuestra que
 * "app.routes.ts" no es 100% generado desde el manifiesto: agregar una ruta como esta (o cualquier
 * feature/bounded-context propio) convive sin tocar nada de "content-parser-md/" ni de las 3
 * barras, que solo conocen secciones REALES del manifiesto (ver nav-bar-primary.ts). Vive en
 * "src/app/shared/" (lienzo limpio), nunca dentro de "content-parser-md/", que es zona de la
 * libreria/CLI.
 */
@Component({
  selector: 'app-status-page',
  templateUrl: './status-page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusPage {}
