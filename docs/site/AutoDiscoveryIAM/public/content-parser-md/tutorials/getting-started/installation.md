---
[slug]: installation
[title]: Instalación
[description]: Instala FrugoCorp.AspNetCore.AutoDiscoveryIAM paso a paso: crea el proyecto, agrega el paquete NuGet, conecta tu base de datos, configura appsettings.json y aplica las migraciones iniciales con dotnet ef.
[keywords]: instalación AutoDiscoveryIAM, dotnet add package, appsettings.json JWT, dotnet ef migrations, RBAC ASP.NET Core
---

# Instalación

## Personaliza el nombre de tu proyecto

A lo largo de esta guía (y del resto del sitio) verás el nombre genérico `TuProyecto` en comandos, namespaces y rutas de carpeta. Es intencional: así el ejemplo se entiende sin importar cómo se llame tu proyecto real. Reemplázalo por el nombre de tu proyecto al copiar cada comando.

**Consejo:** en el sitio de documentación no hace falta reemplazarlo a mano. Escribe el nombre real de tu proyecto en el campo **PROYECTO** del panel "Mis Variables", en la barra derecha, y los comandos y ejemplos de código mostrarán tu nombre en vez de `TuProyecto`, listos para copiar y pegar. El valor se guarda únicamente en el almacenamiento local de tu navegador: nunca se envía a ningún servidor.

## Crea tu proyecto

Crea la carpeta raíz del proyecto:

````code-block
```bash
mkdir (--PROYECTO:pascal--)
```
````

Entra a esa carpeta:

````code-block
```bash
cd (--PROYECTO:pascal--)
```
````

Crea el archivo de solución (`.sln`):

````code-block
```bash
dotnet new sln
```
````

Crea la carpeta `src`, donde va a vivir el proyecto web:

````code-block
```bash
mkdir src
```
````

Entra a `src`:

````code-block
```bash
cd src
```
````

Crea el proyecto web mínimo dentro de `src/(--PROYECTO:pascal--)`:

````code-block
```bash
dotnet new web -n (--PROYECTO:pascal--)
```
````

Regresa a la raíz del repositorio, donde está el `.sln`:

````code-block
```bash
cd ..
```
````

Agrega el proyecto recién creado a la solución:

````code-block
```bash
dotnet sln add src/(--PROYECTO:pascal--)/(--PROYECTO:pascal--).csproj
```
````

## Agrega la librería a tu proyecto

````code-block
```bash
dotnet add package FrugoCorp.AspNetCore.AutoDiscoveryIAM
```
````

> ⚠️ Si clonaste este repositorio desde el código fuente (sin que el paquete esté publicado todavía en NuGet.org), este comando **no va a funcionar tal cual** — ve primero a [Empaquetar la librería desde tu fork y probarla en otro proyecto](/tutorials/getting-started/package-library).

## Necesitas una base de datos

La librería no trae proveedor de base de datos ni migraciones empaquetadas — eliges tu proveedor (SqlServer/Npgsql/Sqlite/etc.) e instalas su paquete **antes** de escribir el código que lo usa, para no toparte con un error de compilación por un método que todavía no existe en tu proyecto:

Instala el proveedor de base de datos (cambia `SqlServer` por `Npgsql`/`Sqlite`/etc. si usas otro):

````code-block
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```
````

> 💡 `Microsoft.EntityFrameworkCore.SqlServer` 10.0.x arrastra `System.Security.Cryptography.Pkcs` 9.0.4, que tiene avisos de seguridad conocidos (GHSA-846f-f764-76mx, GHSA-2fmv-rv2c-59x7): `dotnet build` muestra `NU1902`/`NU1903`, y con `TreatWarningsAsErrors` el build falla. Viene del proveedor, no de esta librería. Fija la versión corregida en tu proyecto: `dotnet add package System.Security.Cryptography.Pkcs --version 10.0.14`.

Requerido por `dotnet ef` en tu propio proyecto de inicio — no lo usa la librería en tiempo de ejecución:

````code-block
```bash
dotnet add package Microsoft.EntityFrameworkCore.Design
```
````

Instalar el paquete no basta — necesitas además una base de datos real y alcanzable. Si todavía no tienes una instancia de SQL Server a la mano, este mismo repositorio trae un contenedor Docker desechable listo para desarrollo local (ver [Docker: db/](/tutorials/docker/docker-db)):

Entra a la carpeta del contenedor de base de datos de este repo:

````code-block
```bash
cd docker/db
```
````

Copia la plantilla de variables de entorno (edítala antes de continuar):

````code-block
```bash
cp .env.template .env
```
````

Construye y levanta el contenedor en segundo plano:

````code-block
```bash
docker-compose up --build -d
```
````

## Configura appsettings.json

Configuración mínima obligatoria (sin esto la app no arranca):

````code-block
```json src/(--PROYECTO:pascal--)/appsettings.json
{
  "ConnectionStrings": { "DefaultConnection": "..." },
  "JwtSettings": { "Key": "...", "Issuer": "...", "Audience": "..." }
}
```
````

Ver la tabla completa de opciones en [Configuración](/references/configuration-testing/configuration) más abajo, incluida una [plantilla completa de `appsettings.json`](/references/configuration-testing/configuration#plantilla-completa-de-appsettingsjson) lista para copiar y rellenar.

## Crea tu DbContext

En `Program.cs` puedes terminar registrando **solo el `DbContext` de la librería**, o **también uno propio** para tus propias entidades de negocio, dependiendo de si tu app ya tiene (o va a tener pronto) alguna:

- **`AutoDiscoveryIAMDbContext`** — el de la librería (`Users`/`Roles`/`Permissions`). No lo escribes tú, solo lo registras — eso lo ves en el siguiente paso, [Configura Program.cs](/tutorials/getting-started/installation#configura-programcs).
- **Tu propio `ApplicationDbContext`** — para tus propias entidades (lo que sea que modele tu app). Si todavía no lo tienes, créalo ahora. Hay dos formas válidas de construirlo — ver [Elige tu propio DbContext: uno o dos](/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos) para el detalle completo de ambas y su tradeoff; en resumen, la forma recomendada para empezar (un solo `DbContext`, heredando el de la librería):

  ````code-block
  ```csharp src/(--PROYECTO:pascal--)/Data/ApplicationDbContext.cs
  public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentAuditUserService? currentAuditUserService = null)
    : AutoDiscoveryIAMDbContext(options, currentAuditUserService)
  {
    // tus propios DbSet<T> aquí
  }
  ```
  ````

  La plantilla completa (con una entidad de ejemplo, `OnModelCreating`, etc.) está en [Reutilizar auditoría y soft-delete en tus propias entidades](/docs/security-rbac/audit-soft-delete).

Si tu app todavía no tiene ninguna entidad propia, no pasa nada por saltarte esto: registra solo `AutoDiscoveryIAMDbContext` por ahora y agrega tu `ApplicationDbContext` más adelante, cuando lo necesites — el paso [Configura Program.cs](/tutorials/getting-started/installation#configura-programcs) siguiente muestra cómo se ve cada opción registrada.

## Extras opcionales: Validator, OpenAPI, Logging y Diagnóstico

Ninguno de estos 4 es requerido por AutoDiscoveryIAM para funcionar — son piezas comunes que la mayoría de equipos termina queriendo para sus propios slices (ver el ejemplo completo en [Ejemplo: arquitectura Vertical Slice en tu propia app](/tutorials/examples/vertical-slice-example)). Se explican aquí, antes de [Configura Program.cs](/tutorials/getting-started/installation#configura-programcs), porque ese paso ya incluye las líneas de cada uno (marcadas como opcionales) — así sabes de antemano si te sirven o si te conviene borrarlas.

- **Validator (FluentValidation)** — valida un `Command` antes de que llegue al Handler. **No necesitas instalar nada**: `FluentValidation` ya viene como dependencia transitiva de `FrugoCorp.AspNetCore.AutoDiscoveryIAM` (la librería la usa para sus propios Commands), así que ya está disponible en tu proyecto en cuanto instalas el paquete. Solo agrega `using FluentValidation;` y hereda de `AbstractValidator<TCommand>`.

- **OpenAPI (documentación de la API)** — `AddAutoDiscoveryIAM()` ya registra el documento OpenAPI (nativo, `Microsoft.AspNetCore.OpenApi`, incluido en el SDK Web de .NET) junto con los transformers que hacen que la documentación que escribas por endpoint (`EndpointOperationDocs`, ver [Docs/&lt;Slice&gt;OperationDocs.cs](/tutorials/examples/vertical-slice-example)) realmente aparezca en el JSON generado — no necesitas llamar `AddOpenApi(...)` tú mismo ni cablear ningún transformer. Lo que sí necesitas instalar es un **renderizador** (la interfaz visual que lee ese documento) — elige uno o ambos:

  Instala el renderizador Swagger UI:

  ````code-block
  ```bash
  dotnet add package Swashbuckle.AspNetCore.SwaggerUI
  ```
  ````

  Instala el renderizador Scalar (alternativa, no excluyente con Swagger UI):

  ````code-block
  ```bash
  dotnet add package Scalar.AspNetCore
  ```
  ````

  > ⚠️ Recuerda marcar tu propio endpoint `MapOpenApi(...)` con `.WithMetadata(new PermissionsNotRequiredAttribute("..."))` — ver el aviso 💡 en [Configura Program.cs](/tutorials/getting-started/installation#configura-programcs) más abajo. Sin esto, tu documentación responde 401 y ningún renderizador puede mostrarla.

- **Logging** — no requiere ningún paquete (usa `Microsoft.Extensions.Logging`, ya incluido). El patrón que usa la librería internamente (un `I{Entidad}Logger` público por feature, envolviendo `ILoggerBase<T>`, en vez de llamar `ILogger<T>` directo en cada Handler) es reutilizable en tu propia app — ver `Infrastructure/Logging/` en el [ejemplo completo](https://github.com/frugocorp/AutoDiscoveryIAM/tree/master/samples/SimpleCode/Features/UserProfiles).

## Configura Program.cs

`AutoDiscoveryIAMDbContext` es el `DbContext` de la **librería** (`Users`/`Roles`/`Permissions`) — no lo escribes tú, solo lo registras. Cómo lo registras depende de si vas a tener tu propio `ApplicationDbContext` y, si sí, de cuál de las dos opciones de [Elige tu propio DbContext: uno o dos](/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos) elegiste:

````code-block
```csharp src/(--PROYECTO:pascal--)/Program.cs
// Program.cs de tu proyecto -- estos 5 usings son obligatorios, ninguno viene incluido
// por los usings implícitos del SDK Web (ImplicitUsings), ni siquiera Microsoft.EntityFrameworkCore
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Composition;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
// Los siguientes 2 solo si vas a exponer/renderizar la documentación OpenAPI (ver "Extras
// opcionales" arriba) -- borra los que no apliquen (Scalar.AspNetCore solo si instalaste ese
// renderizador; Microsoft.AspNetCore.OpenApi solo hace falta si personalizas el 3er argumento de
// AddAutoDiscoveryIAM(), ver el aviso 💡 más abajo):
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using Scalar.AspNetCore;
// Los siguientes 2 solo si ya tienes tu propio ApplicationDbContext y/o tus propias features (ver
// más abajo y "Ejemplo: arquitectura Vertical Slice en tu propia app") -- "(--PROYECTO:pascal--)" aquí es tu
// namespace real (ej. "ReMemSelf"), no lo dejes literal:
using (--PROYECTO:pascal--).Data;
using (--PROYECTO:pascal--).Features;

var builder = WebApplication.CreateBuilder(args);

// Lee "ConnectionStrings:DefaultConnection" de tu appsettings.json (ver Configura appsettings.json
// más abajo) -- "(--PROYECTO:pascal--)" aquí es solo un nombre de variable, no lo del csproj.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
```
````

Después de esa parte común, el registro del/los `DbContext` cambia según tu caso — usa **uno** de estos 3 bloques, no varios a la vez:

**Si todavía no tienes entidades propias** (registra solo el de la librería, agrega tu `ApplicationDbContext` cuando lo necesites):

````code-block
```csharp src/(--PROYECTO:pascal--)/Program.cs
builder.Services.AddDbContext<AutoDiscoveryIAMDbContext>(options => options.UseSqlServer(connectionString));
```
````

**Opción 1 — un solo `DbContext`** (recomendada para empezar; tu `ApplicationDbContext` hereda `AutoDiscoveryIAMDbContext`, ver [Crea tu DbContext](/tutorials/getting-started/installation#crea-tu-dbcontext) arriba):

````code-block
```csharp src/(--PROYECTO:pascal--)/Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<AutoDiscoveryIAMDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
```
````

**Opción 2 — dos `DbContext` separados** (ver el porqué del `MigrationsHistoryTable` en el aviso 💡 debajo):

````code-block
```csharp src/(--PROYECTO:pascal--)/Program.cs
builder.Services.AddDbContext<AutoDiscoveryIAMDbContext>(options => options.UseSqlServer(
  connectionString,
  sql => sql
    .MigrationsAssembly("(--PROYECTO:pascal--)")
    .MigrationsHistoryTable("__EFMigrationsHistory_AutoDiscoveryIAM")));
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
```
````

Y el resto de `Program.cs`, igual sin importar cuál de los 3 bloques anteriores hayas usado:

````code-block
```csharp src/(--PROYECTO:pascal--)/Program.cs
builder.Services.AddAutoDiscoveryIAM(builder.Configuration);

// Obligatorio en producción: dónde se guarda el anillo de claves de Data Protection, con el que
// la librería cifra el secreto de Authenticator (2FA) de cada usuario. Tiene que sobrevivir a
// reinicios y redespliegues y ser el mismo en todas las instancias -- ver "Claves de Data
// Protection" más abajo. La ruta es un ejemplo: usa un volumen de Docker o una carpeta persistente
// del servidor. Requiere "using Microsoft.AspNetCore.DataProtection;".
builder.Services.AddDataProtection()
  .SetApplicationName("TuProyecto")
  .PersistKeysToFileSystem(new DirectoryInfo("/var/lib/tuproyecto/dataprotection-keys"));

// Opcional: solo si ya tienes (o vas a tener) tus propias features (Commands/Queries) además de
// las de la librería -- ej. el UserProfile de "Ejemplo: arquitectura Vertical Slice en tu propia
// app" más abajo. AddFeaturesDI() aquí es TU PROPIO agregador (Features/FeaturesDependencyInjection.cs
// en tu proyecto), no algo que traiga la librería -- si todavía no tienes ninguna feature propia,
// omite esta línea por ahora y agrégala cuando crees la primera.
builder.Services.AddFeaturesDI();

// Nombre del documento que AddAutoDiscoveryIAM() ya registró arriba (default "v1" -- pásalo como
// segundo argumento ahí si necesitas otro nombre). Solo se reutiliza aquí para las rutas de abajo;
// borra esta línea y las 3 de "Opcional" más adelante si no vas a exponer/renderizar OpenAPI.
var openApiDocumentName = "v1";

var app = builder.Build();

// Opcional, pero obligatorio si tu API corre detrás de un reverse proxy que termina TLS (nginx,
// Traefik, etc.) -- sin esto, Request.Scheme siempre ve "http" (lo que tu API recibe internamente),
// aunque el sitio sea https por fuera, y eso rompe cosas como las URLs que genera Swagger UI/Scalar.
// Debe ir ANTES de UseAutoDiscoveryIAM(). La librería ya configura qué headers confiar
// (X-Forwarded-Proto/X-Forwarded-For) y en qué rango de red confiar en AddAutoDiscoveryIAM() -- ver
// el aviso 💡 debajo si tu proxy no está en una red Docker con rango 172.16.0.0/12.
app.UseForwardedHeaders();

app.UseAutoDiscoveryIAM();
app.MapAutoDiscoveryIAMEndpoints();

// Mismo caso que AddFeaturesDI() arriba -- tu propio agregador de endpoints
// (Features/FeaturesEndpointMapping.cs), omite esta línea si todavía no tienes ninguna feature propia.
app.MapAllFeatureEndpoints();

// Opcional: borra este bloque y los 2 de abajo si no vas a exponer/renderizar OpenAPI -- el
// documento se sigue generando en memoria igual (no cuesta nada dejarlo sin publicar).
app.MapOpenApi($"/openapi/{openApiDocumentName}.json")
  .WithMetadata(new PermissionsNotRequiredAttribute("Documento OpenAPI público")); // obligatorio si dejas este bloque -- ver el aviso 💡 debajo

// Swagger UI -- requiere el paquete Swashbuckle.AspNetCore.SwaggerUI.
app.UseSwaggerUI(c =>
{
  c.SwaggerEndpoint($"/openapi/{openApiDocumentName}.json", "(--PROYECTO:pascal--) API");
  c.RoutePrefix = "swagger";
});

// Scalar (renderizador alternativo, no excluyente con Swagger UI de arriba) -- requiere el paquete Scalar.AspNetCore.
app.MapScalarApiReference("/scalar", options => options.WithOpenApiRoutePattern($"/openapi/{openApiDocumentName}.json"));

// Obligatorio -- sin esto, las migraciones crean las tablas pero NUNCA se insertan los roles
// (AdminAutoDiscoveryIAM/UserDefault), permisos, ni el usuario de sistema: no es parte de la
// migración, es una llamada aparte. Idempotente (seguro en cada arranque, incluso con varias
// instancias arrancando en paralelo). Requiere "using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;".
// No crea cuentas de ejemplo: para tenerlas en desarrollo, pasa includeDevelopmentUsers: true
// (la librería lo ignora fuera de Development/Testing).
await app.SeedAutoDiscoveryIAMDefaultDataAsync();

app.Run();
```
````

> 💡 `using (--PROYECTO:pascal--).Data;`/`using (--PROYECTO:pascal--).Features;` son **tu propio** namespace (ajusta `(--PROYECTO:pascal--)` al de tu proyecto real, ej. `ReMemSelf`), no algo que venga con el paquete. Ojo con el autofix del IDE en `AddFeaturesDI()`/`MapAllFeatureEndpoints()`: la librería **también** tiene sus propios métodos con esos mismos nombres (en `FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features`, la misma convención que copia `samples/SimpleCode` a propósito) — normalmente nunca necesitas importar ese namespace tú mismo (`AddAutoDiscoveryIAM()`/`MapAutoDiscoveryIAMEndpoints()` ya los llaman por dentro), así que si el autofix te ofrece las dos opciones, la que quieres es la de tu propio proyecto, no la de la librería. Elegir la incorrecta compila igual (siempre que solo agregues uno de los dos usings, no ambos a la vez -- si importas los dos, ahí sí el compilador marca error de llamada ambigua, CS0121), pero nunca registra ni mapea tus propias features.

> 💡 El "secure by default" de la librería protege **todos** los endpoints del pipeline, no solo los de la librería — incluido cualquier endpoint que agregues tú mismo (tu propio `app.MapOpenApi()`, o hasta el `app.MapGet("/", () => "Hello World!")` de ejemplo que trae `dotnet new web`). Ninguno de esos tiene un `Permission` propio (no viven bajo un namespace `...Features.<Feature>.<Acción>`, ver [Permisos para tus propios endpoints](/docs/security-rbac/authorization#permisos-para-tus-propios-endpoints)), así que sin excepción explícita responden 401/403 en vez de funcionar. Si quieres conservar un endpoint propio así, márcalo con `.WithMetadata(new PermissionsNotRequiredAttribute("..."))`; si no lo necesitas (como el `Hello World!` de la plantilla por default), simplemente bórralo. Validado en vivo instalando el paquete en un proyecto externo limpio.

> 💡 `AddAutoDiscoveryIAM()` acepta dos parámetros opcionales para el documento OpenAPI que registra por dentro: `openApiDocumentName` (default `"v1"`, cámbialo solo si ya usas otro nombre en tu proyecto) y `configureOpenApi` (un `Action<OpenApiOptions>` que corre *después* de los transformers de la librería, sobre el mismo documento — úsalo para agregar tus propios transformers o, si tu renderizador lo necesita, fijar `options.OpenApiVersion` — Swagger UI, por ejemplo, no renderiza `3.1.x` todavía). Ninguno de los dos es obligatorio.

> 💡 `AddAutoDiscoveryIAM()` ya registra `ForwardedHeadersOptions` (confía en `X-Forwarded-Proto`/`X-Forwarded-For` viniendo de la red `172.16.0.0/12` -- el rango que Docker asigna por defecto a sus redes bridge), pero **no** llama `app.UseForwardedHeaders()` por ti: es una decisión de topología de red específica de cada despliegue, así que queda explícita en tu propio `Program.cs`. Si tu proxy no vive en una red Docker dentro de ese rango (por ejemplo, corre fuera de Docker, o en una red con una subred distinta), esos headers llegan pero la librería no confía en el origen y los ignora en silencio -- `Request.Scheme` se sigue quedando en `http`. Verifica la IP/subred real de tu proxy con `docker network inspect <red>` si `UseForwardedHeaders()` no resuelve el problema. Para confiar en otra red o en IPs puntuales, declara `ForwardedHeaders:KnownNetworks` (CIDR) o `ForwardedHeaders:KnownProxies` en tu configuración: reemplazan a `172.16.0.0/12`.

> 💡 `.MigrationsHistoryTable("__EFMigrationsHistory_AutoDiscoveryIAM")` **solo aplica a la Opción 2** (dos `DbContext` separados) — con dos contextos hay dos historiales de migraciones independientes, y este nombre evita que se pisen entre sí en la misma base de datos, dejando además el `__EFMigrationsHistory` default libre para el tuyo. Con la Opción 1 (un solo `DbContext`) esto no aplica: solo hay un modelo y un historial de migraciones para todo, así que no necesitas (ni debes) configurar `MigrationsHistoryTable` en absoluto. Ver [Elige tu propio DbContext: uno o dos](/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos) para el resto del tradeoff entre ambas.

> 💡 Llama `app.MapAutoDiscoveryIAMEndpoints()` sobre la aplicación, nunca dentro de `MapGroup("/api")`: los límites de peticiones por defecto se comparan contra la ruta real (`/auth/login`, `/users/password/reset/request`...) y con un prefijo de grupo dejarían de aplicarse, así que la app no arranca si lo intentas. Para servir la API bajo un prefijo, usa `app.UsePathBase("/api")` antes del resto del pipeline.

## Claves de Data Protection (obligatorio en producción)

La librería cifra el secreto de Authenticator (TOTP) de cada usuario con [ASP.NET Core Data Protection](https://learn.microsoft.com/aspnet/core/security/data-protection/introduction) antes de guardarlo en la base de datos. Las claves con las que cifra (el *anillo de claves*) **no** viven en la base de datos: ASP.NET Core las guarda en una carpeta. Si esa carpeta se pierde o cambia, los secretos ya guardados no se pueden descifrar y **los usuarios con Authenticator no pueden completar el segundo factor** hasta que se les desactive el 2FA directo en la base de datos y vuelvan a enrolarse. La API registra ese caso como error `Critical` (EventId 9101).

Por eso, antes de publicar tu API, decide dónde vive el anillo de claves (el bloque `AddDataProtection()` de la sección anterior):

| Dónde corre tu API | Qué hacer |
| --- | --- |
| Tu máquina, en desarrollo | Nada: ASP.NET Core guarda las claves en tu perfil de usuario y persisten solas. |
| Un servidor, una sola instancia | `PersistKeysToFileSystem(...)` hacia una carpeta del servidor que no se borre en cada despliegue, con permisos solo para el usuario que corre la API. |
| Docker | La carpeta de `PersistKeysToFileSystem(...)` tiene que ser un **volumen** (`volumes:` en tu `docker-compose.yml`): el sistema de archivos del contenedor se pierde cada vez que se recrea. |
| Varias instancias (balanceador, réplicas) | Todas comparten el mismo anillo (una carpeta compartida, o un almacén como Redis o Azure Blob Storage con su paquete `Microsoft.AspNetCore.DataProtection.*`) y el mismo `SetApplicationName(...)`. |

> 💡 Sin el bloque `AddDataProtection()` la app arranca y funciona igual, porque la librería registra Data Protection con su ubicación por defecto. En Docker esa ubicación queda dentro del contenedor, así que el problema aparece en el siguiente redespliegue, cuando los usuarios con Authenticator dejan de poder entrar.

> 💡 En Linux las claves se guardan sin cifrar. Quien tenga a la vez las claves y un respaldo de la base de datos puede descifrar los secretos de Authenticator: guárdalas fuera de los respaldos de la base de datos, o cífralas con `ProtectKeysWithCertificate(...)`.

## Aplica las migraciones

````card-code-block
@description
Los comandos `dotnet ef` requieren la herramienta global `dotnet-ef`, instalada una sola vez por máquina (no viene con el SDK de .NET).

@command bash
dotnet tool install --global dotnet-ef

@note<Nota>
Si no la tienes instalada, `dotnet ef` responde "no se pudo ejecutar porque no se especificó el comando o archivo" -- ese es el error que resuelve este comando. Ver la [documentación oficial de herramientas de EF Core](https://learn.microsoft.com/ef/core/cli/dotnet#installing-the-tools) para más detalle.
````

````card-code-block
@description
Si ya tenías `dotnet-ef` instalado pero de una versión más vieja que tu SDK, actualízalo para no arrastrar una advertencia de versión desincronizada en cada comando.

@command bash
dotnet tool update --global dotnet-ef

@note<Nota>
Si te saltas este paso vas a ver una advertencia como "The Entity Framework tools version 'X' is older than that of the runtime 'Y'" -- las migraciones se aplican igual (validado en vivo), pero conviene actualizar para no arrastrarla.
````

Si todavía no tienes tu propio `ApplicationDbContext`, o si lo tienes pero elegiste la **Opción 2** (dos `DbContext` separados) de [Configura Program.cs](/tutorials/getting-started/installation#configura-programcs):

Genera la migración inicial de las tablas de la librería:

````code-block
```bash
dotnet ef migrations add InitialCreate --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
```
````

Aplica esa migración a la base de datos:

````code-block
```bash
dotnet ef database update
```
````

Si elegiste la **Opción 1** (un solo `DbContext`), apunta el `--context` al tuyo — genera y aplica en un solo paso las tablas de la librería y las tuyas juntas, porque comparten el mismo modelo:

Genera la migración inicial (tablas de la librería + las tuyas juntas, porque comparten el mismo modelo):

````code-block
```bash
dotnet ef migrations add InitialCreate --context ApplicationDbContext
```
````

Aplica esa migración a la base de datos:

````code-block
```bash
dotnet ef database update --context ApplicationDbContext
```
````

Para ver todo esto ya funcionando sin integrarlo primero a tu propio proyecto, corre el [`SampleHost`](/tutorials/getting-started/run-samplehost) de este mismo repo.

> 💡 **Patrón opcional: esperar la base de datos con reintentos al arrancar.** Si tu API y tu SQL Server corren en contenedores de Docker independientes, sin ningún `depends_on`/`healthcheck` que los ordene (el mismo criterio que usan `docker/api/` y `docker/db/` de este repo — cada stack se puede levantar por separado, ver [Docker](/docs/architecture/docker-architecture)), nada garantiza que la base de datos ya acepte conexiones cuando arranca tu proceso. `SampleHost`/`Infrastructure/DatabaseInitialization.cs` resuelve esto con un reintento (10 intentos, 5s de espera cada uno) antes de continuar, y de paso aplica migraciones pendientes solo en Development (`dotnet ef database update` sigue siendo necesario en Producción). Es un patrón real y funcional, no solo un ejemplo — pero también es completamente opcional: no es parte de `AddAutoDiscoveryIAM()`/`UseAutoDiscoveryIAM()`, y un equipo cuyo propio orquestador ya garantice el orden de arranque (o que simplemente prefiera aplicar migraciones a mano antes de levantar la app) puede omitirlo sin perder nada. Cópialo de `samples/AutoDiscoveryIAM.SampleHost/Infrastructure/DatabaseInitialization.cs` si te sirve tal cual.

---
