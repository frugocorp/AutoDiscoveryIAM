# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/), y este proyecto respeta [Semantic Versioning](https://semver.org/lang/es/) (`MAJOR.MINOR.PATCH`; mientras la versión sea `0.y.z`, cualquier parte de la API pública puede cambiar sin previo aviso).

Este archivo es la fuente de verdad legible por humanos del historial de versiones. NuGet.org y GitHub Releases **no lo leen automáticamente**: al publicar cada versión, la sección correspondiente se resume a mano en `PackageReleaseNotes` (`src/AutoDiscoveryIAM/AutoDiscoveryIAM.csproj`) y en el cuerpo del GitHub Release.

## [Unreleased]

Sin cambios pendientes de publicar.

## [0.1.0] - 2026-11-19

Primera versión de vista previa (preview). Validada desde un proyecto externo limpio, tanto en instalación nueva como en actualización desde una versión anterior (`tools/consumer-e2e`). La superficie pública aún puede cambiar mientras la versión sea `0.y.z`. Licencia MIT.

### Added

- Autenticación JWT (access + refresh token) con rotación y detección de reutilización.
- RBAC dinámico: los permisos se descubren a partir de los endpoints reales vía `EndpointDataSource`, los de la librería y los de tu app, con cualquier namespace raíz que siga `<Raíz>.Features.<Feature>.<Acción>`.
- `POST /permissions/sync` reporta los endpoints que exigen un permiso pero no pueden nombrarse (`endpointsWithoutPermission`) y rechaza con un 400 dos slices distintos que producirían la misma clave.
- Todas las rutas de un mismo slice comparten su permiso.
- Rol `AdminAutoDiscoveryIAM` que administra usuarios, roles y permisos desde el primer día, sin flags especiales. `IsSuAdmin`/`IsDevelopment` solo se marcan a mano en la base de datos, y el rol por defecto de los auto-registrados nunca puede recibir permisos de administración.
- Actualizaciones sin perder datos: el esquema lo cubren las migraciones de tu app, y los cambios de datos de cada versión se aplican una sola vez al arrancar (tabla `AutoDiscoveryIAMUpgrades`).
- Autenticación en dos factores (TOTP/email) y login social con Google. El login con Google respeta el 2FA y el bloqueo por intentos fallidos, y no vincula por email las cuentas con `IsAdmin`/`IsDevelopment`/`IsSuAdmin`.
- Las cuentas de ejemplo con contraseña pública solo se siembran con opt-in explícito (`SeedAutoDiscoveryIAMDefaultDataAsync(includeDevelopmentUsers: true)`) y en Development/Testing; si aparecen activas en otro ambiente, se registra un log crítico al arrancar.
- Ciclo de vida completo de cuenta: registro, activación, aprobación, restablecimiento de contraseña, bloqueo por intentos fallidos.
- Rate limiting con el limitador nativo de ASP.NET Core (sección `RateLimiting`, sin dependencias externas), con límites por IP de fábrica en login, 2FA, refresh, restablecimiento de contraseña, activación, registro y eliminación de la propia cuenta, que tu app puede sobrescribir regla por regla. La IP del cliente sale de la conexión (nunca de `X-Real-IP`), y los proxies de confianza se configuran en `ForwardedHeaders`. Documentación OpenAPI nativa (`Microsoft.AspNetCore.OpenApi`), servida vía Swagger UI y Scalar.
- Secretos protegidos en la base de datos: los tokens de activación y de restablecimiento se guardan como hash, y el secreto de Authenticator, cifrado con Data Protection (su anillo de claves debe persistir entre reinicios y compartirse entre instancias: es un paso obligatorio de la instalación en producción). Un código de Authenticator ya aceptado no se acepta de nuevo.
- Sin enumeración de cuentas: el registro con un email existente responde igual que uno nuevo (y avisa al dueño), y el restablecimiento de contraseña y el reenvío de activación tardan lo mismo exista o no la cuenta.
- Las operaciones administrativas sobre una cuenta con `IsAdmin`/`IsDevelopment`/`IsSuAdmin` exigen un nivel igual o superior; `DELETE /users/me` pide la contraseña actual; al vincular Google a una cuenta con email sin verificar se invalida su contraseña.
- Regla de no escalada: nadie asigna un rol o un permiso que no tiene, ni cambia el email de un usuario con más permisos; aplica a los roles administrativos parciales (las cuentas con flags y el rol `AdminAutoDiscoveryIAM` completo quedan exentos).
- Endurecimiento del login y las sesiones: el aviso de cuenta bloqueada solo lo recibe quien da la contraseña correcta, los intentos fallidos simultáneos se cuentan todos, el token de reto del 2FA se guarda como hash, el logout solo revoca refresh tokens propios y su lista de tokens invalidados usa `IDistributedCache` (compartible entre instancias). En cuentas sin contraseña con Authenticator activo, cambiar el 2FA exige un código del Authenticator actual.
- Valores de configuración de seguridad validados al arranque (`JwtSettings`, `RefreshTokenSettings`, `Lockout`, `TwoFactor`), límites de peticiones de fábrica también en el login con Google y en las solicitudes de cambio de email, y paginación en `GET /roles/{publicId}/users` y `GET /permissions/{publicId}/users`.
- Auditoría y soft delete reutilizables en tus propias entidades (`AuditableEntity`).
- Arquitectura Vertical Slice sobre minimal APIs — sin MVC, sin MediatR.

[Unreleased]: https://github.com/frugocorp/AutoDiscoveryIAM/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/frugocorp/AutoDiscoveryIAM/releases/tag/v0.1.0
