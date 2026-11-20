---
[slug]: tech-stack
[title]: Stack técnico
[description]: Tecnologías sobre las que está construido el proyecto.
[keywords]: stack técnico, .NET 10, Entity Framework Core, OpenAPI
[readme_flavors]: github, npmjs
---

# Stack técnico

- **.NET 10 (LTS)** minimal APIs, **Entity Framework Core** (SQL Server — la base de datos vive en su propia infraestructura, fuera del `docker-compose` de este repo, a propósito).
- **`Microsoft.AspNetCore.OpenApi`** (nativo) para la generación del documento OpenAPI; **Swashbuckle.AspNetCore.SwaggerUI** y **Scalar.AspNetCore** como renderizadores intercambiables, sin dependencias propias entre sí.
- **xUnit / NSubstitute / AwesomeAssertions** para pruebas; EF Core InMemory para tests de integración por defecto (no requiere infraestructura real para correr la suite completa).
- **Docker**, reverse proxy/TLS resuelto por un stack Nginx compartido externo ([`nginx-server-docker`](https://github.com/frugocorp/nginx-server-docker), ver [Docker](/docs/architecture/docker-architecture)), no incluido en este repo.
