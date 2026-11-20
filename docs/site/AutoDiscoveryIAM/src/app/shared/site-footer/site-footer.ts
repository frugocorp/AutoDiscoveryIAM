import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SITE_NAME } from '../../core/site.config';

/** Pie de pagina -- credito de FrugoCorp (herramienta con la que se genero el sitio, no del sitio
 *  en si) + licencia (hereda la de create-ngx-docs-site/NDMKit -- MIT, pagina real del sitio,
 *  "public/content-parser-md/license.md", generada una sola vez al crear el sitio, ver
 *  scaffold.ts -- mismo tratamiento que privacidad, no un archivo estatico crudo) + privacidad
 *  (paginas reales del sitio, `privacy-es.md`/`privacy-en.md`, ver esos archivos -- punto de
 *  partida generico, edita esas paginas antes de publicar). */
@Component({
  selector: 'app-site-footer',
  imports: [RouterLink],
  templateUrl: './site-footer.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SiteFooter {
  protected readonly siteName = SITE_NAME;
  protected readonly year = new Date().getFullYear();
  protected readonly frugocorpUrl = 'https://frugocorp.com';
}
