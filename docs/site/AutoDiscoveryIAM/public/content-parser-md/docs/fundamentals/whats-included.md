---
[slug]: whats-included
[title]: Qué incluye hoy
[description]: Funcionalidades disponibles hoy en la librería: autenticación, RBAC dinámico, 2FA y más.
[keywords]: funcionalidades, autenticación JWT, RBAC, 2FA, AutoDiscoveryIAM
---

# Qué incluye hoy

- **Autenticación JWT** con access + refresh token, rotación y detección de reuso, blacklist de tokens por `jti`, e invalidación global inmediata al cambiar contraseña (`TokenValidFrom`).
- **Autenticación de dos factores (2FA)** por correo o app autenticadora (TOTP), y **login social con Google** (vinculación de cuenta externa).
- **RBAC dinámico**: los permisos (`Users`, `Roles`, `Permissions`) no se declaran a mano — se descubren automáticamente leyendo `EndpointDataSource` (la tabla de ruteo nativa de ASP.NET Core, no reflexión ni OpenAPI) y se sincronizan a base de datos. Autorización "secure by default": todo endpoint requiere un permiso salvo que se marque explícitamente lo contrario, con una regla de no escalada (nadie otorga ni administra permisos que no tiene).
- **Ciclo de vida de cuenta completo**: registro, activación por correo, aprobación/rechazo de cuentas, recuperación y cambio de contraseña.
- **Rate limiting** (limitador nativo de ASP.NET Core) y **logging de eventos de seguridad** centralizado.
- **Soft delete** consistente en todas las entidades principales, con endpoints de restauración.
- **Documentación OpenAPI nativa** (`Microsoft.AspNetCore.OpenApi`, no Swashbuckle) servida en `/openapi/{documentName}.json` — el renderizador (Swagger UI en la raíz, Scalar en `/scalar`) es intercambiable sin tocar el núcleo, probado en vivo con ambos a la vez.
- Suite de pruebas real: **xUnit + NSubstitute + AwesomeAssertions** (unitarias + integración).

Para el detalle de cómo funciona el RBAC dinámico y los niveles de acceso especiales, ver [Autorización con niveles de acceso](/docs/security-rbac/authorization). Para instalarla en tu propio proyecto, ve a [Instalación](/tutorials/getting-started/installation).
