import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import type { NavItem, NavTreeNode } from '@site/core/nav';

/**
 * Un nivel del arbol de la barra 2 -- se importa a si mismo para dibujar grupos/subgrupos de
 * profundidad ilimitada (ver buildNavTree() en nav-tree.util.ts). "group" ausente es el nivel RAIZ
 * de la seccion (items sueltos, sin encabezado de grupo); con "group" es un grupo/subgrupo normal
 * -- y ahora TAMBIEN es un link real (su propio "content_dir.md"), no solo una etiqueta.
 */
@Component({
  selector: 'app-nav-tree',
  imports: [RouterLink, RouterLinkActive, NavTreeView],
  templateUrl: './nav-tree.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NavTreeView {
  readonly group = input<NavTreeNode>();
  readonly items = input.required<readonly NavItem[]>();
  readonly groups = input<readonly NavTreeNode[]>([]);
  readonly navigate = output<void>();
}
