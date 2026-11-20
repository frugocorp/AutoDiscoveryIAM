import { InjectionToken, Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { type Observable, catchError, map, of, timeout } from 'rxjs';
import { cardsExtension, createParser, type DocPageContent } from '@ngx-docs-markdown-kit/parser-md';
import { seoExtension } from '@ngx-docs-markdown-kit/parser-md-seo';
import { codeBlockExtension } from '@ngx-docs-markdown-kit/parser-md-code-block';
import { imageExtension } from '@ngx-docs-markdown-kit/parser-md-image';
import { contentCardExtension } from '@ngx-docs-markdown-kit/parser-md-card';
import { fileEmbedExtension } from '@ngx-docs-markdown-kit/parser-md-file-embed';
import { SiteVariablesService } from '@frugocorp/create-ngx-docs-site/services/site-variables/site-variables.service';
import { applySiteVariables, groupPlaceholders, scanPlaceholders } from '@frugocorp/create-ngx-docs-site/services/site-variables/site-variables.js';
import { findPage } from '@site/core/nav';

// Un asset que NO EXISTE durante el prerenderizado de build no falla rapido con 404 -- se queda
// colgado hasta abortar el worker entero. catchError() solo entra en juego si el Observable
// subyacente llega a emitir un error, no si el fetch nunca resuelve -- por eso el timeout.
const CONTENT_FETCH_TIMEOUT_MS = 3000;

/** Implementacion real inyectada SOLO en app.config.server.ts (ver content-disk-reader.server.ts,
 *  que importa "node:fs"/"node:path" -- ese archivo nunca debe llegar al bundle de browser). Este
 *  archivo se compila para AMBAS plataformas (es un servicio "providedIn: root" referenciado desde
 *  el arbol de componentes), asi que no puede importar Node el mismo directamente. */
export const CONTENT_DISK_READER = new InjectionToken<(contentFile: string) => string>('CONTENT_DISK_READER');

/** Carga public/content-parser-md/<...>.md y lo parsea con las extensiones instaladas -- el UNICO
 *  responsable del HTML del cuerpo de una pagina. Nav/headings/meta de SEO NO pasan por aca (ver
 *  core/nav.ts, generado en build-time desde el mismo contenido). */
@Injectable({ providedIn: 'root' })
export class ContentService {
  private readonly http = inject(HttpClient);
  private readonly siteVariables = inject(SiteVariablesService);
  private readonly isServer = isPlatformServer(inject(PLATFORM_ID));
  private readonly diskReader = inject(CONTENT_DISK_READER, { optional: true });
  private readonly parser = createParser()
    .use(seoExtension())
    .use(codeBlockExtension())
    .use(cardsExtension())
    .use(imageExtension())
    .use(contentCardExtension())
    .use(fileEmbedExtension())
    ;

  /** "pathSegments" es el path COMPLETO de la URL (ej. ["docs","examples","sintaxis"], o
   *  ["docs","examples"] para la pagina propia de ese grupo) -- el manifiesto ya resolvio
   *  "contentFile" (el .md real a pedir, sea una pagina real o el "content_dir.md" autogenerado de
   *  un grupo), no hace falta reconstruir ninguna ruta aca. */
  getPage(pathSegments: readonly string[]): Observable<DocPageContent> {
    const contentFile = findPage(pathSegments)?.contentFile;
    const label = pathSegments.join('/') || 'index';
    const notFound = (): Observable<DocPageContent> =>
      of(this.parser.parse(`# Pagina no encontrada\n\nNo se encontro contenido para "${label}".`));
    if (!contentFile) {
      return notFound();
    }

    // "public/content-parser-md/" existe tal cual en "ng serve" y durante el prerenderizado de
    // build (ambos corren con cwd en la raiz del sitio, codigo fuente completo presente), que es
    // justo cuando el self-fetch HTTP de mas abajo no tiene ningun servidor real escuchando
    // todavia (se queda colgado hasta el timeout). En la imagen Docker final ese directorio ya no
    // existe (solo queda dist/), asi que ahi el intento de disco falla silencioso y cae al
    // self-fetch de siempre, que en ese punto SI tiene un servidor real escuchando.
    if (this.isServer && this.diskReader) {
      try {
        return of(this.parser.parse(this.applySiteVars(this.diskReader(contentFile))));
      } catch {
        // sigue abajo con el self-fetch.
      }
    }

    return this.http.get(`${this.baseUrl()}/content-parser-md/${contentFile}`, { responseType: 'text' }).pipe(
      timeout(CONTENT_FETCH_TIMEOUT_MS),
      map((markdown) => this.parser.parse(this.applySiteVars(markdown))),
      catchError(() => notFound()),
    );
  }

  /** En SSR, un fetch RELATIVO se resuelve contra el Host de la peticion entrante -- no siempre
   *  alcanzable desde DENTRO del propio contenedor (DNS/red externa). "http://127.0.0.1:<PORT>"
   *  (el mismo puerto que este proceso Node ya escucha) evita depender de esa red. */
  private baseUrl(): string {
    if (!this.isServer) {
      return '';
    }
    const port = process.env['PORT'] || 4000;
    return `http://127.0.0.1:${port}`;
  }

  /** Escanea `(--CLAVE--)`/`(--CLAVE:casing--)` en el markdown CRUDO de esta pagina (antes de
   *  parsear) y publica los grupos encontrados en SiteVariablesService -- la barra 3 los lee para
   *  saber que campos mostrar (vacio = no mostrar el panel, esta pagina no tiene nada que
   *  personalizar). Recien despues sustituye con los valores ya guardados (si los hay). */
  private applySiteVars(markdown: string): string {
    const occurrences = scanPlaceholders(markdown);
    this.siteVariables.discoveredOnCurrentPage.set(groupPlaceholders(occurrences));
    return applySiteVariables(markdown, this.siteVariables.effectiveValuesMap());
  }
}
