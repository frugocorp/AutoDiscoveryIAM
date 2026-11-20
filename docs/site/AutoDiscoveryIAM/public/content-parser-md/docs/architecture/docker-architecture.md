---
[slug]: docker-architecture
[title]: Arquitectura Docker
[description]: Cómo se organizan los contenedores del proyecto y qué papel cumple cada uno.
[keywords]: Docker, Docker Compose, arquitectura, contenedores
[readme_flavors]: github, npmjs
---

# Docker

## api/

El contenedor de la API (.NET, consume `FrugoCorp.AspNetCore.AutoDiscoveryIAM`) — desarrollo
(`dotnet watch`) y producción, configuración vía `.env`, y el detalle de cómo acceder por dominio
junto con tu propio reverse proxy. Ver [Docker: api/](/tutorials/docker/docker-api).

---

## app/web/

El contenedor del frontend Angular (SSR, `@angular/ssr`) — desarrollo (`ng serve`) y producción
(build real + `server.mjs`), configuración vía `.env`, genérico para reutilizarse con cualquier
proyecto Angular con SSR. Ver [Docker: app/web/](/tutorials/docker/docker-app-web).

---

## db/

Un contenedor de SQL Server desechable, pensado únicamente para desarrollo local — para quien no
tiene ya una instancia propia a la mano y no quiere instalar SQL Server directamente en su
máquina. Ver [Docker: db/](/tutorials/docker/docker-db).

---

## Proxy reverso (fuera de este repositorio)

Este repositorio no incluye ningún stack de nginx. `api/` y `app/web/` no publican puertos al host:
se conectan a la red externa `edge-net`, que crea el proxy reverso compartido del servidor,
[`nginx-server-docker`](https://github.com/frugocorp/nginx-server-docker) (repositorio aparte, con el TLS, los certificados y las plantillas
`.conf`). Ese stack tiene que estar levantado antes que ellos.

---
Cada carpeta es independiente: tiene su propio `docker-compose`, su propio `.env` (copiado de su
`.env.template`), y se levanta por separado — puedes usar `api/` con tu propia base de datos
remota sin tocar `db/`, o `db/` solo para tener algo con qué probar sin levantar la API en Docker.
`api/` y `app/web/` necesitan que `nginx-server-docker` ya esté levantado (crea la red `edge-net`).
