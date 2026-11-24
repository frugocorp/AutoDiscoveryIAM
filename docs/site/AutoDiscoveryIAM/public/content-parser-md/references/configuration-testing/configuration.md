---
[slug]: configuration
[title]: Configuración
[description]: Todas las claves de configuración de la librería: obligatorias, recomendadas y opcionales.
[keywords]: configuración, appsettings.json, JwtSettings, variables de entorno
---

# Configuración

Todas las claves viven bajo `appsettings.json` (o variables de entorno equivalentes). Se dividen en tres grupos — validado en vivo instalando el paquete en un proyecto externo limpio, agregando configuración una sección a la vez:

## Configuración obligatoria

Sin esto la app no arranca (falla al primer intento de conexión o por validación explícita al inicio):

| Sección             | Claves                      | Por qué es obligatoria                                                                                                                                                                                                                                                                  |
| ------------------- | --------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ConnectionStrings` | `DefaultConnection`         | Sin ella no hay forma de migrar/sembrar/consultar la base de datos — falla en cuanto tu `Program.cs` intenta usar el `DbContext` (ej. al migrar al arrancar).                                                                                                                           |
| `JwtSettings`       | `Key`, `Issuer`, `Audience` | Único par de secciones con validación explícita al arranque (`JwtSettingsValidator`, `ValidateOnStart()`): si falta alguna clave, o si `Key` tiene menos de **32 caracteres**, la app no llega a levantar, en vez de dejar el login silenciosamente roto o firmado con una clave débil. |

> ⚠️ **Además, en producción:** el anillo de claves de Data Protection no es una clave de `appsettings.json`, pero es igual de obligatorio. Ver [Instalación → Claves de Data Protection](/tutorials/getting-started/installation).

**De dónde salen estos valores**, en general:

- `ConnectionStrings:DefaultConnection` depende de la base de datos que tú mismo levantes o ya tengas (host, usuario, contraseña que tú definiste al crearla) — ver [Necesitas una base de datos](/tutorials/getting-started/installation#necesitas-una-base-de-datos).
- Los 3 valores de `JwtSettings` **no vienen de ningún proveedor externo** — a diferencia de `Mail`/`Authentication:Google` (tabla de abajo), que sí dependen de credenciales de un tercero, estos los inventas tú mismo:
  - `Key`: un valor aleatorio de al menos 32 caracteres, no una frase memorable. Elige **uno solo** de estos dos comandos según tu terminal (son alternativas equivalentes, no dos pasos) y córrelo ahí (no lo pegues dentro del JSON):

    ````card-code-block
    @description
    PowerShell (Windows 5.1, la que abre `powershell.exe` por default) corre sobre .NET Framework y no tiene el método estático moderno `RandomNumberGenerator.GetBytes(int)` (ese es de .NET 6+) -- de ahí la forma clásica con `Create()` + `GetBytes(byte[])`, compatible con ambas versiones.

    @command powershell
    $bytes = [byte[]]::new(32)
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    [Convert]::ToBase64String($bytes)

    @result<Resultado esperado> text
    QzyFnaNEDf61rMsKVJpgBKcFEdW6uKwZBexonNCxwT0=

    @note<Nota>
    Ese texto impreso (no el comando) es el que va en `"Key"` dentro de tu `appsettings.json` -- tu resultado real va a ser distinto a este ejemplo, cada corrida genera un valor nuevo.
    ````

    ````card-code-block
    @description
    Bash / Git Bash / Mac / Linux -- requiere tener `openssl` instalado en esa terminal.

    @command bash
    openssl rand -base64 32

    @result<Resultado esperado> text
    xK3mP9vQ2wZ8Rj4tL7nH1sA6dF0cB5eY==

    @note<Nota>
    Igual que con PowerShell, el texto impreso (no el comando) es el que va en `"Key"` -- tu valor real va a ser distinto.
    ````

    Con ese resultado, tu `appsettings.json` queda así (con el valor real que a ti te imprimió, no este ejemplo):

    ````code-block
    ```json
    "JwtSettings": { "Key": "xK3mP9vQ2wZ8Rj4tL7nH1sA6dF0cB5eY==", "Issuer": "...", "Audience": "..." }
    ```
    ````

  - `Issuer`/`Audience`: no son secretos — son solo texto identificador que tú eliges libremente para representar tu propia app (ej. el nombre de tu proyecto). Solo necesitan mantenerse consistentes; no se "obtienen" de ningún lado. El `SampleHost` de este repo usa `"autodiscoveryiam-api"`/`"autodiscoveryiam-clients"` como ejemplo real.

- `BaseUrl`: la URL pública de **tu propia API** (no la del frontend) — se usa para armar los links absolutos de los correos que manda la librería (activación de cuenta, recuperación de contraseña), ej. `{BaseUrl}/users/activation/confirm?token=...`. En desarrollo local, la URL donde corre tu API (ej. `"https://localhost:7001"`); en producción, tu dominio real (ej. `"https://miproyecto-api.frugocorp.com"` o `"https://miproyecto-api.com"`). Vacío cae a un link relativo, poco confiable en un correo real.
- `Cors:AllowedOrigins`: la lista de orígenes de **tu frontend** a los que le permites llamar a esta API desde el navegador — ej. `["http://localhost:4200"]` si tu Angular corre con `ng serve` en desarrollo o ej. `["https://miproyecto.frugocorp.com"]` o `["https://miproyecto.com"]`si tu app ya corre en tu servidor. Un origen no listado aquí queda bloqueado por CORS del lado del navegador, aunque la API responda bien.

## Configuración requerida solo si usas esa función

No tienen default posible (son credenciales propias de cada instalación), pero **no bloquean el arranque** — confirmado corriendo la app sin declarar ninguna de estas: solo fallan en el momento en que se intenta usar esa función específica (enviar un correo real, o completar un login de Google):

| Sección                 | Claves                                    | Cuándo falla si falta                                                                                               |
| ----------------------- | ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| `Mail:Smtp`             | `Host`, `User`, `Password`                | Al primer intento real de enviar un correo (activación de cuenta, recuperación de contraseña).                      |
| `Mail:From`             | `Email`                                   | Igual que arriba.                                                                                                   |
| `Authentication:Google` | `ClientId`, `ClientSecret`, `RedirectUri` | Solo si `Enabled: true` (default `false`, ver tabla de abajo) — al intentar completar el flujo de login con Google. |

## Configuración recomendada

Ya trae un default seguro embebido en código — solo la tocas si quieres cambiar el valor:

| Sección                           | Default                                                                                                                                               | Se puede sobreescribir parcialmente (ej. solo un campo) |
| --------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------- |
| `JwtSettings.ExpiryMinutes`       | `15`                                                                                                                                                  | ✅                                                      |
| `RefreshTokenSettings`            | `ExpiryDays: 7`, `TokenLength: 32`, `ReuseGracePeriodSeconds: 0` (ventana de gracia apagada), `Delivery: Body` (refresh token en el cuerpo JSON; `Cookie` o `Both` para la cookie HttpOnly). Ver las notas debajo de la tabla | ✅                                                      |
| `Activation`                      | `EmailVerificationTokenLength: 64`, `EmailVerificationTokenExpiryMinutes: 30`, `ActivationResendCooldownSeconds: 60`, `ActivationResendMaxPerHour: 5`, `MinimumResponseMilliseconds: 1000` (duración mínima del reenvío público, exista o no la cuenta) | ✅                                                      |
| `Registration`                    | `EmailPasswordEnabled: true` (registro público en `POST /users`), `ExternalProviders: {}` (cada proveedor externo crea cuentas nuevas salvo que lo apagues, por ejemplo `"Google": false`), `RequireAdminApproval: false` (con `true`, quien se registra solo queda en `pending_approval` aunque confirme su correo, hasta `POST /users/{publicId}/approve`; no aplica a las altas de un administrador), `PasswordSetupLinkExpiryHours: 72` (vigencia del enlace para definir la contraseña cuando un administrador crea la cuenta sin contraseña). Pueden quedar todas las formas apagadas: entonces solo un administrador da de alta usuarios. Apagar una forma nunca impide iniciar sesión a quien ya tiene cuenta; el endpoint apagado responde 403 `REGISTRATION_DISABLED` | ✅                                                      |
| `PasswordReset`                   | `TokenLength: 64`, `TokenExpiryMinutes: 30`, `MinimumResponseMilliseconds: 1000` (duración mínima de la solicitud, exista o no la cuenta)               | ✅                                                      |
| `Lockout`                         | `MaxFailedAttempts: 5`, `LockoutMinutes: 60`                                                                                                          | ✅                                                      |
| `TwoFactor`                       | `CodeLength: 6`, `CodeExpiryMinutes: 5`, `ChallengeTokenExpiryMinutes: 10`, `Issuer: "AutoDiscoveryIAM"`                                              | ✅                                                      |
| `ForwardedHeaders`                | `KnownNetworks: ["172.16.0.0/12"]` (red bridge de Docker), `KnownProxies: []`. Los proxies de confianza de los que se aceptan `X-Forwarded-For`/`X-Forwarded-Proto`; si declaras cualquiera de las dos listas, reemplaza a la red por defecto. Loopback siempre es de confianza. Un valor inválido detiene el arranque | ✅                                                      |
| `PasswordStruct`                  | Sin regla de complejidad adicional (`Regex` vacío) — solo longitud mínima                                                                             | ✅                                                      |
| `Authentication:Google.Enabled`   | `false`                                                                                                                                               | —                                                       |
| `Authentication.ExternalLoginRedirectUri` | Vacío/`null` — el callback de login social responde JSON directo en vez de redirigir (ver nota debajo de la tabla)                            | ✅                                                      |
| `Authentication.ExchangeCodeExpirySeconds` | `60` — solo aplica si `ExternalLoginRedirectUri` está configurado (ver nota debajo de la tabla)                            | ✅                                                      |
| `Cors.AllowedOrigins`             | `[]` (vacío) — CORS queda cerrado por completo (nunca `AllowAnyOrigin`); hay que declarar explícitamente qué front-ends pueden llamar a la API        | ✅                                                      |
| `RateLimiting`                    | Límites por IP en las rutas sensibles de la librería (login, 2FA, refresh, restablecimiento de contraseña, activación, registro, eliminación de la propia cuenta, login con Google y solicitudes de cambio de email; ver nota debajo de la tabla). Ningún límite general: declara `Rules` para tus propios endpoints | ✅                                                      |

No hace falta declarar ninguna sección de esta tabla en tu propio `appsettings.json` a menos que quieras cambiar alguno de sus valores — y si la declaras, solo necesitas incluir el campo que te importa, el resto conserva su default.

> **Límites por defecto de `RateLimiting`.** Usa el limitador nativo de ASP.NET Core (`Microsoft.AspNetCore.RateLimiting`), sin dependencias externas. Por IP: login y verificación 2FA, 10 por minuto y 60 por hora; refresh, 30 por minuto; canje del código de login externo, 10 por minuto; solicitud de restablecimiento de contraseña y reenvío de activación, 5 por minuto y 20 por hora; confirmación de restablecimiento y de activación, 10 por minuto y 60 por hora; cambio de contraseña y operaciones de 2FA del propio usuario, 10 por minuto; eliminación de la propia cuenta (`DELETE /users/me`, que pide la contraseña actual), 10 por minuto; registro (`POST /users`), 5 por minuto y 30 por hora; inicio y callback del login con Google (`GET /auth/google*`), 20 por minuto; vinculación de la cuenta de Google, 10 por minuto; solicitud de cambio de email, 3 por minuto y 10 por hora. Cada regla tiene `Endpoint` (`*` para todas las rutas, o `metodo:/ruta` con `*` como comodín, ej. `post:/auth/login*`), `Period` (`30s`, `1m`, `1h`, `1d`) y `Limit`. Para cambiar un límite de la librería, declara en `RateLimiting:Rules` una regla con el mismo `Endpoint` y `Period` (por ejemplo `{ "Endpoint": "post:/auth/login*", "Period": "1m", "Limit": 30 }`): la tuya reemplaza a la de la librería. Si varias reglas aplican a una petición, se exigen todas; una regla `*` cuenta en un solo contador todas las peticiones de la IP, sin importar la ruta. `RateLimiting:EndpointWhitelist` exime rutas de todas las reglas (ej. `"get:/openapi/*"`). Al pasar el límite, la API responde 429 con el header `Retry-After` y el mismo formato de error que el resto de los endpoints. Una regla mal escrita detiene el arranque. La IP del cliente es siempre la de la conexión (`RemoteIpAddress`), nunca un header que mande el propio cliente (como `X-Real-IP`); detrás de un proxy inverso, la IP real llega con `app.UseForwardedHeaders()`, que solo acepta `X-Forwarded-For` de los proxies de confianza de la sección `ForwardedHeaders` (ver la fila de la tabla). Los contadores viven en memoria de cada instancia: con varias instancias detrás de un balanceador, el límite efectivo se multiplica por el número de instancias. Las reglas se comparan contra la ruta real, por eso `MapAutoDiscoveryIAMEndpoints()` no se puede llamar dentro de `MapGroup(...)`: la app no arranca. Para servir la API bajo un prefijo, usa `app.UsePathBase("/prefijo")`.

> **Ventana de gracia del refresh token.** Cada refresh revoca el token usado, y reusarlo se trata como robo: se revoca toda la sesión del usuario. Eso también corta a un cliente legítimo que reintenta porque se perdió la respuesta, o que lanza dos refresh a la vez (una tablet con red inestable, dos pestañas). Con `RefreshTokenSettings:ReuseGracePeriodSeconds` en N (máximo 30), durante N segundos después de rotar un token, reusarlo desde el mismo cliente que hizo la rotación (misma IP y mismo User-Agent) devuelve el mismo par nuevo (mismo access y mismo refresh token) sin revocar nada, siempre que el refresh token nuevo siga vigente. Desde otro cliente se trata como robo aunque esté dentro de la ventana: revoca toda la sesión y registra el evento de seguridad, así que quien copie un token recién rotado no obtiene el par. El costo es que un cliente legítimo que cambia de red justo durante el reintento (de Wi-Fi a datos, por ejemplo) pierde la sesión y vuelve a iniciarla. Si el refresh token nuevo ya no está vigente (por ejemplo, tras un logout), responde 401 sin revocar la sesión. Pasados los N segundos, el reuso vuelve a revocar toda la sesión. El par se guarda cifrado con Data Protection en `IDistributedCache` durante esos segundos: con varias instancias, ambos deben ser compartidos (Redis o SQL Server para el caché, y un anillo de claves común), o el reintento que caiga en otra instancia recibe 401. Unos 10 a 30 segundos bastan para los reintentos.

> **Refresh token en cookie HttpOnly.** Por defecto (`RefreshTokenSettings:Delivery: Body`) el refresh token viaja en el cuerpo JSON de login, verificación 2FA, refresh, canje del código de login externo y callback de Google en modo JSON. Con `Cookie`, viaja solo en una cookie `HttpOnly`, `Secure`, `SameSite=Strict`, y el cuerpo trae `refreshToken: null`: el JavaScript de la SPA nunca lo ve ni lo guarda en storage. Con `Both`, viaja en los dos (una SPA y una app móvil contra la misma API, o mientras migras). Con la cookie en uso, `POST /auth/refresh` y `POST /auth/logout` la leen cuando el cuerpo no trae el token (el refresh acepta un cuerpo vacío), el logout la borra, y un refresh rechazado también. Cuando el refresh toma el token de la cookie, la petición debe traer el encabezado `X-Requested-With` (con cualquier valor, por ejemplo `XMLHttpRequest`); sin él responde 400 sin rotar ni borrar la cookie. Un encabezado propio obliga al navegador a hacer el preflight de CORS, así que una página de otro sitio no puede forzar la rotación aunque la cookie viaje con `SameSite: None`. `RefreshTokenSettings:Cookie` ajusta la cookie: `Name` (default `adiam_refresh_token`), `Path` (default `/auth`, relativa al `PathBase` de la app: el navegador solo la manda a las rutas de autenticación), `Domain` (default solo el host de la API) y `SameSite` (default `Strict`). La cookie es `Secure` siempre, así que la API se sirve por HTTPS. La SPA debe llamar a la API con credenciales (`fetch(..., { credentials: "include", headers: { "X-Requested-With": "XMLHttpRequest" } })`); si la API vive en otro origen, declara ese origen en `Cors:AllowedOrigins`: con la cookie en uso, la política de CORS de la librería ya permite credenciales. `SameSite: None` solo hace falta si la SPA vive en otro sitio, no solo en otro subdominio del mismo dominio.

> **Valores validados al arranque.** Estas secciones tienen default, pero un valor fuera de rango detiene el arranque en vez de dejar la protección rota sin aviso: `JwtSettings:Key` de al menos 32 bytes (se mide en UTF-8, así que un carácter acentuado cuenta doble) y `JwtSettings:ExpiryMinutes` entre 1 y 1440; `RefreshTokenSettings:TokenLength` de al menos 32, `ExpiryDays` de al menos 1, `ReuseGracePeriodSeconds` entre 0 y 30, `Cookie:Name` no vacío y `Cookie:Path` empezando con `/`; `Lockout:MaxFailedAttempts` y `Lockout:LockoutMinutes` de al menos 1; `TwoFactor:CodeLength` entre 6 y 9, y `CodeExpiryMinutes`/`ChallengeTokenExpiryMinutes` de al menos 1.

> **Varias instancias de la API.** El logout invalida el access token vigente guardando su `jti` en el `IDistributedCache` de la app. La librería registra la versión en memoria, que con una sola instancia basta. Con varias instancias detrás de un balanceador, registra en tu `Program.cs` un caché distribuido compartido (por ejemplo `builder.Services.AddStackExchangeRedisCache(...)` o `AddDistributedSqlServerCache(...)`, en cualquier orden respecto a `AddAutoDiscoveryIAM()`): si no, un token cerrado en una instancia sigue sirviendo en las otras hasta que expira. Lo mismo aplica al anillo de claves de Data Protection (ver [Instalación → Claves de Data Protection](/tutorials/getting-started/installation)) y, como dice la nota de `RateLimiting`, a los contadores de límites.

> `TwoFactor.Issuer` es distinto de `JwtSettings.Issuer` — este es el nombre que aparece en apps autenticadoras (Google Authenticator, Authy, etc.) junto a la cuenta de tu usuario al activar 2FA (viaja en la URI estándar `otpauth://totp/...&issuer=...`). Con el default sin cambiar, tus usuarios ven **"AutoDiscoveryIAM"** en su app autenticadora en vez del nombre de tu propia app — sí conviene sobreescribirlo, aunque técnicamente no sea obligatorio.

> **`Authentication.ExternalLoginRedirectUri` no es lo mismo que `Authentication.Google.RedirectUri`** — es fácil confundirlos porque ambos son "una URL de redirect", pero cumplen roles opuestos:
>
> - `Authentication.Google.RedirectUri` es la URL de **tu propia API** que **Google** llama al terminar el consentimiento (`https://tu-api/auth/google/callback`) — la registras en Google Cloud Console, y debe coincidir carácter por carácter.
> - `Authentication.ExternalLoginRedirectUri` es la URL de **tu app cliente** (SPA, móvil, lo que sea) a la que **tu propia API** redirige después de procesar ese callback — nunca se registra en Google, Google no la conoce.
>
> Es deliberadamente independiente de `Authentication:Google` (vive en `Authentication` directo, como hermana) porque aplica a **cualquier** proveedor de login externo que uses, no solo Google — un mismo valor sirve sin importar cuántos proveedores agregues a futuro.
>
> Sin configurar, el callback responde JSON directo (compatible con versiones anteriores, útil para probar el flujo con Swagger/Postman sin una app cliente real). Configurado, el callback **nunca pone el token/refreshToken reales en la URL** — eso quedaría visible en el historial del navegador mientras la SPA no limpie la URL, exactamente lo que un atacante con acceso al dispositivo/al historial querría encontrar. En vez de eso:
>
> 1. En éxito, redirige (302) con un **código de un solo uso y vida cortísima** en el fragmento (`#code=...`) — mismo patrón "authorization code" que usan Auth0/Okta/Cognito para este mismo hand-off de sesión hacia una SPA.
> 2. Tu SPA, al cargar esa ruta, canjea ese código de inmediato con `POST /auth/external-login/exchange` (`{ "code": "..." }`) — **ahí sí**, en el cuerpo de una respuesta JSON normal, recibe el `token`/`expiresAt`/`refreshToken` reales. El código es de un solo uso (un segundo intento con el mismo código falla con 401) y vence en `Authentication.ExchangeCodeExpirySeconds` (default 60s, pensado para segundos: la SPA lo canjea al instante).
> 3. En error (Google rechazó el login, cuenta inactiva, etc.), sí redirige directo con el detalle en el fragmento (`#error=...&errorDescription=...`) — ahí no hay ninguna credencial que proteger, así que no amerita un código de intercambio.
>
> Ni el código de un solo uso ni el error viajan nunca en query string ni en el cuerpo de una respuesta cacheable, para que no queden en logs de servidor ni en el header `Referer`.

> **Login con Google y cuentas existentes.** Si el usuario tiene 2FA activo, el login con Google no emite tokens: responde igual que `POST /auth/login`, con `requiresTwoFactor: true` y un `twoFactorChallengeToken` que se completa con `POST /auth/2fa/verify` (con `ExternalLoginRedirectUri`, esa respuesta es la que entrega el canje del código). Una cuenta bloqueada por intentos fallidos tampoco entra por Google mientras dure el bloqueo. La primera vez que alguien entra con Google, su identidad se vincula a la cuenta que ya tenga ese email, salvo que esa cuenta tenga `IsAdmin`, `IsDevelopment` o `IsSuAdmin`: esas responden 409 y vinculan Google solo con la sesión iniciada (`POST /auth/google/link`), porque quien controle una cuenta de Google con ese correo (por ejemplo, el administrador del dominio en Google Workspace) obtendría sus privilegios. Si la cuenta existente nunca verificó su email, al vincularla se invalida su contraseña (el dueño define una nueva con el restablecimiento) y los tokens ya emitidos: cualquiera pudo registrarla con ese correo y una contraseña propia, esperando a que el dueño entrara con Google. Aprobar una cuenta (`Registration:RequireAdminApproval`) o crearla con contraseña desde `POST /users/by-admin` no cuenta como verificar su email: el administrador no tiene forma de probar de quién es el correo, así que esa defensa sigue aplicando hasta que el usuario lo confirme.

> **Secretos y tokens en la base de datos.** Los tokens de activación y de restablecimiento de contraseña se guardan solo como hash (SHA-256): quien lea la base de datos no puede usar ningún enlace pendiente. Por eso cada reenvío de activación genera un enlace nuevo y el anterior deja de servir. El secreto de Authenticator (TOTP) se guarda cifrado con ASP.NET Core Data Protection, y un código ya aceptado no se acepta de nuevo, aunque siga dentro de su ventana de 30 segundos. El token de reto del segundo factor (`twoFactorChallengeToken`) también queda solo como hash: el código de 6 dígitos no basta para completar un login sin él. El anillo de claves de Data Protection tiene que persistir entre reinicios y ser el mismo en todas las instancias: ver [Instalación → Claves de Data Protection](/tutorials/getting-started/installation).

> **Registro y enumeración de cuentas.** `POST /users` con un email ya registrado responde igual que un registro nuevo y no crea nada: el dueño del correo recibe un aviso de que alguien intentó registrarse con él. Un username repetido sí responde 409. `DELETE /users/me` pide la contraseña actual en el cuerpo (`{ "currentPassword": "..." }`): un access token robado no alcanza para borrar la cuenta. Con una cuenta bloqueada por intentos fallidos, solo quien da la contraseña correcta recibe el aviso de bloqueo; con una incorrecta, `POST /auth/login` responde igual que con un email que no existe. `POST /users/me/email-change-requests` tampoco revela si el email nuevo ya pertenece a otra cuenta: la solicitud queda pendiente igual, y se rechaza al ejecutarla.

> **2FA en cuentas sin contraseña.** Una cuenta creada solo con login social no tiene contraseña que pedir al cambiar su 2FA. Si tiene Authenticator activo, activar el 2FA por correo, volver a enrolar el Authenticator o desactivarlo (`POST /users/me/2fa/...`) exige además un código válido del Authenticator actual en el campo `code` del cuerpo: un access token robado no alcanza para apagarlo ni para llevarlo a otro dispositivo.

## Plantilla completa de `appsettings.json`

Esta es la plantilla real usada por el `SampleHost` de este mismo repo, con todas las secciones de las 3 tablas de arriba en un solo archivo — útil como punto de partida sin importar si instalaste el paquete desde NuGet o clonaste este repositorio (en cuyo caso, es exactamente el contenido de `samples/AutoDiscoveryIAM.SampleHost/appsettings.template.json`, y puedes copiarlo directo en vez de escribirlo a mano). Reemplaza cada `{{PLACEHOLDER}}` por tu propio valor real:

````code-block
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server={{DB_HOST}},{{DB_PORT}};Initial Catalog={{DB_NAME}};Persist Security Info=False;User ID={{DB_USER}};Password={{DB_PASSWORD}};MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=False;Connection Timeout=30;"
  },
  "JwtSettings": {
    "Key": "{{JWT_SECRET_KEY}}",
    "Issuer": "{{JWT_ISSUER}}",
    "Audience": "{{JWT_AUDIENCE}}",
    "ExpiryMinutes": 15
  },
  "RefreshTokenSettings": {
    "ExpiryDays": 7,
    "TokenLength": 32,
    "ReuseGracePeriodSeconds": 0,
    "Delivery": "Body",
    "Cookie": {
      "Name": "adiam_refresh_token",
      "Path": "/auth",
      "SameSite": "Strict"
    }
  },
  "Authentication": {
    "ExternalLoginRedirectUri": "{{EXTERNAL_LOGIN_REDIRECT_URI = https://tu-app-frontend.com/auth/callback (opcional, distinto de Google.RedirectUri)}}",
    "ExchangeCodeExpirySeconds": 60,
    "Google": {
      "Enabled": false,
      "ClientId": "{{GOOGLE_CLIENT_ID}}",
      "ClientSecret": "{{GOOGLE_CLIENT_SECRET}}",
      "RedirectUri": "{{GOOGLE_REDIRECT_URI = https://tu-api.com/auth/google/callback}}"
    }
  },
  "BaseUrl": "{{PUBLIC_BASE_URL}}",
  "Cors": {
    "AllowedOrigins": ["{{CORS_ALLOWED_ORIGIN}}"]
  },
  "Mail": {
    "Provider": "Smtp",
    "From": { "Email": "{{MAIL_FROM_EMAIL}}", "Name": "{{MAIL_FROM_NAME}}" },
    "Smtp": {
      "Host": "{{MAIL_SMTP_HOST}}",
      "Port": "{{MAIL_SMTP_PORT}}",
      "User": "{{MAIL_SMTP_USER}}",
      "Password": "{{MAIL_SMTP_PASSWORD}}",
      "UseSsl": true
    }
  },
  "Activation": {
    "EmailVerificationTokenLength": 64,
    "EmailVerificationTokenExpiryMinutes": 30,
    "ActivationResendCooldownSeconds": 60,
    "ActivationResendMaxPerHour": 5
  },
  "Registration": {
    "EmailPasswordEnabled": true,
    "ExternalProviders": { "Google": true },
    "RequireAdminApproval": false,
    "PasswordSetupLinkExpiryHours": 72
  },
  "PasswordReset": { "TokenLength": 64, "TokenExpiryMinutes": 30 },
  "Lockout": { "MaxFailedAttempts": 5, "LockoutMinutes": 60 },
  "TwoFactor": {
    "CodeLength": 6,
    "CodeExpiryMinutes": 5,
    "ChallengeTokenExpiryMinutes": 10,
    "Issuer": "AutoDiscoveryIAM"
  },
  "PasswordStruct": {
    "Regex": "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[#%*!|_-])[A-Za-z\\d#%*!|_-]{8,}$",
    "Description": "Contraseña de al menos 8 caracteres, con al menos 1 mayúscula, 1 minúscula, 1 número y 1 carácter especial de # % * ! | - _. Solo se permiten letras, números y esos caracteres especiales.",
    "Message": {
      "Valid": "Contraseña válida.",
      "Invalid": "La contraseña debe tener al menos 8 caracteres e incluir al menos 1 mayúscula, 1 minúscula, 1 número y 1 carácter especial (# % * ! | - _)."
    }
  },
  "AllowedHosts": "*"
}
```
````

`RefreshTokenSettings`, `Activation`, `Registration`, `PasswordReset`, `Lockout`, `TwoFactor` y `PasswordStruct` ya traen su default seguro embebido en código (ver [Configuración recomendada](/references/configuration-testing/configuration#configuracion-recomendada) arriba) — se incluyen aquí completos solo como referencia de cómo se ven si decides sobreescribirlos, no porque sea obligatorio declararlos.

---
