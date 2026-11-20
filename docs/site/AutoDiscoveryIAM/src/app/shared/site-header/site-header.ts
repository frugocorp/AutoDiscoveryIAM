import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Lienzo vacio para el header de un equipo real -- alto minimo por default, sin contenido. Ver
 *  ScrollSpyDirective: su altura real (por si un equipo la cambia) forma parte de la linea de
 *  activacion del scrollspy, junto con la miga de pan. */
@Component({
  selector: 'app-site-header',
  templateUrl: './site-header.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteHeader {}
