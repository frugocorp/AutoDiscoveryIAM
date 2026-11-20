<on_site>
# Docker

## Índice

- [Página de inicio (README principal)](../README.md)
- [api/](#api)
- [app/web/](#appweb)
- [db/](#db)
- [Proxy reverso (fuera de este repositorio)](#proxy-reverso-fuera-de-este-repositorio)

---

## api/

[REGRESAR AL ÍNDICE](#índice)

---

El contenedor de la API (.NET, consume `FrugoCorp.AspNetCore.AutoDiscoveryIAM`) — desarrollo
(`dotnet watch`) y producción, configuración vía `.env`, y el detalle de cómo acceder por dominio
junto con tu propio reverse proxy. Ver [api/README.md](api/README.md).

---

## app/web/

[REGRESAR AL ÍNDICE](#índice)

---

El contenedor del frontend Angular (SSR, `@angular/ssr`) — desarrollo (`ng serve`) y producción
(build real + `server.mjs`), configuración vía `.env`, genérico para reutilizarse con cualquier
proyecto Angular con SSR. Ver [app/web/README.md](app/web/README.md).

---

## db/

[REGRESAR AL ÍNDICE](#índice)

---

Un contenedor de SQL Server desechable, pensado únicamente para desarrollo local — para quien no
tiene ya una instancia propia a la mano y no quiere instalar SQL Server directamente en su
máquina. Ver [db/README.md](db/README.md).

---

## Proxy reverso (fuera de este repositorio)

[REGRESAR AL ÍNDICE](#índice)

---

Este repositorio no incluye ningún stack de nginx. `api/` y `app/web/` no publican puertos al host:
se conectan a la red externa `edge-net`, que crea el proxy reverso compartido del servidor,
[`nginx-server-docker`](https://github.com/frugocorp/nginx-server-docker) (repositorio aparte, con el TLS, los certificados y las plantillas
`.conf`). Ese stack tiene que estar levantado antes que ellos.

---
Cada carpeta es independiente: tiene su propio `docker-compose`, su propio `.env` (copiado de su
`.env.template`), y se levanta por separado — puedes usar `api/` con tu propia base de datos
remota sin tocar `db/`, o `db/` solo para tener algo con qué probar sin levantar la API en Docker.
`api/` y `app/web/` necesitan que `nginx-server-docker` ya esté levantado (crea la red `edge-net`).
