---
[slug]: docker-app-web
[title]: Docker: app/web/
[description]: Cómo levantar en un contenedor este sitio de documentación.
[keywords]: Docker, sitio web, Angular, Docker Compose
[readme_flavors]: github, npmjs
---

# Docker — app/web/

## Desarrollo (por defecto)

Entra a la carpeta:

````code-block
```bash
cd docker/app/web
```
````

Construye y levanta el contenedor:

````code-block
```bash
docker-compose up --build
```
````

Docker Compose fusiona automáticamente `docker-compose.yml` (base) con
`docker-compose.override.yml` (que sí existe en este directorio) cuando no se especifica `-f`
explícitamente. `docker-compose.override.yml` define `target: dev` para el servicio, así que este
comando **siempre levanta la imagen de desarrollo** (`ng serve --host 0.0.0.0`, con el código
fuente montado como volumen, equivalente al `dotnet watch` de `docker/api/`).

---

## Producción

Para levantar solo la imagen de producción (`target: final`, build real con SSR — sin `ng serve`
ni montaje de código fuente), hay que excluir explícitamente el override:

Entra a la carpeta:

````code-block
```bash
cd docker/app/web
```
````

Construye y levanta solo la imagen de producción, excluyendo el override de desarrollo:

````code-block
```bash
docker-compose -f docker-compose.yml up --build -d
```
````

La etapa `final` corre `node dist/<APP_PROJECT_NAME>/server/server.mjs` — el servidor Express que
genera `@angular/ssr`, no un servidor de archivos estáticos. Igual que en `docker/api/`,
`docker-compose.yml` fija `target: final` de forma explícita porque el `Dockerfile` tiene la
etapa `dev` como última etapa textual (Docker usaría `dev` como destino implícito si no se
especificara).

---

## Configuración (`.env`)

Copiar `.env.template` a `.env` y completar los valores. Para qué sirve cada variable, ver los
comentarios en `docker-compose.yml`/`.env.template` — no se repiten aquí para no mantener la misma
explicación en dos lugares. Dos cosas puntuales que vale la pena resaltar:

- **`APP_ALLOWED_HOSTS`** — Angular 22+ valida el header `Host` como protección nativa contra
  SSRF (ver [angular.dev/best-practices/security](https://angular.dev/best-practices/security#preventing-server-side-request-forgery-ssrf))
  y responde `400 Bad Request` para cualquier host no listado aquí, **incluido** el que reenvía
  nginx como proxy reverso. Debe incluir tu dominio real y, si quieres poder probar este
  contenedor directo sin pasar por nginx, `localhost` también — **nunca con el puerto** (Angular
  valida contra el hostname ya separado del puerto, `localhost:8010` nunca hace match, solo
  `localhost`). Nunca uses `*`, desactiva la protección por completo.
- Este valor se duplica a mano respecto al dominio que declara la plantilla `.conf` de este sitio
  en `nginx-server-docker` — son repositorios independientes, sin variables compartidas entre sí
  a propósito.

---

## Reutilizar esta carpeta en otro proyecto

Pensada para copiarse tal cual (`docker/app/web/` completa: `Dockerfile`, `docker-compose.yml`,
`.override.yml`, `.env.template`) a cualquier otro proyecto Angular con SSR (`@angular/ssr`):

- **Solo con ajustar `.env`** (`APP_BUILD_CONTEXT`, `APP_DOCKERFILE_PATH`, `APP_PROJECT_PATH`,
  `APP_PROJECT_NAME`, `APP_ALLOWED_HOSTS`, puertos) ya apunta al proyecto nuevo — el `Dockerfile`
  es genérico: no tiene el nombre del proyecto hardcodeado en ningún lado, solo asume la
  convención estándar del CLI (`dist/<PROJECT_NAME>/browser` + `dist/<PROJECT_NAME>/server/server.mjs`).
- **Lo único que sí hay que buscar y reemplazar a mano** es el nombre del servicio en los 3
  archivos de Compose — Docker Compose no permite templatizar nombres de servicio con variables
  (mismo motivo documentado en [Docker: api/](/tutorials/docker/docker-api)).
- Si el proyecto Angular **no** usa SSR (solo SPA estático), este `Dockerfile` no aplica tal
  cual — la etapa `final` necesita ajustarse para servir `dist/<PROJECT_NAME>/browser` con un
  servidor de archivos estáticos (o con el propio nginx directamente) en vez de correr
  `server.mjs`.

---

## Proxy reverso compartido

Igual que `docker/api/`: este `docker-compose.yml` no publica ningún puerto al host, a propósito, y
el contenedor solo es alcanzable por la red externa `edge-net`, que crea
[`nginx-server-docker`](https://github.com/frugocorp/nginx-server-docker) (repositorio aparte). Ese proxy hace `proxy_pass` hacia
`APP_CONTAINER_NAME:APP_PORT_CONTAINER` con la plantilla `.conf` de este sitio, y tiene que estar
levantado antes que este stack.
