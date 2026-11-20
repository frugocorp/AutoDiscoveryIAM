# AutoDiscoveryIAM

![AutoDiscoveryIAM](https://raw.githubusercontent.com/frugocorp/AutoDiscoveryIAM/main/docs/site/AutoDiscoveryIAM/public/images/FrugoCorp-Logo-300x300.jpg)

[![nuget](https://img.shields.io/nuget/v/FrugoCorp.AspNetCore.AutoDiscoveryIAM)](https://www.nuget.org/packages/FrugoCorp.AspNetCore.AutoDiscoveryIAM)

[GitHub](https://github.com/frugocorp/AutoDiscoveryIAM) | [NuGet](https://www.nuget.org/packages/FrugoCorp.AspNetCore.AutoDiscoveryIAM) | [Sitio](https://autodiscoveryiam.frugocorp.com) | [FrugoCorp](https://frugocorp.com)

`FrugoCorp.AspNetCore.AutoDiscoveryIAM` agrega autenticación JWT completa y RBAC dinámico a tus APIs de ASP.NET Core con minimal APIs. Los permisos se descubren solos a partir de tus endpoints reales, así que no tienes que declararlos a mano ni mantenerlos sincronizados cuando agregas uno nuevo.

## Empieza aquí

- [Instalación](#instalación-1): agrega la librería a tu proyecto paso a paso.
- [Introducción](#introducción): qué es y cómo está organizado el repositorio.
- [Qué incluye hoy](#qué-incluye-hoy): las funcionalidades disponibles.
- [Autorización con niveles de acceso](#autorización-con-niveles-de-acceso): cómo se decide quién puede hacer qué.
- [Configuración](#configuración): todas las claves disponibles.

## Licencia

[MIT](https://autodiscoveryiam.frugocorp.com/license): puedes usarla, modificarla y redistribuirla libremente. Cada dependencia conserva su propia licencia; `Isopoh.Cryptography.Argon2`, de Michael Heyman, se distribuye bajo [Creative Commons Attribution 4.0](https://creativecommons.org/licenses/by/4.0/), y si distribuyes una aplicación que la incluye, debes conservar ese crédito.

## Apartados

- [Docs](#docs) -- Conceptos, seguridad y arquitectura de AutoDiscoveryIAM.
- [Tutoriales](#tutoriales) -- Guías paso a paso para instalar, extender y desplegar AutoDiscoveryIAM.
- [Referencias](#referencias) -- Configuración, pruebas y desarrollo de AutoDiscoveryIAM.

---

## Docs

[REGRESAR A APARTADOS](#apartados)

---

## Índice Docs

- [Fundamentos](#fundamentos) -- Qué es AutoDiscoveryIAM y qué incluye hoy.
  - [Introducción](#introducción) -- Qué es AutoDiscoveryIAM, qué problema resuelve y cómo está organizado su repositorio.
  - [Qué incluye hoy](#qué-incluye-hoy) -- Funcionalidades disponibles hoy en la librería: autenticación, RBAC dinámico, 2FA y más.
- [Seguridad y RBAC](#seguridad-y-rbac) -- Autorización, auditoría, soft-delete y manejo de datos de terceros.
  - [Autorización con niveles de acceso](#autorización-con-niveles-de-acceso) -- Niveles de acceso, permisos descubiertos automáticamente y los dos ejes de autorización independientes.
  - [Reutilizar auditoría y soft-delete](#reutilizar-auditoría-y-soft-delete) -- Cómo reutilizar la auditoría y el soft-delete de la librería en tus propias entidades.
  - [Privacidad y datos de terceros](#privacidad-y-datos-de-terceros) -- Qué datos personales recibe y almacena la librería con proveedores de identidad externos y qué controles aplica.

---

## Fundamentos

[< Índice Docs](#índice-docs)

---

### Introducción

[< Fundamentos](#fundamentos)

---

`FrugoCorp.AspNetCore.AutoDiscoveryIAM` es una librería para APIs de ASP.NET Core construidas con **minimal APIs** que agrega **autenticación JWT completa + RBAC dinámico**: los roles y permisos se descubren solos, en tiempo de ejecución, leyendo los endpoints reales de tu app vía `EndpointDataSource` — nada que declarar a mano ni mantener sincronizado cuando agregas un endpoint nuevo. La propia librería está construida con **Vertical Slice Architecture**, sin capa MVC/Controllers y **sin MediatR**.

> El descubrimiento automático de permisos depende de metadatos que ASP.NET Core solo expone para minimal APIs (el método mapeado, vía `EndpointDataSource`). Si tu app usa Controllers/MVC para sus propios endpoints, la librería y su login/JWT/RBAC funcionan igual, pero el auto-descubrimiento de permisos no cubre esos endpoints — necesitarías crear sus `Permission` manualmente.

#### Este repositorio

Este repositorio tiene 5 partes con propósitos distintos:

- **`src/AutoDiscoveryIAM/`** — la librería en sí: el motor RBAC + JWT que se empaqueta y publica en NuGet como `FrugoCorp.AspNetCore.AutoDiscoveryIAM`. Es lo único que tu proyecto necesita instalar, y el resto de la documentación trata casi exclusivamente sobre ella.
- **`samples/AutoDiscoveryIAM.SampleHost/`** — una app ASP.NET Core real y ejecutable que consume la librería, a modo de host de referencia: sirve para correr, explorar y probar la API completa (Swagger UI, Scalar) sin tener que integrarla primero en tu propio proyecto.
- **`tests/`** — las suites de pruebas (`AutoDiscoveryIAM.UnitTests`, `AutoDiscoveryIAM.IntegrationTests`) que validan la librería y el `SampleHost`.
- **`runsettings/`** — perfiles de ejecución de pruebas (`normal`/`coverage`/`performance`/`security`/`ci-full`). Son varios y no uno solo porque cada combinación de filtro/cobertura/paralelismo tiene un costo de tiempo muy distinto.
- **`docker/`** — Docker Compose para levantar el contenedor del `SampleHost` sin instalar el SDK de .NET en tu máquina — el mismo trato que ya le das a cualquier stack en Docker: un solo comando y la API está corriendo. La base de datos **no** viene incluida a propósito: necesitas tu propio SQL Server alcanzable, apuntado vía cadena de conexión.

Para empezar a usar la librería en tu propio proyecto, ve directo a [Instalación](#instalación-1). Si quieres ver primero qué trae hoy la librería en detalle (autenticación, RBAC dinámico, 2FA, etc.), revisa [Qué incluye hoy](#qué-incluye-hoy).

### Qué incluye hoy

[< Fundamentos](#fundamentos)

---

- **Autenticación JWT** con access + refresh token, rotación y detección de reuso, blacklist de tokens por `jti`, e invalidación global inmediata al cambiar contraseña (`TokenValidFrom`).
- **Autenticación de dos factores (2FA)** por correo o app autenticadora (TOTP), y **login social con Google** (vinculación de cuenta externa).
- **RBAC dinámico**: los permisos (`Users`, `Roles`, `Permissions`) no se declaran a mano — se descubren automáticamente leyendo `EndpointDataSource` (la tabla de ruteo nativa de ASP.NET Core, no reflexión ni OpenAPI) y se sincronizan a base de datos. Autorización "secure by default": todo endpoint requiere un permiso salvo que se marque explícitamente lo contrario, con una regla de no escalada (nadie otorga ni administra permisos que no tiene).
- **Ciclo de vida de cuenta completo**: registro, activación por correo, aprobación/rechazo de cuentas, recuperación y cambio de contraseña.
- **Rate limiting** (limitador nativo de ASP.NET Core) y **logging de eventos de seguridad** centralizado.
- **Soft delete** consistente en todas las entidades principales, con endpoints de restauración.
- **Documentación OpenAPI nativa** (`Microsoft.AspNetCore.OpenApi`, no Swashbuckle) servida en `/openapi/{documentName}.json` — el renderizador (Swagger UI en la raíz, Scalar en `/scalar`) es intercambiable sin tocar el núcleo, probado en vivo con ambos a la vez.
- Suite de pruebas real: **xUnit + NSubstitute + AwesomeAssertions** (unitarias + integración).

Para el detalle de cómo funciona el RBAC dinámico y los niveles de acceso especiales, ver [Autorización con niveles de acceso](#autorización-con-niveles-de-acceso). Para instalarla en tu propio proyecto, ve a [Instalación](#instalación-1).

## Seguridad y RBAC

[< Índice Docs](#índice-docs)

---

### Autorización con niveles de acceso

[< Seguridad y RBAC](#seguridad-y-rbac)

---

Todo endpoint queda protegido por RBAC estándar salvo que se marque explícitamente con uno de estos atributos (vía `.WithMetadata(...)` en su `*EndPoint.cs`):

| Atributo                              | Quién pasa                                                                                                                                          | Uso típico                                                                                                                                                                    |
| ------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `[PermissionsNotRequired(reason)]`    | Cualquier usuario autenticado (o incluso sin autenticar, en endpoints públicos como login/registro)                                                 | Endpoints self-service: perfil propio, cambio de contraseña propia, 2FA propio, registro público — la autorización es "es tuyo", no basada en rol.                            |
| `[RequiresDevelopmentAccess(reason)]` | Solo `User.IsDevelopment == true`                                                                                                                   | Herramientas de diagnóstico internas. Ni `IsAdmin` ni `IsSuAdmin` alcanzan.                                                                                                   |
| `[RequiresSuAdminAccess(reason)]`     | `User.IsDevelopment == true` **o** `IsSuAdmin == true`                                                                                              | Las pocas operaciones que nunca deben depender de un rol: cambiar el `IsAdmin` de otro usuario, limpiar las relaciones ocultas de un rol, ejecutar un cambio de correo y enviar el correo de prueba de SMTP. `IsAdmin` no alcanza. |
| `[RequirePermissions(note)]`          | Igual que sin atributo                                                                                                                              | Solo documentación/intención explícita — en runtime se comporta exactamente igual que no tener ningún atributo. Puede asignarse sin problema al rol default de auto-registro. |
| _(sin atributo)_                      | Usuario con un rol que tenga el `Permission` de este endpoint asignado, **o** cualquier usuario con `IsAdmin`/`IsDevelopment`/`IsSuAdmin` en `true` | RBAC estándar. Los 3 flags especiales saltan esta validación por completo, sin importar roles/permisos.                                                                       |

**`IsAdmin`, `IsDevelopment` e `IsSuAdmin` son columnas booleanas en `User`**, evaluadas directamente por `AuthorizationMiddleware` -- con una consulta a la BD en cada request, no desde el JWT -- y no derivan de ningún rol ni permiso. Un usuario con el rol `AdminAutoDiscoveryIAM` (con todos los permisos del package) igual no puede llamar un endpoint `[RequiresSuAdminAccess]` si no tiene ese flag en `true`; y si se le revoca el flag a alguien, el efecto es inmediato en la siguiente petición, sin depender de que expire un token viejo. `IsDevelopment` e `IsSuAdmin` no tienen ningún endpoint: se marcan por acceso directo a la base de datos. `IsAdmin` además se puede cambiar con `PUT /users/{publicId}/admin-type`, que a su vez exige `[RequiresSuAdminAccess]`. Al ser el único punto del sistema sin RBAC detrás, `tests/.../SensitiveFlagMassAssignmentTests.cs` verifica que ningún endpoint público los termine leyendo del body por error. Tampoco las administra cualquiera con el permiso: editar una cuenta con flags (`PUT /users/{publicId}`, por ejemplo para cambiarle el email y luego pedir el reseteo de contraseña), aprobarla, rechazarla, reenviarle la activación o cambiarle el tipo Admin exige un nivel igual o superior (`IsDevelopment` solo otro `IsDevelopment`; `IsSuAdmin`/`IsAdmin`, un `IsSuAdmin` o `IsDevelopment`); si no, responde 403. Es la misma razón por la que tampoco existe un endpoint para marcar qué rol es el default de auto-registro (`Role.IsDefaultForNewUser`, como mucho una fila a la vez) — automatizar una asignación tan sensible, con el riesgo de marcar la fila equivocada, no vale el ahorro de una consulta manual.

#### Roles sembrados por defecto

El package siembra dos roles al arrancar, siempre:

- **`AdminAutoDiscoveryIAM`** — administra la app desde el primer día, sin ningún flag especial: consulta usuarios, roles y permisos; crea, edita y restaura roles; les asigna y quita permisos; asigna roles a usuarios, y aprueba o rechaza registros. Puede darse a sí mismo o a otros roles cualquier permiso, incluidos los de tus propios endpoints después de sincronizar. No puede cambiar `IsAdmin`/`IsDevelopment`/`IsSuAdmin` ni correr el sync: eso sigue detrás de los flags.
- **`UserDefault`** — nace sin ningún permiso. Pensado para asignarse automáticamente a usuarios auto-registrados; el equipo dev le agrega los permisos que su app necesite. Puede eliminarse o renombrarse — si se reemplaza, basta con marcar `IsDefaultForNewUser=true` en el rol sustituto directamente en la base de datos.

La API rechaza asignar, vía `RolePermission`, un permiso `[RequiresDevelopmentAccess]`/`[RequiresSuAdminAccess]` a un rol que ya tiene `IsDefaultForNewUser=true`. **Esto no cubre el camino inverso**: como `IsDefaultForNewUser` solo se marca por acceso directo a la base de datos (sin endpoint, ver arriba), nada en la aplicación puede impedir que alguien marque como default un rol que _ya_ tenía esos permisos asignados desde antes. Es el mismo nivel de confianza que ya se le da a quien administra la base de datos para el resto de asignaciones sensibles — no es un hueco explotable por un usuario, solo una operación manual que hay que hacer con cuidado.

#### Primer usuario con privilegios

No hay ningún admin real sembrado. Las cuentas de ejemplo con contraseña pública solo se crean si las pides con `SeedAutoDiscoveryIAMDefaultDataAsync(includeDevelopmentUsers: true)` y el ambiente es `Development`/`Testing`; en cualquier otro ambiente se ignora el opt-in, y si esas cuentas existen activas se registra un log crítico al arrancar — ver [Credenciales de desarrollo por defecto](https://autodiscoveryiam.frugocorp.com/references/development/dev-credentials)). El registro público (`POST /users`, o alta automática por login social) siempre crea usuarios sin rol y sin ningún flag especial. Para tener el primer administrador: que esa persona se registre con su correo real, y que quien administre la base de datos le asigne el rol `AdminAutoDiscoveryIAM` directamente en la base (ejemplo para SQL Server):

```sql
INSERT INTO UserRoles (UserId, RoleId, AssignedAt, PublicId, CreatedAt, IsDeleted)
SELECT u.Id, r.Id, SYSUTCDATETIME(), NEWID(), SYSUTCDATETIME(), 0
FROM Users u CROSS JOIN Roles r
WHERE u.Email = 'tu-correo-real@ejemplo.com' AND r.Name = 'AdminAutoDiscoveryIAM';
```

`PublicId`, `AssignedAt`, `CreatedAt` e `IsDeleted` son obligatorios: un `INSERT` solo con `UserId`/`RoleId` falla.

A partir de ahí, esa cuenta administra a todos los demás usuarios desde la API.

**`IsSuAdmin` e `IsDevelopment` solo se marcan a mano, directamente en la base de datos**, y solo los marca quien la administra. Es deliberado: ninguna llamada a la API, cuenta comprometida o rol mal configurado puede otorgarlos. Dale `IsDevelopment` solo a quien vaya a correr `POST /permissions/sync`:

```sql
UPDATE Users SET IsDevelopment = 1 WHERE Email = 'quien-sincroniza@ejemplo.com';
UPDATE Users SET IsSuAdmin = 1 WHERE Email = 'tu-correo-real@ejemplo.com';
```

---

#### Regla de no escalada

Nadie otorga ni administra permisos que no tiene. Al guardar, la librería revisa que quien hace la operación tenga:

- **Al asignar o restaurar un rol a un usuario** (o reactivar un rol): todos los permisos de ese rol.
- **Al asignar o restaurar un permiso a un rol**: ese permiso.
- **Al cambiar el email de otro usuario**: todos los permisos de ese usuario, y que ese usuario no tenga flags. Sin esto, quien solo puede editar usuarios le cambiaría el email a un administrador y tomaría su cuenta con el restablecimiento de contraseña.

Si falta algo, la API responde 403 y no guarda nada. La regla aplica en cualquier camino (asignación individual, restauración u operación masiva), así que no depende de cómo se arme cada rol.

No aplica a cuentas con `IsAdmin`, `IsSuAdmin` o `IsDevelopment`, ni a quien tiene **todos** los permisos administrativos de la librería (el rol `AdminAutoDiscoveryIAM` completo): ese ya administra todo, y es quien otorga los permisos de tus propios endpoints después de cada sincronización, que al principio no tiene nadie. Lo que la regla restringe son los roles administrativos parciales. Por ejemplo, un rol de soporte que solo puede asignar roles no puede asignarse a sí mismo `AdminAutoDiscoveryIAM`.

#### Permisos para tus propios endpoints

El secure-by-default también aplica a **tus** endpoints, así que cada uno necesita un `Permission`. La librería lo deriva del namespace de la clase que declara el handler del endpoint y lo crea cuando sincronizas.

##### Convención de namespaces

`<CualquierRaíz>.Features.<Feature>.[Commands|Queries.]<Acción>` produce la clave `feature.accion_snake_case`. La raíz es libre, y la acción es siempre el **último** segmento, así que un slice anidado es un permiso propio:

| Namespace de la clase que declara el handler | Clave del permiso |
|---|---|
| `MiApi.Features.Pedidos.Commands.CrearPedido` | `pedidos.crear_pedido` |
| `Empresa.Tienda.Features.Pedidos.Queries.GetPedidos` | `pedidos.get_pedidos` |
| `MiApi.Features.Pedidos.Commands.CrearPedido.Borrador` | `pedidos.borrador` |
| `MiApi.Pedidos.Endpoints` (sin segmento `Features`) | ninguna: el sync la reporta, ver abajo |

Varias rutas mapeadas desde el mismo namespace comparten un solo permiso (un slice, grano grueso). Con una lambda en línea, cuenta la clase que contiene el `app.MapPost(...)`; con un method group (`app.MapPost("/pedidos", PedidoHandlers.Crear)`), cuenta la clase de ese método.

##### Sincroniza después de cada despliegue

Cada vez que agregues o renombres endpoints, crea sus permisos con `POST /permissions/sync`. Solo puede llamarlo un usuario con `IsDevelopment == true`:

```bash
curl -X POST https://tu-api/permissions/sync \
  -H "Authorization: Bearer <token de un usuario IsDevelopment>" \
  -H "Content-Type: application/json" \
  -d '{ "forceFullSync": false, "dryRun": false }'
```

- Mientras no se sincronice, un endpoint nuevo responde 403 a todos, salvo a los usuarios con `IsAdmin`/`IsDevelopment`/`IsSuAdmin`.
- Los permisos nuevos no pertenecen a ningún rol. Asígnalos con `PUT /roles/{rolePublicId}/permissions/{permissionPublicId}`, y el rol a los usuarios con `PUT /users/{userPublicId}/roles/{rolePublicId}`. `GET /permissions` lista los permisos con su `publicId`.
- Renombrar un slice cambia su clave: el permiso anterior se marca como obsoleto (nunca se borra) y se crea uno nuevo, que hay que volver a asignar.
- Con `"dryRun": true` ves qué cambiaría sin escribir nada.

##### Qué reporta y qué rechaza el sync

- **`endpointsWithoutPermission`** en la respuesta lista los endpoints que exigen un permiso pero cuyo namespace no sigue la convención. Muévelos bajo `Features`, o márcalos con `[PermissionsNotRequired]`, `[RequiresDevelopmentAccess]` o `[RequiresSuAdminAccess]` si no deben depender de un rol.
- **Una clave duplicada hace fallar el sync con un 400** que nombra los dos namespaces. Pasa cuando dos slices distintos producen la misma clave, por ejemplo tu `MiApi.Features.Users.Commands.CreateUser` contra el `users.create_user` de la propia librería. Renombra tu feature o tu acción: si no, quien tuviera uno de los dos permisos entraría también al otro sin que nadie lo decida.

### Reutilizar auditoría y soft-delete

[< Seguridad y RBAC](#seguridad-y-rbac)

---

Las 8 entidades del package heredan `AuditableEntity` (`PublicId`, `CreatedAt`/`By`, `UpdatedAt`/`By`, `DeletedAt`/`By`, `RestoredAt`/`By`, `IsDeleted`), rellenados automáticamente en cada `SaveChanges`/`SaveChangesAsync`. Esa clase base y el mecanismo que la rellena (`AuditableEntitySaveChangesInterceptor`, un `SaveChangesInterceptor` nativo de EF Core) son públicos — tus propias entidades pueden heredar la misma clase y obtener el mismo comportamiento, sin reimplementar nada.

Para tus propias entidades de negocio necesitas tu propio `DbContext`, apuntando a la misma base de datos. Como la librería ya reclamó su propio nombre para el suyo (`AutoDiscoveryIAMDbContext`, ver [Configura Program.cs](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#configura-programcs)), el nombre genérico `ApplicationDbContext` queda libre para el tuyo. Hay dos formas válidas de construirlo — la librería no te obliga a ninguna, ver [Elige tu propio DbContext: uno o dos](https://autodiscoveryiam.frugocorp.com/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos) para el detalle completo de ambas y su tradeoff. La plantilla de abajo usa la opción recomendada para empezar (un solo `DbContext`, heredando el de la librería):

```csharp
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentAuditUserService? currentAuditUserService = null)
  : AutoDiscoveryIAMDbContext(options, currentAuditUserService)
{
  public DbSet<UserProfile> UserProfiles { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder); // aplica los configs de la librería (User, Role, Permission, ...)
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    modelBuilder.ApplySoftDeleteQueryFilters(); // de FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Auditing
  }
}

public class UserProfile : AuditableEntity
{
  public long Id { get; set; }
  public long UserId { get; set; }
  public string Occupation { get; set; } = string.Empty;

  // FK real hacia el User de la librería, con navegación -- solo es posible en modo contexto
  // único, porque UserProfile y User viven en el mismo modelo. Ver "User, Role, Permission... son
  // cerrados a modificación" más abajo: esto extiende los datos de un usuario sin tocar User.
  public virtual User User { get; set; } = null!;
}

// Program.cs
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;

builder.Services.AddDbContext<ApplicationDbContext>(options => options
  .UseSqlServer(connectionString)); // el __EFMigrationsHistory default ya queda libre para ti, ver la nota en Configura Program.cs
builder.Services.AddScoped<AutoDiscoveryIAMDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
// AddAutoDiscoveryIAM(...) va DESPUÉS de estas dos líneas -- su health check depende de que
// AutoDiscoveryIAMDbContext ya esté registrado.
builder.Services.AddAutoDiscoveryIAM(builder.Configuration);
```

> Se propone (no obligatorio) el directorio `src/TuProyecto/Data/ApplicationDbContext.cs` para este archivo — el mismo patrón de carpeta `Data/` que usa la librería internamente.

- `ApplySoftDeleteQueryFilters()` aplica `WHERE !IsDeleted` a cualquier entidad de tu modelo que herede `AuditableEntity`, por convención — no hace falta escribir `HasQueryFilter` a mano por cada una. Como `ApplicationDbContext` ya trae el modelo de la librería (herencia), esta llamada también reaplica (sin cambiar nada) el mismo filtro a `User`/`Role`/etc. — inofensivo, es la misma condición que ya traían.
- No necesitas cablear el interceptor de auditoría a mano: `AutoDiscoveryIAMDbContext` ya lo hace en su propio `OnConfiguring`, usando el `ICurrentAuditUserService` que le llega por constructor (registrado en DI por `AddAutoDiscoveryIAM()`) — al heredar, `ApplicationDbContext` lo hereda también. `GetCurrentUser().Id` regresa `null` automáticamente si no hay usuario autenticado en esa petición (ej. un seed en background sin request HTTP).

Con `ApplicationDbContext` ya registrado, genera y aplica su migración igual que harías con cualquier `DbContext` propio (nota el `--context` explícito, para no confundirlo con el de la librería si tienes los dos registrados):

Genera la migración inicial de tu propio `DbContext`:

```bash
dotnet ef migrations add InitialCreate --context ApplicationDbContext
```

Aplícala a la base de datos:

```bash
dotnet ef database update --context ApplicationDbContext
```

Para ver cómo construir un endpoint completo (Command + Query) sobre esta entidad `UserProfile`, siguiendo la misma arquitectura Vertical Slice que usa la librería internamente, ver [Ejemplo: arquitectura Vertical Slice en tu propia app](#vertical-slice-en-tu-propia-app).

---

#### Elige tu propio DbContext: uno o dos

`User`, `Role`, `Permission` y el resto de las entidades de la librería son **cerradas a modificación, abiertas a extensión** (Open/Closed Principle): nunca las heredas ni les agregas propiedades — ni aunque uses el modo de un solo `DbContext` de abajo, que sí las trae al mismo modelo. Si necesitas más datos por usuario ("estado civil", una foto, lo que sea), la extensión válida es una entidad tuya, propia, con un FK hacia `User.Id` — como `UserProfile` en la plantilla de arriba, o el ejemplo completo en [`samples/SimpleCode/Features/UserProfiles/`](https://github.com/frugocorp/AutoDiscoveryIAM/tree/master/samples/SimpleCode/Features/UserProfiles).

Dicho eso, cómo relacionas esa entidad tuya con las de la librería sí es una decisión tuya — la librería ayuda con ambos caminos, no te obliga a ninguno:

**Opción 1 — Un solo `DbContext`** (`ApplicationDbContext : AutoDiscoveryIAMDbContext`, la plantilla de arriba). Recomendada para empezar, sobre todo si tu equipo tiene poca experiencia con EF Core: tu modelo y el de la librería quedan en la misma base de datos y el mismo `ModelBuilder`, así que consultas cruzadas (`.Include(p => p.User)`, joins, navegación) funcionan de forma nativa, con un único `SaveChangesAsync` para ambos lados (reutiliza `IAutoDiscoveryIAMUnitOfWork` de la librería, no necesitas tu propio Unit of Work). El costo: tu esquema queda ligado al ciclo de vida del package — una actualización de `AutoDiscoveryIAM` que cambie su esquema interno puede obligarte a generar una migración nueva en tu proyecto aunque tú no hayas tocado nada. Requiere dos cosas que la plantilla de arriba ya incluye: que el constructor de `AutoDiscoveryIAMDbContext` reciba `DbContextOptions` sin el genérico (ya es así), y registrar el alias `services.AddScoped<AutoDiscoveryIAMDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>())` **antes** de `AddAutoDiscoveryIAM()` — sin ese alias, el resto de la librería (repositorios, health check) no puede resolver `AutoDiscoveryIAMDbContext` desde DI.

**Opción 2 — Dos `DbContext` separados** (`ApplicationDbContext : DbContext`, independiente). Mejor si tu equipo ya tiene experiencia con EF Core y prefiere desacoplar del todo el ciclo de vida/migraciones de tu esquema del de la librería. El costo: sin joins nativos entre tus entidades y las de la librería, porque EF Core no permite un LINQ join entre dos instancias de `DbContext` distintas.

```csharp
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
  public DbSet<UserProfile> UserProfiles { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplySoftDeleteQueryFilters();
  }
}

public class UserProfile : AuditableEntity
{
  public long Id { get; set; }
  public long UserId { get; set; } // ya no hay navegación User -- vive en otro DbContext/modelo
  public string Occupation { get; set; } = string.Empty;
}

// Program.cs -- usa la variante de AddDbContext que te da el IServiceProvider, para poder
// resolver ICurrentAuditUserService (scoped) dentro de la configuración del contexto
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) => options
  .UseSqlServer(connectionString)
  .AddInterceptors(new AuditableEntitySaveChangesInterceptor(() =>
    serviceProvider.GetRequiredService<ICurrentAuditUserService>().GetCurrentUser().Id)));
```

Con dos contextos separados, dos operaciones antes triviales necesitan un patrón explícito:

**Escribir en ambos a la vez, de forma atómica (todo o nada).** No es el flujo normal — la mayoría de las operaciones solo leen de un lado (ej. verificar permisos) y escriben del otro (ej. crear un perfil), y eso no necesita coordinación porque la lectura no modifica nada. Si de verdad necesitas que una sola operación falle o se aplique completa en ambos lados, comparte la misma transacción física entre los dos contextos (ambos apuntan a la misma base de datos):

```csharp
await using var transaction = await autoDiscoveryIAMDbContext.Database.BeginTransactionAsync(cancellationToken);
applicationDbContext.Database.SetDbConnection(autoDiscoveryIAMDbContext.Database.GetDbConnection());
await applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);

// ... cambios en ambos contextos ...
await autoDiscoveryIAMDbContext.SaveChangesAsync(cancellationToken);
await applicationDbContext.SaveChangesAsync(cancellationToken);

await transaction.CommitAsync(cancellationToken);
```

**Leer con datos de ambos lados a la vez** (ej. listar tus `UserProfile` con el `Username`/`Email` del `User` dueño). EF Core no permite un LINQ join entre dos `DbContext` distintos, así que hay dos alternativas:

- **Join en memoria** (funciona con cualquier proveedor, incluso bases de datos distintas): consulta cada lado por separado y une los resultados en C#.

  ````code-block
  ```csharp
  var profiles = await applicationDbContext.UserProfiles.ToListAsync(cancellationToken);
  var userIds = profiles.Select(p => p.UserId).Distinct().ToList();
  var users = await autoDiscoveryIAMDbContext.Users
    .Where(u => userIds.Contains(u.Id))
    .ToDictionaryAsync(u => u.Id, cancellationToken);

  var result = profiles.Select(p => new UserProfileWithUserDto(
    p.PublicId, users[p.UserId].Username, users[p.UserId].Email, p.Occupation));
  ```
  ````

- **SQL directo** (más eficiente, un solo roundtrip -- solo válido si ambos `DbContext` apuntan a la misma base de datos física):

  ````code-block
  ```csharp
  var result = await applicationDbContext.Database
    .SqlQuery<UserProfileWithUserDto>($@"
      SELECT p.PublicId, u.Username, u.Email, p.Occupation
      FROM UserProfiles p
      INNER JOIN Users u ON u.Id = p.UserId")
    .ToListAsync(cancellationToken);
  ```
  ````

En ambos casos, considera envolver la consulta en un método de tu propio `Services/` (mismo patrón que `Features/Roles/Services/RoleQuery/` de la librería) en vez de repetirla en cada Handler que la necesite.

**Cómo lidia ASP.NET Core Identity con este mismo problema:** no lo resuelve — lo evita fusionando sus tablas dentro de tu propio `DbContext` desde el diseño (`ApplicationDbContext : IdentityDbContext<TUser>`), así que un join nativo siempre está disponible. Es exactamente la Opción 1 de arriba, con el mismo costo: el esquema de Identity queda acoplado al ciclo de vida de tu propio `DbContext`, y viceversa. AutoDiscoveryIAM te deja elegir en qué punto de ese tradeoff quieres estar, en vez de decidirlo por ti.

---

### Privacidad y datos de terceros

[< Seguridad y RBAC](#seguridad-y-rbac)

---

Este documento describe qué datos personales recibe, procesa y almacena `FrugoCorp.AspNetCore.AutoDiscoveryIAM` cuando un usuario inicia sesión con un proveedor de identidad externo (actualmente Google), y qué controles de seguridad se aplican sobre esos datos.

Es documentación técnica, no un aviso de privacidad legal. Antes de operar con usuarios reales, el equipo responsable de cada despliegue debe adaptar este contenido a su propio aviso de privacidad público, con asesoría legal en su jurisdicción — la sección [Responsabilidad de quien opera una instancia](https://autodiscoveryiam.frugocorp.com/docs/security-rbac/third-party-data#responsabilidad-de-quien-opera-una-instancia) detalla qué implica esto en la práctica.

#### Alcance del login social

El flujo de OAuth con Google solicita únicamente el scope `openid email`, con `access_type=online`. En términos concretos:

- **No se solicita ni se recibe ningún token de larga duración de Google.** No hay `access_token` ni `refresh_token` persistente — con `access_type=online`, Google ni siquiera los emite.
- El único artefacto que Google entrega es un `id_token` firmado, validado una sola vez (emisor, audiencia, expiración) y descartado de inmediato. Nunca se persiste.
- No se solicita acceso a Gmail, Drive, Contactos, nombre, foto de perfil ni ningún otro dato — solo la identidad básica (email) que Google ya verifica por su cuenta. El scope `profile` no se pide a propósito: la librería no persiste nombre ni ningún otro dato de perfil, así que pedirlo sería recolectar datos que nunca se usan.

#### Datos almacenados

De la respuesta de Google, la librería persiste únicamente lo siguiente:

| Dato | Dónde se guarda | Detalle |
|---|---|---|
| Email | `Users.Email` | El mismo campo que ya usa cualquier cuenta registrada de forma tradicional (es el username del sistema). |
| Proveedor | `ExternalLogins.Provider` | Valor fijo (`"google"`) — una categoría, no un dato sensible. |
| Identificador de sujeto (`sub`) | `ExternalLogins.ProviderId` | Hasheado con SHA-256, nunca en claro. |

No se almacena nombre, foto de perfil ni ningún otro dato de Google más allá de lo anterior.

##### Por qué el identificador de sujeto está hasheado

El `sub` que devuelve Google es estable pero no es una credencial: conocerlo por sí solo no permite iniciar sesión como esa persona, porque cada login real exige una autenticación vigente con Google que emite un `id_token` firmado, verificado por la librería en el momento.

Aun así, se guarda hasheado (SHA-256, determinista para permitir el match exacto en el login) para no persistir en claro un identificador que podría correlacionarse con otros sistemas que también hayan registrado el mismo `sub` de Google. El hash se calcula una vez por login/vinculación, sin impacto en el rendimiento del resto de la API.

##### Por qué no existe un campo de email de proveedor por separado

`ExternalLogins` no tiene un campo `ProviderEmail` independiente — el email de Google se guarda únicamente en `Users.Email`. Esto evita una copia redundante de un dato personal en la base de datos: menos superficie de exposición ante una filtración, sin ninguna pérdida funcional.

#### Evaluación de riesgo ante una filtración de base de datos

Si la base de datos se ve comprometida, lo que un atacante obtendría de un usuario vinculado a Google es:

- Su email — el mismo riesgo que ya existe para cualquier usuario registrado de forma tradicional (phishing dirigido, spam).
- El hecho de que esa cuenta usa Google para iniciar sesión — una fuga de metadatos menor, no de una credencial.
- **Ninguna posibilidad de acceder a la cuenta real de Google del usuario.** No hay tokens de Google que robar, y el identificador hasheado no sirve para autenticarse ante Google ni ante esta API sin pasar de nuevo por el flujo real de OAuth.

Vincular una cuenta de Google no introduce una categoría de riesgo adicional más allá de la que ya existe por operar una base de usuarios con email.

#### Responsabilidad de quien opera una instancia

Este documento describe el comportamiento técnico del código tal como está en este repositorio. Antes de desplegar esta librería (o un fork) en producción con usuarios reales, el equipo operador debe:

1. **Publicar su propio aviso de privacidad**, adaptado a su jurisdicción (por ejemplo GDPR en la Unión Europea, CCPA en California, o la LFPDPPP en México) y a los datos reales que su aplicación agregue sobre esta base.
2. **Revisar este documento de nuevo si se agregan más scopes** de Google (o de otro proveedor de identidad) o más campos de perfil — cada scope adicional cambia el análisis de riesgo descrito arriba.
3. **Obtener asesoría legal** antes de operar con usuarios reales, especialmente si se maneja información de menores, datos de salud, financieros u otras categorías sensibles.

La licencia [MIT](https://autodiscoveryiam.frugocorp.com/license) de este proyecto cubre el *código* (sin garantía, uso bajo el propio riesgo del operador) y no exime a quien opera una instancia real de sus obligaciones legales sobre los datos de sus propios usuarios.

## Tutoriales

[REGRESAR A APARTADOS](#apartados)

---

## Índice Tutoriales

- [Instalación](#instalación) -- Instala la librería, corre el host de ejemplo o empaqueta tu propia versión.
  - [Instalación](#instalación-1) -- Instala FrugoCorp.AspNetCore.AutoDiscoveryIAM paso a paso: crea el proyecto, agrega el paquete NuGet, conecta tu base de datos, configura appsettings.json y aplica las migraciones iniciales con dotnet ef.
  - [Actualizar AutoDiscoveryIAM](#actualizar-autodiscoveryiam) -- Cómo actualizar el paquete en una app que ya lo usa, sin perder datos, roles ni asignaciones.
- [Ejemplos](#ejemplos) -- Ejemplos para construir tus propias features sobre la librería.
  - [Vertical Slice en tu propia app](#vertical-slice-en-tu-propia-app) -- Cómo construir una feature con Vertical Slice Architecture en tu propia aplicación.
  - [Ejemplo copiable: UserProfiles](#ejemplo-copiable-userprofiles) -- Una feature UserProfiles completa, lista para copiar a tu proyecto y adaptarla.

---

## Instalación

[< Índice Tutoriales](#índice-tutoriales)

---

### Instalación

[< Instalación](#instalación)

---

#### Personaliza el nombre de tu proyecto

A lo largo de esta guía (y del resto del sitio) verás el nombre genérico `TuProyecto` en comandos, namespaces y rutas de carpeta. Es intencional: así el ejemplo se entiende sin importar cómo se llame tu proyecto real. Reemplázalo por el nombre de tu proyecto al copiar cada comando.

**Consejo:** en el sitio de documentación no hace falta reemplazarlo a mano. Escribe el nombre real de tu proyecto en el campo **PROYECTO** del panel "Mis Variables", en la barra derecha, y los comandos y ejemplos de código mostrarán tu nombre en vez de `TuProyecto`, listos para copiar y pegar. El valor se guarda únicamente en el almacenamiento local de tu navegador: nunca se envía a ningún servidor.

#### Crea tu proyecto

Crea la carpeta raíz del proyecto:

```bash
mkdir TuProyecto
```

Entra a esa carpeta:

```bash
cd TuProyecto
```

Crea el archivo de solución (`.sln`):

```bash
dotnet new sln
```

Crea la carpeta `src`, donde va a vivir el proyecto web:

```bash
mkdir src
```

Entra a `src`:

```bash
cd src
```

Crea el proyecto web mínimo dentro de `src/TuProyecto`:

```bash
dotnet new web -n TuProyecto
```

Regresa a la raíz del repositorio, donde está el `.sln`:

```bash
cd ..
```

Agrega el proyecto recién creado a la solución:

```bash
dotnet sln add src/TuProyecto/TuProyecto.csproj
```

#### Agrega la librería a tu proyecto

```bash
dotnet add package FrugoCorp.AspNetCore.AutoDiscoveryIAM
```

> ⚠️ Si clonaste este repositorio desde el código fuente (sin que el paquete esté publicado todavía en NuGet.org), este comando **no va a funcionar tal cual** — ve primero a [Empaquetar la librería desde tu fork y probarla en otro proyecto](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/package-library).

#### Necesitas una base de datos

La librería no trae proveedor de base de datos ni migraciones empaquetadas — eliges tu proveedor (SqlServer/Npgsql/Sqlite/etc.) e instalas su paquete **antes** de escribir el código que lo usa, para no toparte con un error de compilación por un método que todavía no existe en tu proyecto:

Instala el proveedor de base de datos (cambia `SqlServer` por `Npgsql`/`Sqlite`/etc. si usas otro):

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

> 💡 `Microsoft.EntityFrameworkCore.SqlServer` 10.0.x arrastra `System.Security.Cryptography.Pkcs` 9.0.4, que tiene avisos de seguridad conocidos (GHSA-846f-f764-76mx, GHSA-2fmv-rv2c-59x7): `dotnet build` muestra `NU1902`/`NU1903`, y con `TreatWarningsAsErrors` el build falla. Viene del proveedor, no de esta librería. Fija la versión corregida en tu proyecto: `dotnet add package System.Security.Cryptography.Pkcs --version 10.0.14`.

Requerido por `dotnet ef` en tu propio proyecto de inicio — no lo usa la librería en tiempo de ejecución:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Instalar el paquete no basta — necesitas además una base de datos real y alcanzable. Si todavía no tienes una instancia de SQL Server a la mano, este mismo repositorio trae un contenedor Docker desechable listo para desarrollo local (ver [Docker: db/](https://autodiscoveryiam.frugocorp.com/tutorials/docker/docker-db)):

Entra a la carpeta del contenedor de base de datos de este repo:

```bash
cd docker/db
```

Copia la plantilla de variables de entorno (edítala antes de continuar):

```bash
cp .env.template .env
```

Construye y levanta el contenedor en segundo plano:

```bash
docker-compose up --build -d
```

#### Configura appsettings.json

Configuración mínima obligatoria (sin esto la app no arranca):

```json src/TuProyecto/appsettings.json
{
  "ConnectionStrings": { "DefaultConnection": "..." },
  "JwtSettings": { "Key": "...", "Issuer": "...", "Audience": "..." }
}
```

Ver la tabla completa de opciones en [Configuración](#configuración) más abajo, incluida una [plantilla completa de `appsettings.json`](https://autodiscoveryiam.frugocorp.com/references/configuration-testing/configuration#plantilla-completa-de-appsettingsjson) lista para copiar y rellenar.

#### Crea tu DbContext

En `Program.cs` puedes terminar registrando **solo el `DbContext` de la librería**, o **también uno propio** para tus propias entidades de negocio, dependiendo de si tu app ya tiene (o va a tener pronto) alguna:

- **`AutoDiscoveryIAMDbContext`** — el de la librería (`Users`/`Roles`/`Permissions`). No lo escribes tú, solo lo registras — eso lo ves en el siguiente paso, [Configura Program.cs](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#configura-programcs).
- **Tu propio `ApplicationDbContext`** — para tus propias entidades (lo que sea que modele tu app). Si todavía no lo tienes, créalo ahora. Hay dos formas válidas de construirlo — ver [Elige tu propio DbContext: uno o dos](https://autodiscoveryiam.frugocorp.com/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos) para el detalle completo de ambas y su tradeoff; en resumen, la forma recomendada para empezar (un solo `DbContext`, heredando el de la librería):

  ````code-block
  ```csharp src/TuProyecto/Data/ApplicationDbContext.cs
  public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentAuditUserService? currentAuditUserService = null)
    : AutoDiscoveryIAMDbContext(options, currentAuditUserService)
  {
    // tus propios DbSet<T> aquí
  }
  ```
  ````

  La plantilla completa (con una entidad de ejemplo, `OnModelCreating`, etc.) está en [Reutilizar auditoría y soft-delete en tus propias entidades](#reutilizar-auditoría-y-soft-delete).

Si tu app todavía no tiene ninguna entidad propia, no pasa nada por saltarte esto: registra solo `AutoDiscoveryIAMDbContext` por ahora y agrega tu `ApplicationDbContext` más adelante, cuando lo necesites — el paso [Configura Program.cs](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#configura-programcs) siguiente muestra cómo se ve cada opción registrada.

#### Extras opcionales: Validator, OpenAPI, Logging y Diagnóstico

Ninguno de estos 4 es requerido por AutoDiscoveryIAM para funcionar — son piezas comunes que la mayoría de equipos termina queriendo para sus propios slices (ver el ejemplo completo en [Ejemplo: arquitectura Vertical Slice en tu propia app](#vertical-slice-en-tu-propia-app)). Se explican aquí, antes de [Configura Program.cs](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#configura-programcs), porque ese paso ya incluye las líneas de cada uno (marcadas como opcionales) — así sabes de antemano si te sirven o si te conviene borrarlas.

- **Validator (FluentValidation)** — valida un `Command` antes de que llegue al Handler. **No necesitas instalar nada**: `FluentValidation` ya viene como dependencia transitiva de `FrugoCorp.AspNetCore.AutoDiscoveryIAM` (la librería la usa para sus propios Commands), así que ya está disponible en tu proyecto en cuanto instalas el paquete. Solo agrega `using FluentValidation;` y hereda de `AbstractValidator<TCommand>`.

- **OpenAPI (documentación de la API)** — `AddAutoDiscoveryIAM()` ya registra el documento OpenAPI (nativo, `Microsoft.AspNetCore.OpenApi`, incluido en el SDK Web de .NET) junto con los transformers que hacen que la documentación que escribas por endpoint (`EndpointOperationDocs`, ver [Docs/&lt;Slice&gt;OperationDocs.cs](#vertical-slice-en-tu-propia-app)) realmente aparezca en el JSON generado — no necesitas llamar `AddOpenApi(...)` tú mismo ni cablear ningún transformer. Lo que sí necesitas instalar es un **renderizador** (la interfaz visual que lee ese documento) — elige uno o ambos:

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

  > ⚠️ Recuerda marcar tu propio endpoint `MapOpenApi(...)` con `.WithMetadata(new PermissionsNotRequiredAttribute("..."))` — ver el aviso 💡 en [Configura Program.cs](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#configura-programcs) más abajo. Sin esto, tu documentación responde 401 y ningún renderizador puede mostrarla.

- **Logging** — no requiere ningún paquete (usa `Microsoft.Extensions.Logging`, ya incluido). El patrón que usa la librería internamente (un `I{Entidad}Logger` público por feature, envolviendo `ILoggerBase<T>`, en vez de llamar `ILogger<T>` directo en cada Handler) es reutilizable en tu propia app — ver `Infrastructure/Logging/` en el [ejemplo completo](https://github.com/frugocorp/AutoDiscoveryIAM/tree/master/samples/SimpleCode/Features/UserProfiles).

#### Configura Program.cs

`AutoDiscoveryIAMDbContext` es el `DbContext` de la **librería** (`Users`/`Roles`/`Permissions`) — no lo escribes tú, solo lo registras. Cómo lo registras depende de si vas a tener tu propio `ApplicationDbContext` y, si sí, de cuál de las dos opciones de [Elige tu propio DbContext: uno o dos](https://autodiscoveryiam.frugocorp.com/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos) elegiste:

```csharp src/TuProyecto/Program.cs
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
// más abajo y "Ejemplo: arquitectura Vertical Slice en tu propia app") -- "TuProyecto" aquí es tu
// namespace real (ej. "ReMemSelf"), no lo dejes literal:
using TuProyecto.Data;
using TuProyecto.Features;

var builder = WebApplication.CreateBuilder(args);

// Lee "ConnectionStrings:DefaultConnection" de tu appsettings.json (ver Configura appsettings.json
// más abajo) -- "TuProyecto" aquí es solo un nombre de variable, no lo del csproj.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
```

Después de esa parte común, el registro del/los `DbContext` cambia según tu caso — usa **uno** de estos 3 bloques, no varios a la vez:

**Si todavía no tienes entidades propias** (registra solo el de la librería, agrega tu `ApplicationDbContext` cuando lo necesites):

```csharp src/TuProyecto/Program.cs
builder.Services.AddDbContext<AutoDiscoveryIAMDbContext>(options => options.UseSqlServer(connectionString));
```

**Opción 1 — un solo `DbContext`** (recomendada para empezar; tu `ApplicationDbContext` hereda `AutoDiscoveryIAMDbContext`, ver [Crea tu DbContext](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#crea-tu-dbcontext) arriba):

```csharp src/TuProyecto/Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<AutoDiscoveryIAMDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
```

**Opción 2 — dos `DbContext` separados** (ver el porqué del `MigrationsHistoryTable` en el aviso 💡 debajo):

```csharp src/TuProyecto/Program.cs
builder.Services.AddDbContext<AutoDiscoveryIAMDbContext>(options => options.UseSqlServer(
  connectionString,
  sql => sql
    .MigrationsAssembly("TuProyecto")
    .MigrationsHistoryTable("__EFMigrationsHistory_AutoDiscoveryIAM")));
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
```

Y el resto de `Program.cs`, igual sin importar cuál de los 3 bloques anteriores hayas usado:

```csharp src/TuProyecto/Program.cs
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
  c.SwaggerEndpoint($"/openapi/{openApiDocumentName}.json", "TuProyecto API");
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

> 💡 `using TuProyecto.Data;`/`using TuProyecto.Features;` son **tu propio** namespace (ajusta `TuProyecto` al de tu proyecto real, ej. `ReMemSelf`), no algo que venga con el paquete. Ojo con el autofix del IDE en `AddFeaturesDI()`/`MapAllFeatureEndpoints()`: la librería **también** tiene sus propios métodos con esos mismos nombres (en `FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features`, la misma convención que copia `samples/SimpleCode` a propósito) — normalmente nunca necesitas importar ese namespace tú mismo (`AddAutoDiscoveryIAM()`/`MapAutoDiscoveryIAMEndpoints()` ya los llaman por dentro), así que si el autofix te ofrece las dos opciones, la que quieres es la de tu propio proyecto, no la de la librería. Elegir la incorrecta compila igual (siempre que solo agregues uno de los dos usings, no ambos a la vez -- si importas los dos, ahí sí el compilador marca error de llamada ambigua, CS0121), pero nunca registra ni mapea tus propias features.

> 💡 El "secure by default" de la librería protege **todos** los endpoints del pipeline, no solo los de la librería — incluido cualquier endpoint que agregues tú mismo (tu propio `app.MapOpenApi()`, o hasta el `app.MapGet("/", () => "Hello World!")` de ejemplo que trae `dotnet new web`). Ninguno de esos tiene un `Permission` propio (no viven bajo un namespace `...Features.<Feature>.<Acción>`, ver [Permisos para tus propios endpoints](https://autodiscoveryiam.frugocorp.com/docs/security-rbac/authorization#permisos-para-tus-propios-endpoints)), así que sin excepción explícita responden 401/403 en vez de funcionar. Si quieres conservar un endpoint propio así, márcalo con `.WithMetadata(new PermissionsNotRequiredAttribute("..."))`; si no lo necesitas (como el `Hello World!` de la plantilla por default), simplemente bórralo. Validado en vivo instalando el paquete en un proyecto externo limpio.

> 💡 `AddAutoDiscoveryIAM()` acepta dos parámetros opcionales para el documento OpenAPI que registra por dentro: `openApiDocumentName` (default `"v1"`, cámbialo solo si ya usas otro nombre en tu proyecto) y `configureOpenApi` (un `Action<OpenApiOptions>` que corre *después* de los transformers de la librería, sobre el mismo documento — úsalo para agregar tus propios transformers o, si tu renderizador lo necesita, fijar `options.OpenApiVersion` — Swagger UI, por ejemplo, no renderiza `3.1.x` todavía). Ninguno de los dos es obligatorio.

> 💡 `AddAutoDiscoveryIAM()` ya registra `ForwardedHeadersOptions` (confía en `X-Forwarded-Proto`/`X-Forwarded-For` viniendo de la red `172.16.0.0/12` -- el rango que Docker asigna por defecto a sus redes bridge), pero **no** llama `app.UseForwardedHeaders()` por ti: es una decisión de topología de red específica de cada despliegue, así que queda explícita en tu propio `Program.cs`. Si tu proxy no vive en una red Docker dentro de ese rango (por ejemplo, corre fuera de Docker, o en una red con una subred distinta), esos headers llegan pero la librería no confía en el origen y los ignora en silencio -- `Request.Scheme` se sigue quedando en `http`. Verifica la IP/subred real de tu proxy con `docker network inspect <red>` si `UseForwardedHeaders()` no resuelve el problema. Para confiar en otra red o en IPs puntuales, declara `ForwardedHeaders:KnownNetworks` (CIDR) o `ForwardedHeaders:KnownProxies` en tu configuración: reemplazan a `172.16.0.0/12`.

> 💡 `.MigrationsHistoryTable("__EFMigrationsHistory_AutoDiscoveryIAM")` **solo aplica a la Opción 2** (dos `DbContext` separados) — con dos contextos hay dos historiales de migraciones independientes, y este nombre evita que se pisen entre sí en la misma base de datos, dejando además el `__EFMigrationsHistory` default libre para el tuyo. Con la Opción 1 (un solo `DbContext`) esto no aplica: solo hay un modelo y un historial de migraciones para todo, así que no necesitas (ni debes) configurar `MigrationsHistoryTable` en absoluto. Ver [Elige tu propio DbContext: uno o dos](https://autodiscoveryiam.frugocorp.com/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos) para el resto del tradeoff entre ambas.

> 💡 Llama `app.MapAutoDiscoveryIAMEndpoints()` sobre la aplicación, nunca dentro de `MapGroup("/api")`: los límites de peticiones por defecto se comparan contra la ruta real (`/auth/login`, `/users/password/reset/request`...) y con un prefijo de grupo dejarían de aplicarse, así que la app no arranca si lo intentas. Para servir la API bajo un prefijo, usa `app.UsePathBase("/api")` antes del resto del pipeline.

#### Claves de Data Protection (obligatorio en producción)

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

#### Aplica las migraciones

Los comandos `dotnet ef` requieren la herramienta global `dotnet-ef`, instalada una sola vez por máquina (no viene con el SDK de .NET).

```bash
dotnet tool install --global dotnet-ef
```

> **Nota:** Si no la tienes instalada, `dotnet ef` responde "no se pudo ejecutar porque no se especificó el comando o archivo" -- ese es el error que resuelve este comando. Ver la [documentación oficial de herramientas de EF Core](https://learn.microsoft.com/ef/core/cli/dotnet#installing-the-tools) para más detalle.

Si ya tenías `dotnet-ef` instalado pero de una versión más vieja que tu SDK, actualízalo para no arrastrar una advertencia de versión desincronizada en cada comando.

```bash
dotnet tool update --global dotnet-ef
```

> **Nota:** Si te saltas este paso vas a ver una advertencia como "The Entity Framework tools version 'X' is older than that of the runtime 'Y'" -- las migraciones se aplican igual (validado en vivo), pero conviene actualizar para no arrastrarla.

Si todavía no tienes tu propio `ApplicationDbContext`, o si lo tienes pero elegiste la **Opción 2** (dos `DbContext` separados) de [Configura Program.cs](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#configura-programcs):

Genera la migración inicial de las tablas de la librería:

```bash
dotnet ef migrations add InitialCreate --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
```

Aplica esa migración a la base de datos:

```bash
dotnet ef database update
```

Si elegiste la **Opción 1** (un solo `DbContext`), apunta el `--context` al tuyo — genera y aplica en un solo paso las tablas de la librería y las tuyas juntas, porque comparten el mismo modelo:

Genera la migración inicial (tablas de la librería + las tuyas juntas, porque comparten el mismo modelo):

```bash
dotnet ef migrations add InitialCreate --context ApplicationDbContext
```

Aplica esa migración a la base de datos:

```bash
dotnet ef database update --context ApplicationDbContext
```

Para ver todo esto ya funcionando sin integrarlo primero a tu propio proyecto, corre el [`SampleHost`](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/run-samplehost) de este mismo repo.

> 💡 **Patrón opcional: esperar la base de datos con reintentos al arrancar.** Si tu API y tu SQL Server corren en contenedores de Docker independientes, sin ningún `depends_on`/`healthcheck` que los ordene (el mismo criterio que usan `docker/api/` y `docker/db/` de este repo — cada stack se puede levantar por separado, ver [Docker](https://autodiscoveryiam.frugocorp.com/docs/architecture/docker-architecture)), nada garantiza que la base de datos ya acepte conexiones cuando arranca tu proceso. `SampleHost`/`Infrastructure/DatabaseInitialization.cs` resuelve esto con un reintento (10 intentos, 5s de espera cada uno) antes de continuar, y de paso aplica migraciones pendientes solo en Development (`dotnet ef database update` sigue siendo necesario en Producción). Es un patrón real y funcional, no solo un ejemplo — pero también es completamente opcional: no es parte de `AddAutoDiscoveryIAM()`/`UseAutoDiscoveryIAM()`, y un equipo cuyo propio orquestador ya garantice el orden de arranque (o que simplemente prefiera aplicar migraciones a mano antes de levantar la app) puede omitirlo sin perder nada. Cópialo de `samples/AutoDiscoveryIAM.SampleHost/Infrastructure/DatabaseInitialization.cs` si te sirve tal cual.

---

### Actualizar AutoDiscoveryIAM

[< Instalación](#instalación)

---

La librería no trae migraciones: su modelo vive en el paquete y las migraciones son de tu app, así que actualizarla es el mismo flujo de EF Core que ya usas para tus propias entidades.

1. Lee las [notas de la versión](https://github.com/frugocorp/AutoDiscoveryIAM/releases) (las mismas del CHANGELOG del repositorio). Mientras la versión sea `0.y.z`, cualquier versión menor puede traer cambios incompatibles.
2. Actualiza el paquete: `dotnet add package FrugoCorp.AspNetCore.AutoDiscoveryIAM --version <nueva>`.
3. Genera una migración y revísala. Debe contener solo los cambios de esquema que anuncia el CHANGELOG, o ninguno:

   ````code-block
   ```bash
   dotnet ef migrations add UpdateAutoDiscoveryIAM --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
   dotnet ef database update
   ```
   ````

4. Despliega. Al arrancar, `SeedAutoDiscoveryIAMDefaultDataAsync()` aplica una sola vez por base de datos los cambios de **datos** del paquete (los registra en la tabla `AutoDiscoveryIAMUpgrades`) y después agrega al rol `AdminAutoDiscoveryIAM` los permisos por defecto nuevos. Los roles, asignaciones y usuarios existentes quedan como estaban.
5. Corre `POST /permissions/sync` para que los endpoints nuevos de la librería obtengan su permiso.

El paquete mantiene estables las claves de permiso entre versiones. Si algún día hay que renombrar un slice de la librería, la propia actualización mueve sus asignaciones de rol a la clave nueva, para que nadie pierda acceso.

Para probar una versión nueva en tu proyecto real antes de publicarla, empaquétala a un feed local con una versión preliminar única (`0.2.0-local.1`, `0.2.0-local.2`...) e instálala desde ahí, como se explica en [Empaquetar la librería desde tu fork y probarla en otro proyecto](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/package-library).

## Ejemplos

[< Índice Tutoriales](#índice-tutoriales)

---

### Vertical Slice en tu propia app

[< Ejemplos](#ejemplos)

---

La librería está construida internamente con **Vertical Slice Architecture** (ver [Qué incluye hoy](#qué-incluye-hoy)) — un Command/Query por carpeta, con su propio `Handler`, `Repository`, `DI` y `EndPoint`, sin capa MVC/Controllers y sin MediatR. Nada te obliga a seguir el mismo patrón en tu propia app (un `DbContext` + minimal APIs normales funcionan igual de bien), pero si tu equipo no tiene ya una arquitectura definida, te damos un ejemplo completo y funcional para que la adoptes si te sirve — queda naturalmente compatible con el resto de la librería, y es la misma arquitectura que ya viste en [Reutilizar auditoría y soft-delete en tus propias entidades](#reutilizar-auditoría-y-soft-delete).

El ejemplo usa la entidad `UserProfile` y el `ApplicationDbContext` de esa misma sección (contexto único, hereda `AutoDiscoveryIAMDbContext`): un Command (`CreateUserProfile`) y una Query (`GetUserProfiles`), con la misma estructura de archivos por carpeta que usa la librería (ver `src/AutoDiscoveryIAM/Features/Roles/Commands/CreateRole/` en este repo como referencia 1:1).

Lo que sigue explica **por qué** existe cada archivo por separado, simplificado a un solo campo (`Occupation`) para no distraer del punto de cada archivo — en la práctica tu API recibiría el `PublicId` del usuario, no su `Id` interno, y resolvería uno a partir del otro (ver el ejemplo completo para esa resolución). Si prefieres los archivos completos y listos para copiar (incluidas las 3 piezas opcionales — Validator, OpenAPI, Logging — que aquí solo se explican, ver [Extras opcionales: Validator, OpenAPI y Logging](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#extras-opcionales-validator-openapi-logging-y-diagnostico) — y la resolución `PublicId` → `Id` mencionada arriba), están en [`samples/SimpleCode/Features/UserProfiles/`](https://github.com/frugocorp/AutoDiscoveryIAM/tree/master/samples/SimpleCode/Features/UserProfiles) — no es un proyecto que compile como parte de este repo, es solo material de referencia para copiar y pegar en el tuyo.

#### Command

`CreateUserProfile`, dividido en 8 archivos — cada uno con una sola responsabilidad, para poder cambiar una pieza (ej. la forma del JSON de entrada) sin tocar las demás (ej. la lógica de negocio). Un solo archivo con todo el código junto capturaría el request y regresaría el response igual de "funcional", pero mezclaría HTTP, validación, reglas de negocio y acceso a datos en un mismo lugar — difícil de probar en aislado y de mantener según crece.

##### Contrato del Command

Por qué es su propio archivo: el registro de entrada — solo datos, sin lógica ni nada de HTTP. El Handler recibe esto, no el DTO de HTTP directamente, para que la lógica de negocio no dependa de la forma exacta del JSON que llega por la red.

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileCommand.cs`

```csharp
namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public record CreateUserProfileCommand(long UserId, string Occupation);
```

##### Repository

Por qué es su propio archivo: es el único lugar que toca el `DbContext` directamente para este slice. Aísla el acceso a datos del Handler — así el Handler se puede probar con un mock de `ICreateUserProfileRepository`, sin necesitar una base de datos real.

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileRepository.cs` (interfaz + implementación en el mismo archivo — igual que hace la librería)

```csharp
namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public interface ICreateUserProfileRepository
{
  Task<UserProfile> CreateUserProfile(UserProfile userProfile);
}

public class CreateUserProfileRepository(ApplicationDbContext context) : ICreateUserProfileRepository
{
  private readonly ApplicationDbContext _context = context;

  public Task<UserProfile> CreateUserProfile(UserProfile userProfile)
  {
    _context.UserProfiles.Add(userProfile);
    return Task.FromResult(userProfile);
  }
}
```

##### Handler

Por qué es su propio archivo: es la lógica de negocio del slice — valida reglas, arma la entidad, guarda, y regresa un `OperationResult`. Es lo único que una prueba unitaria necesita ejercitar directamente, sin pasar por HTTP ni por una base de datos real (con un mock del Repository).

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileHandler.cs`

```csharp
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;

namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public class CreateUserProfileHandler(ICreateUserProfileRepository repository, IAutoDiscoveryIAMUnitOfWork unitOfWork)
{
  private readonly ICreateUserProfileRepository _repository = repository;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

  public async Task<OperationResult<CreateUserProfileResponseDto>> Handle(CreateUserProfileCommand command, CancellationToken cancellationToken)
  {
    var userProfile = new UserProfile { UserId = command.UserId, Occupation = command.Occupation };

    await _repository.CreateUserProfile(userProfile);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    var responseDto = new CreateUserProfileResponseDto(userProfile.PublicId, userProfile.UserId, userProfile.Occupation);
    return OperationResult.Ok(responseDto);
  }
}
```

> `IAutoDiscoveryIAMUnitOfWork` es de la librería (`FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW`), no algo que tengas que construir tú — en modo contexto único (la opción usada en este ejemplo) `ApplicationDbContext` **es**, por herencia, un `AutoDiscoveryIAMDbContext`, así que guardar aquí persiste también `UserProfile`. Si en cambio usaras dos `DbContext` separados (ver [Elige tu propio DbContext: uno o dos](https://autodiscoveryiam.frugocorp.com/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos)), aquí no aplicaría — llamarías `_context.SaveChangesAsync()` directo, o tu propio Unit of Work si construiste uno.

##### DI

Por qué es su propio archivo: registra el `Repository` y el `Handler` de este slice en el contenedor de dependencias. Agregar un slice nuevo no significa tocar un archivo central compartido por todos los demás — cada slice trae y mantiene su propio registro.

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileDI.cs`

```csharp
namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public static class CreateUserProfileDI
{
  public static IServiceCollection AddCreateUserProfileDI(this IServiceCollection services)
  {
    services.AddScoped<ICreateUserProfileRepository, CreateUserProfileRepository>();
    services.AddScoped<CreateUserProfileHandler>();
    return services;
  }
}
```

##### RequestDto

Por qué es su propio archivo: la forma exacta del JSON que el cliente envía por HTTP — separada del Command para que un cambio en el contrato HTTP (ej. renombrar un campo del request) no obligue a tocar la lógica interna del Handler.

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileRequestDto.cs`

```csharp
namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public record CreateUserProfileRequestDto(long UserId, string Occupation);
```

##### ResponseDto

Por qué es su propio archivo: los datos que sí se exponen al cliente en la respuesta — nunca la entidad interna directamente, para controlar explícitamente qué campos salen (ej. nunca el `Id` interno de base de datos, solo el `PublicId`).

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileResponseDto.cs`

```csharp
namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public record CreateUserProfileResponseDto(Guid PublicId, long UserId, string Occupation);
```

##### Response

Por qué es su propio archivo: el envoltorio de respuesta tipado específicamente para este slice — nunca se expone `ApiResponse<T>` genérico directo en un endpoint, cada slice tiene el suyo propio.

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileResponse.cs`

```csharp
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public record CreateUserProfileResponse : ApiResponse<CreateUserProfileResponseDto>
{
  public CreateUserProfileResponse(bool success, CreateUserProfileResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
    : base(success, data, error, errorInfo) { }
}
```

> **Por qué esta estructura vale la pena** (no es burocracia porque sí): toda respuesta de la API — éxito o error — tiene siempre la misma forma (`Success`, `Data`, `Error`, `ErrorInfo`). Un frontend puede revisar `response.success` sin adivinar qué campos trae cada endpoint distinto, y ante un error, leer `errorInfo.key`/`errorInfo.statusCode` de forma programática (para tomar una decisión en código) en vez de parsear un mensaje de texto libre que puede cambiar de redacción. `ErrorInfo` además centraliza el catálogo de errores conocidos (`Status400Validation`, `Status404NotFound`, etc.) en un solo lugar, reutilizable en todos tus slices, en vez de que cada endpoint invente su propio formato de error. `OperationResult<T>`/`OperationResult` (fábrica estática `Ok`/`Fail`), `ApiResponse<T>` y `ErrorInfo` son públicos en `FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common`/`Common.ApiResponse` — puedes ver la estructura real completa en [`OperationResult.cs`](https://github.com/frugocorp/AutoDiscoveryIAM/blob/master/src/AutoDiscoveryIAM/Common/OperationResult.cs), [`ApiResponse.cs`](https://github.com/frugocorp/AutoDiscoveryIAM/blob/master/src/AutoDiscoveryIAM/Common/ApiResponse/ApiResponse.cs) y [`ErrorInfo.cs`](https://github.com/frugocorp/AutoDiscoveryIAM/blob/master/src/AutoDiscoveryIAM/Common/ErrorInfo.cs) — y reutilizarlos tal cual en tu propia app, en vez de inventar tu propio envoltorio de respuesta.

##### EndPoint — ejemplo de uso de Metadata

Por qué es su propio archivo: conecta HTTP con el Handler — recibe el request, arma el Command, llama al Handler, y traduce el `OperationResult` a un código de estado HTTP real. Es también donde se declara, vía `.WithMetadata(...)`, qué nivel de autorización necesita el endpoint.

Este ejemplo usa `.WithMetadata(new PermissionsNotRequiredAttribute("..."))` a propósito, para que el endpoint funcione tal cual sin necesitar sembrar un `Permission`/`Role` propio primero — **no es la recomendación por default**, ver [Métodos de permisos usados en este ejemplo](https://autodiscoveryiam.frugocorp.com/tutorials/examples/vertical-slice-example#metodos-de-permisos-usados-en-este-ejemplo) más abajo para las demás opciones (RBAC estándar, `[RequiresDevelopmentAccess]`, `[RequiresSuAdminAccess]`, etc.).

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileEndPoint.cs`

```csharp
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public static class CreateUserProfileEndPoint
{
  public static void MapCreateUserProfileEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/user-profiles", async (
        CreateUserProfileRequestDto dto,
        CreateUserProfileHandler handler,
        CancellationToken cancellationToken) =>
    {
      var command = new CreateUserProfileCommand(dto.UserId, dto.Occupation);
      var result = await handler.Handle(command, cancellationToken);

      if (result.Success && result.Data != null)
      {
        var response = new CreateUserProfileResponse(true, result.Data);
        return Results.Created($"/user-profiles/{result.Data.PublicId}", response);
      }

      var errorResponse = new CreateUserProfileResponse(false, null, result.Error, result.ErrorInfo);
      return Results.Json(errorResponse, statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .WithName("CreateUserProfile")
    .WithTags("UserProfiles")
    .WithMetadata(new PermissionsNotRequiredAttribute("Ejemplo ilustrativo del README"));
  }
}
```

#### Query

`GetUserProfiles`, con la misma división de responsabilidades que `CreateUserProfile` — sin `RequestDto` (una consulta con parámetros simples de query string no necesita un DTO de body).

##### Contrato del Query

Por qué es su propio archivo: el registro de entrada de la consulta — parámetros de paginación, sin lógica. Igual que el Command, mantiene al Handler independiente de cómo llegan esos parámetros por HTTP.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesQuery.cs`

```csharp
namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

public record GetUserProfilesQuery(int PageNumber, int PageSize);
```

##### Repository

Por qué es su propio archivo: mismo motivo que en el Command — único lugar que toca el `DbContext`, para poder probar el Handler con un mock. Aquí además es donde se ve el beneficio concreto del contexto único: `.Include(p => p.User)` es un join nativo de EF Core, posible porque `UserProfile` y `User` (de la librería) viven en el mismo modelo.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;

namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

public interface IGetUserProfilesRepository
{
  Task<(List<UserProfile> UserProfiles, int TotalCount)> GetUserProfiles(int pageNumber, int pageSize);
}

public class GetUserProfilesRepository(ApplicationDbContext context) : IGetUserProfilesRepository
{
  private readonly ApplicationDbContext _context = context;

  public async Task<(List<UserProfile> UserProfiles, int TotalCount)> GetUserProfiles(int pageNumber, int pageSize)
  {
    var query = _context.UserProfiles.Include(p => p.User).OrderByDescending(p => p.CreatedAt);
    var totalCount = await query.CountAsync();
    var userProfiles = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
    return (userProfiles, totalCount);
  }
}
```

##### Handler

Por qué es su propio archivo: arma la lista paginada de resultados a partir de lo que devuelve el Repository. A diferencia del Command, aquí no hay `SaveChangesAsync` — una consulta no modifica datos.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesHandler.cs`

```csharp
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

public class GetUserProfilesHandler(IGetUserProfilesRepository repository)
{
  private readonly IGetUserProfilesRepository _repository = repository;

  public async Task<OperationResult<PagedResultDto<GetUserProfilesResponseDto>>> Handle(GetUserProfilesQuery query, CancellationToken cancellationToken)
  {
    var (userProfiles, totalCount) = await _repository.GetUserProfiles(query.PageNumber, query.PageSize);
    var dtoList = userProfiles.Select(p => new GetUserProfilesResponseDto(p.PublicId, p.User.Username, p.Occupation, p.CreatedAt));
    var pagedResult = new PagedResultDto<GetUserProfilesResponseDto>(dtoList, query.PageNumber, query.PageSize, totalCount);
    return OperationResult.Ok(pagedResult);
  }
}
```

> `PagedResultDto<T>` (de `FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination`) es el mismo envoltorio de paginación que usan las Queries de listado de la librería (ej. `GetRoles`) — también reutilizable en tu propia app. Nota también `p.User.Username` en el `Select` — viene del `Include(p => p.User)` del Repository, sin una segunda consulta ni un `DbContext` adicional.

##### DI

Por qué es su propio archivo: mismo motivo que en el Command — registro del slice aislado del resto.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesDI.cs`

```csharp
namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

public static class GetUserProfilesDI
{
  public static IServiceCollection AddGetUserProfilesDI(this IServiceCollection services)
  {
    services.AddScoped<IGetUserProfilesRepository, GetUserProfilesRepository>();
    services.AddScoped<GetUserProfilesHandler>();
    return services;
  }
}
```

##### ResponseDto

Por qué es su propio archivo: mismo motivo que en el Command — controla explícitamente qué campos de `UserProfile` (y del `User` relacionado) salen en la respuesta.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesResponseDto.cs`

```csharp
namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

public record GetUserProfilesResponseDto(Guid PublicId, string Username, string Occupation, DateTime CreatedAt);
```

##### Response

Por qué es su propio archivo: mismo motivo que en el Command — envoltorio tipado propio de este slice, esta vez envolviendo un `PagedResultDto<T>` en vez de un DTO simple.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesResponse.cs`

```csharp
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

public record GetUserProfilesResponse : ApiResponse<PagedResultDto<GetUserProfilesResponseDto>>
{
  public GetUserProfilesResponse(bool success, PagedResultDto<GetUserProfilesResponseDto>? data, string? error = null, ErrorInfo? errorInfo = null)
    : base(success, data, error, errorInfo) { }
}
```

##### EndPoint — ejemplo de uso de Metadata

Por qué es su propio archivo: mismo motivo que en el Command — conecta HTTP con el Handler y declara el nivel de autorización del endpoint.

Igual que en `CreateUserProfile`, usa `.WithMetadata(new PermissionsNotRequiredAttribute("..."))` a propósito para que el ejemplo funcione sin sembrar permisos primero — ver [Métodos de permisos usados en este ejemplo](https://autodiscoveryiam.frugocorp.com/tutorials/examples/vertical-slice-example#metodos-de-permisos-usados-en-este-ejemplo) más abajo para las demás opciones.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesEndPoint.cs`

```csharp
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

public static class GetUserProfilesEndPoint
{
  public static void MapGetUserProfilesEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapGet("/user-profiles", async (
        GetUserProfilesHandler handler,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 20) =>
    {
      var query = new GetUserProfilesQuery(pageNumber, pageSize);
      var result = await handler.Handle(query, cancellationToken);
      var response = new GetUserProfilesResponse(result.Success, result.Data, result.Error, result.ErrorInfo);
      return Results.Ok(response);
    })
    .WithName("GetUserProfiles")
    .WithTags("UserProfiles")
    .WithMetadata(new PermissionsNotRequiredAttribute("Ejemplo ilustrativo del README"));
  }
}
```

#### Métodos de permisos usados en este ejemplo

Los dos endpoints de este ejemplo usan `[PermissionsNotRequired]` a propósito, para que el ejemplo funcione tal cual sin necesitar sembrar un `Permission`/`Role` propio primero. **Esa no es la única opción, ni la recomendada por default** — ver la tabla completa de atributos disponibles (`[RequiresDevelopmentAccess]`, `[RequiresSuAdminAccess]`, `[RequirePermissions]`, o sin atributo para RBAC estándar) en [Autorización con niveles de acceso](#autorización-con-niveles-de-acceso). En tu app real, lo normal es dejar `CreateUserProfile`/`GetUserProfiles` **sin ningún atributo** (RBAC estándar, secure-by-default), sincronizar los permisos con `POST /permissions/sync` para que se creen `userprofiles.create_user_profile`/`userprofiles.get_user_profiles` (la feature `UserProfiles` solo se pasa a minúsculas, la acción a snake_case), y asignarlos al rol que corresponda — ver [Permisos para tus propios endpoints](https://autodiscoveryiam.frugocorp.com/docs/security-rbac/authorization#permisos-para-tus-propios-endpoints).

Por último, registra la cadena completa — igual que hace la librería internamente (`AddAutoDiscoveryIAM()` → `AddFeaturesDI()` → `AddRolesDI()` → `AddCreateRoleDI()`, y lo mismo para el mapeo de endpoints):

```csharp
// Cada slice registra lo suyo (ver CreateUserProfileDI.cs/GetUserProfilesDI.cs):
builder.Services.AddCreateUserProfileDI().AddGetUserProfilesDI();

// La feature completa registra todos sus slices en un solo lugar (ver UserProfilesDI.cs):
builder.Services.AddUserProfilesDI();
```

`AddCreateUserProfileDI()`/`AddGetUserProfilesDI()`/`AddUserProfilesDI()` van dentro de tu propio `Features/FeaturesDependencyInjection.cs` (el agregador, no `Program.cs` directo) — el equivalente para el mapeo de endpoints va en tu `Features/FeaturesEndpointMapping.cs`. `Program.cs` en sí **no cambia por cada feature que agregues**: solo necesita llamar `builder.Services.AddFeaturesDI()` y `app.MapAllFeatureEndpoints()` **una sola vez**, ya sea que tengas una feature o veinte — esas dos líneas ya están en el `Program.cs` completo de [Configura Program.cs](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#configura-programcs) más arriba (marcadas ahí como opcionales, por si todavía no tienes ninguna feature propia). Ver el ejemplo completo en [`samples/SimpleCode/Features/`](https://github.com/frugocorp/AutoDiscoveryIAM/tree/master/samples/SimpleCode/Features) para los 2 archivos concentradores ya armados.

---

<!-- ============================================================
     Fin de la macro-sección de librería. Lo que sigue es sobre este
     repositorio (cómo contribuir, correr el SampleHost, Docker,
     pruebas) -- no es información que un consumidor del paquete NuGet
     necesite para usar la librería en su propio proyecto.
     ============================================================ -->

---

### Ejemplo copiable: UserProfiles

[< Ejemplos](#ejemplos)

---

Esta carpeta **no es un proyecto que compile por sí solo** — son archivos de referencia, listos para copiar dentro de tu propio proyecto y renombrar. No forma parte de la solución (`.sln`), no se compila, no se prueba, y no afecta a `samples/AutoDiscoveryIAM.SampleHost` ni a ningún otro proyecto de este repo.

Es la versión completa del ejemplo explicado paso a paso en [Ejemplo: arquitectura Vertical Slice en tu propia app](#vertical-slice-en-tu-propia-app) — ahí se explica el *por qué* de cada archivo; aquí está el *qué*, completo, incluyendo las piezas que esa página solo describe (Validator, OpenAPI, Logging, Catalogs, Services).

`UserProfile` no es una entidad cualquiera: tiene un FK real (`UserId`) y una navegación (`User`) hacia el `User` de la librería. Es la respuesta en código a "¿cómo agrego más datos por usuario (estado civil, ocupación, etc.) si `User` es cerrado a modificación?" — ver [Elige tu propio DbContext: uno o dos](https://autodiscoveryiam.frugocorp.com/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos).

#### Qué copiar y cómo

Nota importante de estructura: `Data/` vive como **hermana** de `Features/` en esta misma carpeta `samples/SimpleCode/` — no dentro de `Features/UserProfiles/`. `ApplicationDbContext` es transversal (lo va a usar cualquier feature que agregues después, no solo UserProfiles), así que nunca debe quedar anidado dentro de una sola feature — mismo patrón que usa la propia librería (`src/AutoDiscoveryIAM/Data/` es hermana de `src/AutoDiscoveryIAM/Features/`, no está dentro de `Features/Roles/`). Por la misma razón, `Features/FeaturesDependencyInjection.cs` y `Features/FeaturesEndpointMapping.cs` viven como hermanos de `Features/UserProfiles/`, no dentro — son el punto único de registro de **todas** tus features, no solo de UserProfiles.

1. Copia `Data/ApplicationDbContext.cs` a tu propio proyecto, como hermana de tu `Features/`.
2. Copia `Features/UserProfiles/` dentro de tu propio `Features/`.
3. Copia `Features/FeaturesDependencyInjection.cs` y `Features/FeaturesEndpointMapping.cs` a tu propio `Features/` (si ya tienes los tuyos porque instalaste otra feature antes, solo agrégales la línea de UserProfiles).
4. Cambia el namespace `TuProyecto` por el de tu propio proyecto (buscar y reemplazar).
5. Registra tu `ApplicationDbContext` (contexto único, hereda `AutoDiscoveryIAMDbContext`) en tu `Program.cs` **en vez de** registrar `AutoDiscoveryIAMDbContext` por separado -- ver [Elige tu propio DbContext: uno o dos](https://autodiscoveryiam.frugocorp.com/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos) para el porqué de cada línea:

   ````code-block
   ```csharp
   builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(...));
   builder.Services.AddScoped<AutoDiscoveryIAMDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
   // ... AddAutoDiscoveryIAM(...) va DESPUÉS de estas dos líneas, no antes.
   builder.Services.AddFeaturesDI();
   // ...
   app.MapAllFeatureEndpoints();
   ```
   ````

6. Genera y aplica la migración de tu `ApplicationDbContext` (mismos comandos `dotnet ef` de siempre, apuntando a `ApplicationDbContext` en vez de a `AutoDiscoveryIAMDbContext` -- ver [Aplica las migraciones](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#aplica-las-migraciones)).

#### Qué es obligatorio y qué es opcional

Este ejemplo incluye todas las piezas completas a propósito — es más fácil borrar lo que no quieras que reconstruirlo desde un ejemplo simplificado.

- **Obligatorio** (sin esto el slice no compila): `../../Data/ApplicationDbContext.cs`, `Models/Entities/`, `Commands/CreateUserProfile/CreateUserProfileCommand.cs`, `CreateUserProfileRepository.cs`, `CreateUserProfileHandler.cs`, `CreateUserProfileDI.cs`, `CreateUserProfileRequestDto.cs`, `CreateUserProfileResponseDto.cs`, `CreateUserProfileResponse.cs`, `CreateUserProfileEndPoint.cs` (y sus equivalentes en `Queries/GetUserProfiles/`), `UserProfilesDI.cs`, `UserProfilesEndPointMapping.cs`, `../FeaturesDependencyInjection.cs`, `../FeaturesEndpointMapping.cs`.
- **Opcional — Validator + Catalogs** (`CreateUserProfileCommandValidator.cs`, `Infrastructure/Validation/`, `Models/Catalogs/MaritalStatusCatalog.cs`): requiere el paquete `FluentValidation`. El campo `MaritalStatus` se valida contra una lista cerrada de valores (`MaritalStatusCatalog.GetAll()`) en vez de aceptar cualquier texto — mismo patrón que usa la librería para sus propios catálogos cerrados (ver `Features/Users/Models/Catalogs/UserStatusCatalog.cs`). Si prefieres un campo de texto libre sin catálogo, borra `Infrastructure/Validation/`, `Models/Catalogs/`, quita la validación de `MaritalStatus` en el Validator, y simplifica el resto si no quieres validación en absoluto.
- **Opcional — OpenAPI** (carpetas `Docs/` en cada slice, y la línea `.WithMetadata(new ...OperationDocs())` en cada `EndPoint.cs`): requiere `AddOpenApi(...)` configurado en tu `Program.cs`. Si no vas a documentar la API con OpenAPI, borra las carpetas `Docs/` y esas líneas.
- **Opcional — Logging** (`Infrastructure/Logging/`, el parámetro `IUserProfileLogger logger` en los Handlers, y su registro en `UserProfilesDI.cs`): no requiere ningún paquete adicional (usa `Microsoft.Extensions.Logging`, ya incluido), pero puedes quitarlo igual si prefieres no tener logging por feature.
- **Opcional — Services/UserProfileQuery** (`Services/UserProfileQuery/`): consultas genéricas del dominio UserProfiles (buscar un perfil por `PublicId`, verificar si existe), reutilizables por cualquier slice futuro (`UpdateUserProfile`, `DeleteUserProfile`, etc.) sin reescribir la misma consulta en cada uno — mismo patrón que `Features/Roles/Services/RoleQuery/` de la librería. **Ninguno de los 2 slices de este ejemplo lo usa todavía** (`CreateUserProfile` resuelve el `UserId` directo en su propio Repository, y `GetUserProfiles` ya tiene su propia consulta paginada en el suyo) — está aquí como el lugar ya preparado para cuando agregues un slice que sí lo necesite. Si no planeas agregar más slices a UserProfiles, puedes borrarlo sin que nada se rompa.

#### Por qué no hay un `Data/UoW/` propio en este ejemplo

A diferencia de un ejemplo con dos `DbContext` separados, aquí `CreateUserProfileHandler` inyecta y usa directamente `IAutoDiscoveryIAMUnitOfWork` (de la librería, `FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW`) — no necesitas construir tu propio Unit of Work. En modo contexto único, `ApplicationDbContext` **es** (por herencia) un `AutoDiscoveryIAMDbContext`, así que `IAutoDiscoveryIAMUnitOfWork.SaveChangesAsync()` guarda también tus propias entidades (`UserProfile`), porque es literalmente la misma instancia de `DbContext` detrás de ambos. Si en cambio usaras dos `DbContext` separados, sí necesitarías tu propio Unit of Work para el tuyo (ver [Elige tu propio DbContext: uno o dos](https://autodiscoveryiam.frugocorp.com/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos)).

## Referencias

[REGRESAR A APARTADOS](#apartados)

---

## Índice Referencias

- [Configuración y pruebas](#configuración-y-pruebas) -- Claves de configuración, pruebas, cobertura y stack técnico.
  - [Configuración](#configuración) -- Todas las claves de configuración de la librería: obligatorias, recomendadas y opcionales.

---

## Configuración y pruebas

[< Índice Referencias](#índice-referencias)

---

### Configuración

[< Configuración y pruebas](#configuración-y-pruebas)

---

Todas las claves viven bajo `appsettings.json` (o variables de entorno equivalentes). Se dividen en tres grupos — validado en vivo instalando el paquete en un proyecto externo limpio, agregando configuración una sección a la vez:

#### Configuración obligatoria

Sin esto la app no arranca (falla al primer intento de conexión o por validación explícita al inicio):

| Sección             | Claves                      | Por qué es obligatoria                                                                                                                                                                                                                                                                  |
| ------------------- | --------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ConnectionStrings` | `DefaultConnection`         | Sin ella no hay forma de migrar/sembrar/consultar la base de datos — falla en cuanto tu `Program.cs` intenta usar el `DbContext` (ej. al migrar al arrancar).                                                                                                                           |
| `JwtSettings`       | `Key`, `Issuer`, `Audience` | Único par de secciones con validación explícita al arranque (`JwtSettingsValidator`, `ValidateOnStart()`): si falta alguna clave, o si `Key` tiene menos de **32 caracteres**, la app no llega a levantar, en vez de dejar el login silenciosamente roto o firmado con una clave débil. |

> ⚠️ **Además, en producción:** el anillo de claves de Data Protection no es una clave de `appsettings.json`, pero es igual de obligatorio. Ver [Instalación → Claves de Data Protection](#instalación-1).

**De dónde salen estos valores**, en general:

- `ConnectionStrings:DefaultConnection` depende de la base de datos que tú mismo levantes o ya tengas (host, usuario, contraseña que tú definiste al crearla) — ver [Necesitas una base de datos](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#necesitas-una-base-de-datos).
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

#### Configuración requerida solo si usas esa función

No tienen default posible (son credenciales propias de cada instalación), pero **no bloquean el arranque** — confirmado corriendo la app sin declarar ninguna de estas: solo fallan en el momento en que se intenta usar esa función específica (enviar un correo real, o completar un login de Google):

| Sección                 | Claves                                    | Cuándo falla si falta                                                                                               |
| ----------------------- | ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| `Mail:Smtp`             | `Host`, `User`, `Password`                | Al primer intento real de enviar un correo (activación de cuenta, recuperación de contraseña).                      |
| `Mail:From`             | `Email`                                   | Igual que arriba.                                                                                                   |
| `Authentication:Google` | `ClientId`, `ClientSecret`, `RedirectUri` | Solo si `Enabled: true` (default `false`, ver tabla de abajo) — al intentar completar el flujo de login con Google. |

#### Configuración recomendada

Ya trae un default seguro embebido en código — solo la tocas si quieres cambiar el valor:

| Sección                           | Default                                                                                                                                               | Se puede sobreescribir parcialmente (ej. solo un campo) |
| --------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------- |
| `JwtSettings.ExpiryMinutes`       | `15`                                                                                                                                                  | ✅                                                      |
| `RefreshTokenSettings`            | `ExpiryDays: 7`, `TokenLength: 32`                                                                                                                    | ✅                                                      |
| `Activation`                      | `EmailVerificationTokenLength: 64`, `EmailVerificationTokenExpiryMinutes: 30`, `ActivationResendCooldownSeconds: 60`, `ActivationResendMaxPerHour: 5`, `MinimumResponseMilliseconds: 1000` (duración mínima del reenvío público, exista o no la cuenta) | ✅                                                      |
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

> **Valores validados al arranque.** Estas secciones tienen default, pero un valor fuera de rango detiene el arranque en vez de dejar la protección rota sin aviso: `JwtSettings:Key` de al menos 32 bytes (se mide en UTF-8, así que un carácter acentuado cuenta doble) y `JwtSettings:ExpiryMinutes` entre 1 y 1440; `RefreshTokenSettings:TokenLength` de al menos 32 y `ExpiryDays` de al menos 1; `Lockout:MaxFailedAttempts` y `Lockout:LockoutMinutes` de al menos 1; `TwoFactor:CodeLength` entre 6 y 9, y `CodeExpiryMinutes`/`ChallengeTokenExpiryMinutes` de al menos 1.

> **Varias instancias de la API.** El logout invalida el access token vigente guardando su `jti` en el `IDistributedCache` de la app. La librería registra la versión en memoria, que con una sola instancia basta. Con varias instancias detrás de un balanceador, registra en tu `Program.cs` un caché distribuido compartido (por ejemplo `builder.Services.AddStackExchangeRedisCache(...)` o `AddDistributedSqlServerCache(...)`, en cualquier orden respecto a `AddAutoDiscoveryIAM()`): si no, un token cerrado en una instancia sigue sirviendo en las otras hasta que expira. Lo mismo aplica al anillo de claves de Data Protection (ver [Instalación → Claves de Data Protection](#instalación-1)) y, como dice la nota de `RateLimiting`, a los contadores de límites.

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

> **Login con Google y cuentas existentes.** Si el usuario tiene 2FA activo, el login con Google no emite tokens: responde igual que `POST /auth/login`, con `requiresTwoFactor: true` y un `twoFactorChallengeToken` que se completa con `POST /auth/2fa/verify` (con `ExternalLoginRedirectUri`, esa respuesta es la que entrega el canje del código). Una cuenta bloqueada por intentos fallidos tampoco entra por Google mientras dure el bloqueo. La primera vez que alguien entra con Google, su identidad se vincula a la cuenta que ya tenga ese email, salvo que esa cuenta tenga `IsAdmin`, `IsDevelopment` o `IsSuAdmin`: esas responden 409 y vinculan Google solo con la sesión iniciada (`POST /auth/google/link`), porque quien controle una cuenta de Google con ese correo (por ejemplo, el administrador del dominio en Google Workspace) obtendría sus privilegios. Si la cuenta existente nunca verificó su email, al vincularla se invalida su contraseña (el dueño define una nueva con el restablecimiento) y los tokens ya emitidos: cualquiera pudo registrarla con ese correo y una contraseña propia, esperando a que el dueño entrara con Google.

> **Secretos y tokens en la base de datos.** Los tokens de activación y de restablecimiento de contraseña se guardan solo como hash (SHA-256): quien lea la base de datos no puede usar ningún enlace pendiente. Por eso cada reenvío de activación genera un enlace nuevo y el anterior deja de servir. El secreto de Authenticator (TOTP) se guarda cifrado con ASP.NET Core Data Protection, y un código ya aceptado no se acepta de nuevo, aunque siga dentro de su ventana de 30 segundos. El token de reto del segundo factor (`twoFactorChallengeToken`) también queda solo como hash: el código de 6 dígitos no basta para completar un login sin él. El anillo de claves de Data Protection tiene que persistir entre reinicios y ser el mismo en todas las instancias: ver [Instalación → Claves de Data Protection](#instalación-1).

> **Registro y enumeración de cuentas.** `POST /users` con un email ya registrado responde igual que un registro nuevo y no crea nada: el dueño del correo recibe un aviso de que alguien intentó registrarse con él. Un username repetido sí responde 409. `DELETE /users/me` pide la contraseña actual en el cuerpo (`{ "currentPassword": "..." }`): un access token robado no alcanza para borrar la cuenta. Con una cuenta bloqueada por intentos fallidos, solo quien da la contraseña correcta recibe el aviso de bloqueo; con una incorrecta, `POST /auth/login` responde igual que con un email que no existe. `POST /users/me/email-change-requests` tampoco revela si el email nuevo ya pertenece a otra cuenta: la solicitud queda pendiente igual, y se rechaza al ejecutarla.

> **2FA en cuentas sin contraseña.** Una cuenta creada solo con login social no tiene contraseña que pedir al cambiar su 2FA. Si tiene Authenticator activo, activar el 2FA por correo, volver a enrolar el Authenticator o desactivarlo (`POST /users/me/2fa/...`) exige además un código válido del Authenticator actual en el campo `code` del cuerpo: un access token robado no alcanza para apagarlo ni para llevarlo a otro dispositivo.

#### Plantilla completa de `appsettings.json`

Esta es la plantilla real usada por el `SampleHost` de este mismo repo, con todas las secciones de las 3 tablas de arriba en un solo archivo — útil como punto de partida sin importar si instalaste el paquete desde NuGet o clonaste este repositorio (en cuyo caso, es exactamente el contenido de `samples/AutoDiscoveryIAM.SampleHost/appsettings.template.json`, y puedes copiarlo directo en vez de escribirlo a mano). Reemplaza cada `{{PLACEHOLDER}}` por tu propio valor real:

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
    "TokenLength": 32
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

`RefreshTokenSettings`, `Activation`, `PasswordReset`, `Lockout`, `TwoFactor` y `PasswordStruct` ya traen su default seguro embebido en código (ver [Configuración recomendada](https://autodiscoveryiam.frugocorp.com/references/configuration-testing/configuration#configuracion-recomendada) arriba) — se incluyen aquí completos solo como referencia de cómo se ven si decides sobreescribirlos, no porque sea obligatorio declararlos.

---
