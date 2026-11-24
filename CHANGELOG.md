# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/), y este proyecto respeta [Semantic Versioning](https://semver.org/lang/es/) (`MAJOR.MINOR.PATCH`; mientras la versión sea `0.y.z`, cualquier parte de la API pública puede cambiar sin previo aviso).

Este archivo es la fuente de verdad legible por humanos del historial de versiones. NuGet.org y GitHub Releases **no lo leen automáticamente**: al publicar cada versión, la sección correspondiente se resume a mano en `PackageReleaseNotes` (`src/AutoDiscoveryIAM/AutoDiscoveryIAM.csproj`) y en el cuerpo del GitHub Release.

Cada versión abre con `### Breaking` cuando algo deja de funcionar igual que en la versión anterior. Cada entrada dice el tipo de cambio, a quién afecta y qué hacer:

- **Código fuente**: tu proyecto deja de compilar (o compila con advertencias nuevas) hasta que ajustes tu código.
- **Binario**: compila igual, pero hay que recompilar; una librería tuya ya compilada contra la versión anterior puede fallar en tiempo de ejecución.
- **Comportamiento**: compila igual, pero un endpoint o un flujo responde distinto.
- **Esquema**: la actualización requiere una migración en tu proyecto.

Las rupturas de API pública no dependen de que alguien las recuerde: la validación de paquete del SDK compara cada `dotnet pack` contra la última versión publicada y falla si una no está declarada en `src/AutoDiscoveryIAM/CompatibilitySuppressions.xml`. Toda supresión nueva en ese archivo tiene su entrada en `### Breaking`.

## [Unreleased]

## [0.2.0] - 2026-11-24

### Breaking

- **Comportamiento.** Todo usuario nuevo (registro por correo, alta con Google y alta por un administrador) recibe el rol marcado con `IsDefaultForNewUser` (por defecto `UserDefault`, sin permisos). Antes nacía sin roles. Si le diste permisos a ese rol, los usuarios nuevos los tienen desde que se crean: revísalos antes de actualizar.
- **Código fuente.** `RefreshTokenResponseDto.RefreshToken` y `RefreshTokenRequestDto.RefreshToken` ahora son `string?` (`null` cuando el token viaja solo en cookie). Con `Nullable` y `TreatWarningsAsErrors` activos, el código que los usa como `string` deja de compilar: maneja el `null` o, si no usas la cookie, aplica `!` (con `Delivery: Body`, el valor por defecto, nunca es `null`).
- **Binario.** `LoginResponseDto` tiene un parámetro posicional nuevo (`PasswordChangeRequired`) y `RefreshTokenRequestDtoExtensions.ToCommand` uno opcional (`cookieToken`). Recompila; una deconstrucción posicional de `LoginResponseDto` necesita el sexto elemento.
- **Código fuente.** Los constructores de `CreateUserHandler`, `RefreshTokenHandler` y `ExternalLoginOrchestrator` reciben dependencias nuevas. Solo te afecta si los construyes a mano (por ejemplo, en tus pruebas); resueltos desde DI no cambia nada.
- **Código fuente.** `IRefreshTokenRepository` agrega `GetByHashAsync` y `ReloadAsync`, e `IPasswordResetEmailService` agrega `SendPasswordSetupEmailAsync`. Solo te afecta si registraste tu propia implementación de alguna; los mocks no cambian.
- **Comportamiento.** `POST /users/{publicId}/approve` ya no marca el correo como verificado (`IsEmailVerified` queda como estaba). Ver `### Security`.
- **Esquema.** Columna nueva `Users.MustChangePassword` (`bit`, no nula, default `false`). Genera la migración de la actualización como siempre; no debe traer nada más.

### Added

- `app.SyncAutoDiscoveryIAMPermissionsAsync()`: sincroniza los permisos desde código, sin login HTTP ni servidor levantado, para un pipeline de despliegue o una bandera de arranque (el SampleHost trae `--sync-permissions` de ejemplo). Va después de mapear los endpoints, acepta las mismas opciones que `POST /permissions/sync` y lanza `InvalidOperationException` si el sync falla.
- `ITwoFactorStepUpService`: verifica un código del Authenticator de un usuario ya autenticado antes de una acción sensible de tu app, con protección contra reuso (el paso usado queda guardado) y el mismo bloqueo por intentos fallidos del login. Sin endpoint propio: se inyecta en el endpoint del consumidor.
- `RefreshTokenSettings:Delivery` (`Body` por defecto, `Cookie` o `Both`): entrega el refresh token en una cookie `HttpOnly`, `Secure`, `SameSite=Strict` en vez de (o además de) el cuerpo JSON, para las SPA que no quieren guardarlo en storage. Con la cookie en uso, `POST /auth/refresh` y `POST /auth/logout` la leen si el cuerpo no trae el token, y el logout la borra. El refresh que toma el token de la cookie exige el encabezado `X-Requested-With` (cualquier valor): obliga al preflight de CORS, y otro sitio no puede forzar la rotación. Ajustes en `RefreshTokenSettings:Cookie` (`Name`, `Path`, `Domain`, `SameSite`).
- `RefreshTokenSettings:ReuseGracePeriodSeconds` (0 por defecto, apagado; máximo 30): durante esos segundos después de rotar un refresh token, reusarlo desde el mismo cliente (misma IP y User-Agent que la rotación) devuelve el mismo par nuevo en vez de revocar toda la sesión; desde otro cliente se trata como reuso. Cubre el reintento por una respuesta perdida y dos refresh simultáneos. Con varias instancias requiere `IDistributedCache` y Data Protection compartidos.
- `POST /users/by-admin` (permiso `users.create_user_by_admin`, incluido en el rol `AdminAutoDiscoveryIAM`): un administrador da de alta usuarios con el rol por defecto, ya activos (`activate: true`, sin correo) o pendientes de activación por correo. Funciona aunque el registro público esté apagado, y un correo o usuario repetido responde 409.
- `IUserProvisioningService`: alta de usuarios desde el código de tu app, con las mismas reglas que `POST /users/by-admin` (validación de formato incluida), para un endpoint propio que crea el usuario junto con su entidad ligada. La sobrecarga con `beforeSave` recibe el usuario nuevo antes del único guardado: tu entidad y el usuario se guardan juntos, y si tu código falla no queda el usuario ni sale ningún correo. Sin permiso propio: lo decide el endpoint que lo llama.
- En las altas de un administrador con `password`, el usuario debe cambiar la contraseña inicial al entrar (`mustChangePassword`, `true` por defecto; `false` la deja vigente): el login devuelve `passwordChangeRequired: true` y todo endpoint con permisos responde 403 `AUTH_PASSWORD_CHANGE_REQUIRED` hasta `POST /users/me/password`. Sin `password` el usuario recibe un solo correo para definir la suya (`Registration:PasswordSetupLinkExpiryHours`, 72 h por defecto). Definir la contraseña con un enlace del correo también confirma el correo de la cuenta.
- `Registration:RequireAdminApproval`: con `true`, quien se registra solo (por correo o con Google) queda en el nuevo estado `pending_approval` aunque confirme su correo, y solo entra después de `POST /users/{publicId}/approve`.
- Sección `Registration` para prender o apagar cada forma de registro por separado: `EmailPasswordEnabled` (registro público en `POST /users`) y `ExternalProviders` (alta automática con cada proveedor externo, por ejemplo `"Google": false`). Con una forma apagada, su endpoint responde 403 `REGISTRATION_DISABLED`; quien ya tiene cuenta sigue entrando.

### Changed

- El cuerpo de `POST /auth/refresh` es opcional cuando el refresh token viaja en cookie (`RefreshTokenSettings:Delivery`).

### Fixed

- `POST /permissions/sync` ya no falla con un 500 cuando la descripción de un endpoint (`.WithDescription()`, de la librería o de tu app) supera los 500 caracteres de la columna: se guarda recortada.
- Una cuenta rechazada (`POST /users/{publicId}/reject`) ya no puede reactivarse sola: el reenvío público de activación no le envía un enlace nuevo, y un enlace de activación solo activa cuentas que esperan activación. Reabrirla queda en manos de un administrador (`POST /users/{publicId}/activation/resend` o `approve`).
- El rol marcado con `IsDefaultForNewUser` (por defecto `UserDefault`) ahora sí se asigna a todo usuario nuevo, tanto en el registro por correo como en el alta con Google. Antes solo existía la marca y el usuario nacía sin roles.

### Security

- Una cuenta creada por un administrador con contraseña nace con el correo sin verificar, y el administrador ya no conoce indefinidamente la contraseña vigente salvo que lo pida (`mustChangePassword: false`): el primer login del dueño con Google o un restablecimiento invalidan la contraseña que eligió el administrador.
- Aprobar una cuenta ya no la marca con el correo verificado. Quien registraba el correo de otra persona con una contraseña propia y conseguía la aprobación conservaba el acceso aunque el dueño real entrara después con Google; ahora ese primer login con Google invalida la contraseña, igual que en una cuenta sin aprobar.

## [0.1.1] - 2026-11-19

### Changed

- El enlace "Project website" del paquete en NuGet.org apunta al sitio de documentación (https://autodiscoveryiam.frugocorp.com) en vez del repositorio; el repositorio sigue enlazado como "Source repository".

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

[Unreleased]: https://github.com/frugocorp/AutoDiscoveryIAM/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/frugocorp/AutoDiscoveryIAM/compare/v0.1.1...v0.2.0
[0.1.1]: https://github.com/frugocorp/AutoDiscoveryIAM/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/frugocorp/AutoDiscoveryIAM/releases/tag/v0.1.0
