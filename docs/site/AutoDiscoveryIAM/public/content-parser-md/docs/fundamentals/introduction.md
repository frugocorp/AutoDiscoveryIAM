---
[slug]: introduction
[title]: Introducción
[description]: Qué es AutoDiscoveryIAM, qué problema resuelve y cómo está organizado su repositorio.
[keywords]: AutoDiscoveryIAM, RBAC dinámico, JWT, ASP.NET Core, minimal APIs
---

# AutoDiscoveryIAM

`FrugoCorp.AspNetCore.AutoDiscoveryIAM` es una librería para APIs de ASP.NET Core construidas con **minimal APIs** que agrega **autenticación JWT completa + RBAC dinámico**: los roles y permisos se descubren solos, en tiempo de ejecución, leyendo los endpoints reales de tu app vía `EndpointDataSource` — nada que declarar a mano ni mantener sincronizado cuando agregas un endpoint nuevo. La propia librería está construida con **Vertical Slice Architecture**, sin capa MVC/Controllers y **sin MediatR**.

> El descubrimiento automático de permisos depende de metadatos que ASP.NET Core solo expone para minimal APIs (el método mapeado, vía `EndpointDataSource`). Si tu app usa Controllers/MVC para sus propios endpoints, la librería y su login/JWT/RBAC funcionan igual, pero el auto-descubrimiento de permisos no cubre esos endpoints — necesitarías crear sus `Permission` manualmente.

## Este repositorio

Este repositorio tiene 5 partes con propósitos distintos:

- **`src/AutoDiscoveryIAM/`** — la librería en sí: el motor RBAC + JWT que se empaqueta y publica en NuGet como `FrugoCorp.AspNetCore.AutoDiscoveryIAM`. Es lo único que tu proyecto necesita instalar, y el resto de la documentación trata casi exclusivamente sobre ella.
- **`samples/AutoDiscoveryIAM.SampleHost/`** — una app ASP.NET Core real y ejecutable que consume la librería, a modo de host de referencia: sirve para correr, explorar y probar la API completa (Swagger UI, Scalar) sin tener que integrarla primero en tu propio proyecto.
- **`tests/`** — las suites de pruebas (`AutoDiscoveryIAM.UnitTests`, `AutoDiscoveryIAM.IntegrationTests`) que validan la librería y el `SampleHost`.
- **`runsettings/`** — perfiles de ejecución de pruebas (`normal`/`coverage`/`performance`/`security`/`ci-full`). Son varios y no uno solo porque cada combinación de filtro/cobertura/paralelismo tiene un costo de tiempo muy distinto.
- **`docker/`** — Docker Compose para levantar el contenedor del `SampleHost` sin instalar el SDK de .NET en tu máquina — el mismo trato que ya le das a cualquier stack en Docker: un solo comando y la API está corriendo. La base de datos **no** viene incluida a propósito: necesitas tu propio SQL Server alcanzable, apuntado vía cadena de conexión.

Para empezar a usar la librería en tu propio proyecto, ve directo a [Instalación](/tutorials/getting-started/installation). Si quieres ver primero qué trae hoy la librería en detalle (autenticación, RBAC dinámico, 2FA, etc.), revisa [¿Qué incluye?](/docs/fundamentals/whats-included).
