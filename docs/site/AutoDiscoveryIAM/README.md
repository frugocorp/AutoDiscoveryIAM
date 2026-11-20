# AutoDiscoveryIAM

Sitio de documentacion generado con `create-ngx-docs-site`, sobre `@ngx-docs-markdown-kit/parser-md`
+ `parser-md-seo` + `parser-md-code-block` -- mas las extensiones opcionales que hayas instalado
(`parser-md-image`, `parser-md-card`, `parser-md-code-block-themes`).

## Desarrollo

```bash
npm install
npm start
```

SSR real con recarga en caliente en `http://localhost:4000`.

## Agregar una pagina

Cualquier `.md` dentro de `public/content-parser-md/` (menos los 3 archivos reservados con prefijo
`___`, ver abajo) es una pagina real -- el frontmatter usa corchetes:

```markdown
---
[slug]: mi-pagina
[title]: Mi pagina
[description]: Descripcion para SEO.
[keywords]: palabra1, palabra2
---

# Mi pagina

Contenido normal en Markdown.
```

`[slug]` es **obligatorio y siempre explicito** -- nunca se deriva del nombre del archivo (el
nombre en disco es 100% cosmetico). Una carpeta con contenido navegable necesita su propio
`___meta.md` (mismo formato de frontmatter, mas `[icon]` si es una seccion de nivel superior). El
orden entre hermanos es alfabetico por nombre de archivo/carpeta por default, o el explicito de un
`___order.md` en esa carpeta si existe. `npm start`/`npm run build` escanean `public/content-parser-md/`
solos y regeneran el manifiesto de navegacion -- nunca hay que registrar una pagina a mano en
ningun lado.

## Extensiones de Markdown disponibles

Ademas del Markdown estandar: ` ```code-block `/` ```card-code-block ` (bloques de codigo/tarjetas
de comando, siempre instalados), ` ```image ` (imagenes enriquecidas, `--no-image` para omitirlo),
` ```content-card ` (card de contenido con `<mat-card>` real, `--no-card` para omitirlo), y avisos
destacados (`> [!TIP]` etc.).

## Build

```bash
npm run build
```

Genera el bundle de browser + servidor (SSR) bajo `dist/autodiscoveryiam/`, mas
`robots.txt`/`sitemap.xml` reales a partir del contenido.

## README publicable (`npm run readme`)

```bash
npm run readme -- --github
```

Genera un `README.md` real (indice + contenido, badges automaticos, links resueltos) a partir de
este mismo contenido -- ver `@ngx-docs-markdown-kit/parser-md-converter`. No corre automaticamente
como parte de `npm run build`/`npm start` -- es un paso deliberado para cuando quieras publicar.

## Tests (npm-profiles)

Cada sitio generado trae su propio sistema de perfiles de test, organizado por categoria/sub-grupo/perfil (ver `npm-profiles/`):

```bash
npm run profile -- tests unit normal        # suite completa, rapido
npm run profile -- tests unit coverage      # suite completa + cobertura real (texto + HTML)
npm run profile -- tests links internal     # valida links internos del contenido (sin red)
npm run profile -- tests links external     # valida SOLO links externos (requiere red)
npm run profile -- tests links full         # internos + externos
npm run profile -- tests performance content # mide tiempo de parseo de cada pagina real
```

Agregar un perfil nuevo es un archivo JSON mas (ver `npm-profiles/tests/*/npm-profiles-tests-*.json`) -- nunca hay que tocar `package.json` ni `run-profile.mjs`. Dos cosas utiles que no son obvias a simple vista:

- **Excluir archivos/carpetas**: cada perfil acepta tambien `"exclude": ["src/app/legacy/**"]` (mismo formato que `include`) -- por ejemplo, "todos los `*.spec.ts` menos los de `legacy/`" es `"include": ["**/*.spec.ts"], "exclude": ["src/app/legacy/**"]`.
- **Profundidad libre**: no hay un numero fijo de niveles. `tests/links/internal` son 3, pero si tu equipo quiere anidar mas (ej. `tests/unit/auth/login`) o menos, solo agregas/quitas un archivo puente (`{ "description": "...", "file": "..." }`) en la cadena -- `run-profile.mjs` no necesita cambios, camina el arbol solo hasta encontrar una entrada sin `"file"` (esa es la hoja ejecutable).

## Configuracion rapida (`src/app/core/*.config.ts`)

`site.config.ts`, `resources-config.service.ts`, `design-config.service.ts` y
`frugocorp_modules/parser-md-seo/config/seo.config.ts` controlan nombre/logos/barras visibles/SEO
del sitio -- TS fuente compilado, NUNCA un JSON de `public/` pedido por HTTP en runtime (para que
el SSR tenga el valor real desde el primer render, sin depender de que un fetch resuelva a tiempo).
Cambiar un valor requiere editar el archivo y recompilar.

<!-- autodiscoveryiam:doc_start -->
<!-- autodiscoveryiam:doc_header_start -->
# AutoDiscoveryIAM

![AutoDiscoveryIAM](https://raw.githubusercontent.com/frugocorp/AutoDiscoveryIAM/main/docs/site/AutoDiscoveryIAM/public/images/FrugoCorp-Logo-300x300.jpg)

[![nuget](https://img.shields.io/nuget/v/FrugoCorp.AspNetCore.AutoDiscoveryIAM)](https://www.nuget.org/packages/FrugoCorp.AspNetCore.AutoDiscoveryIAM)
<!-- autodiscoveryiam:doc_header_end -->
<!-- autodiscoveryiam:doc_body_start -->
[GitHub](https://github.com/frugocorp/AutoDiscoveryIAM) | [NuGet](https://www.nuget.org/packages/FrugoCorp.AspNetCore.AutoDiscoveryIAM) | [Sitio](https://autodiscoveryiam.frugocorp.com) | [FrugoCorp](https://frugocorp.com)

`FrugoCorp.AspNetCore.AutoDiscoveryIAM` agrega autenticación JWT completa y RBAC dinámico a tus APIs de ASP.NET Core con minimal APIs. Los permisos se descubren solos a partir de tus endpoints reales, así que no tienes que declararlos a mano ni mantenerlos sincronizados cuando agregas uno nuevo.

## Empieza aquí

- [Instalación](#instalación-1): agrega la librería a tu proyecto paso a paso.
- [Introducción](#introducción): qué es y cómo está organizado el repositorio.
- [¿Qué incluye?](#qué-incluye): las funcionalidades disponibles.
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

- [Fundamentos](#fundamentos) -- Qué es AutoDiscoveryIAM, qué problema resuelve y qué funcionalidades incluye.
  - [Introducción](#introducción) -- Qué es AutoDiscoveryIAM, qué problema resuelve y cómo está organizado su repositorio.
  - [¿Qué incluye?](#qué-incluye) -- Funcionalidades disponibles hoy en la librería: autenticación, RBAC dinámico, 2FA y más.
- [Seguridad y RBAC](#seguridad-y-rbac) -- Autorización, auditoría, soft-delete y manejo de datos de terceros.
  - [Autorización con niveles de acceso](#autorización-con-niveles-de-acceso) -- Niveles de acceso, permisos descubiertos automáticamente y los dos ejes de autorización independientes.
  - [Reutilizar auditoría y soft-delete](#reutilizar-auditoría-y-soft-delete) -- Cómo reutilizar la auditoría y el soft-delete de la librería en tus propias entidades.
  - [Privacidad y datos de terceros](#privacidad-y-datos-de-terceros) -- Qué datos personales recibe y almacena la librería con proveedores de identidad externos y qué controles aplica.
- [Arquitectura](#arquitectura) -- Cómo se organiza el proyecto a nivel de infraestructura.
  - [Arquitectura Docker](#arquitectura-docker) -- Cómo se organizan los contenedores del proyecto y qué papel cumple cada uno.

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

Para empezar a usar la librería en tu propio proyecto, ve directo a [Instalación](#instalación-1). Si quieres ver primero qué trae hoy la librería en detalle (autenticación, RBAC dinámico, 2FA, etc.), revisa [¿Qué incluye?](#qué-incluye).

### ¿Qué incluye?

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

No hay ningún admin real sembrado. Las cuentas de ejemplo con contraseña pública solo se crean si las pides con `SeedAutoDiscoveryIAMDefaultDataAsync(includeDevelopmentUsers: true)` y el ambiente es `Development`/`Testing`; en cualquier otro ambiente se ignora el opt-in, y si esas cuentas existen activas se registra un log crítico al arrancar — ver [Credenciales de desarrollo por defecto](#credenciales-de-desarrollo-por-defecto)). El registro público (`POST /users`, o alta automática por login social) siempre crea usuarios sin rol y sin ningún flag especial. Para tener el primer administrador: que esa persona se registre con su correo real, y que quien administre la base de datos le asigne el rol `AdminAutoDiscoveryIAM` directamente en la base (ejemplo para SQL Server):

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

## Arquitectura

[< Índice Docs](#índice-docs)

---

### Arquitectura Docker

[< Arquitectura](#arquitectura)

---

#### api/

El contenedor de la API (.NET, consume `FrugoCorp.AspNetCore.AutoDiscoveryIAM`) — desarrollo
(`dotnet watch`) y producción, configuración vía `.env`, y el detalle de cómo acceder por dominio
junto con tu propio reverse proxy. Ver [Docker: api/](#docker-api).

---

#### app/web/

El contenedor del frontend Angular (SSR, `@angular/ssr`) — desarrollo (`ng serve`) y producción
(build real + `server.mjs`), configuración vía `.env`, genérico para reutilizarse con cualquier
proyecto Angular con SSR. Ver [Docker: app/web/](#docker-appweb).

---

#### db/

Un contenedor de SQL Server desechable, pensado únicamente para desarrollo local — para quien no
tiene ya una instancia propia a la mano y no quiere instalar SQL Server directamente en su
máquina. Ver [Docker: db/](#docker-db).

---

#### Proxy reverso (fuera de este repositorio)

Este repositorio no incluye ningún stack de nginx. `api/` y `app/web/` no publican puertos al host:
se conectan a la red externa `edge-net`, que crea el proxy reverso compartido del servidor,
[`nginx-server-docker`](https://github.com/frugocorp/nginx-server-docker) (repositorio aparte, con el TLS, los certificados y las plantillas
`.conf`). Ese stack tiene que estar levantado antes que ellos.

---

Cada carpeta es independiente: tiene su propio `docker-compose`, su propio `.env` (copiado de su
`.env.template`), y se levanta por separado — puedes usar `api/` con tu propia base de datos
remota sin tocar `db/`, o `db/` solo para tener algo con qué probar sin levantar la API en Docker.
`api/` y `app/web/` necesitan que `nginx-server-docker` ya esté levantado (crea la red `edge-net`).

## Tutoriales

[REGRESAR A APARTADOS](#apartados)

---

## Índice Tutoriales

- [Instalación](#instalación) -- Instala la librería, corre el host de ejemplo o empaqueta tu propia versión.
  - [Instalación](#instalación-1) -- Instala AutoDiscoveryIAM paso a paso: agrega el paquete NuGet, conecta tu base de datos, configura appsettings.json y aplica las migraciones.
  - [Actualizar AutoDiscoveryIAM](#actualizar-autodiscoveryiam) -- Cómo actualizar el paquete en una app que ya lo usa, sin perder datos, roles ni asignaciones.
  - [Correr el SampleHost](#correr-el-samplehost) -- Cómo ejecutar el host de ejemplo para explorar la API completa desde Swagger UI o Scalar.
  - [Empaquetar la librería desde tu fork](#empaquetar-la-librería-desde-tu-fork) -- Cómo generar tu propio paquete NuGet de la librería a partir de un fork.
- [Ejemplos](#ejemplos) -- Ejemplos para construir tus propias features sobre la librería.
  - [Vertical Slice en tu propia app](#vertical-slice-en-tu-propia-app) -- Cómo construir una feature con Vertical Slice Architecture en tu propia aplicación.
  - [Ejemplo copiable: UserProfiles](#ejemplo-copiable-userprofiles) -- Una feature UserProfiles completa, lista para copiar a tu proyecto y adaptarla.
- [Docker](#docker) -- Cómo levantar cada parte del proyecto en contenedores.
  - [Docker: api/](#docker-api) -- Cómo levantar en un contenedor la API del host de ejemplo.
  - [Docker: app/web/](#docker-appweb) -- Cómo levantar en un contenedor este sitio de documentación.
  - [Docker: db/](#docker-db) -- Cómo levantar una base de datos SQL Server de desarrollo en un contenedor.

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

> ⚠️ Si clonaste este repositorio desde el código fuente (sin que el paquete esté publicado todavía en NuGet.org), este comando **no va a funcionar tal cual** — ve primero a [Empaquetar la librería desde tu fork y probarla en otro proyecto](#empaquetar-la-librería-desde-tu-fork).

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

Instalar el paquete no basta — necesitas además una base de datos real y alcanzable. Si todavía no tienes una instancia de SQL Server a la mano, este mismo repositorio trae un contenedor Docker desechable listo para desarrollo local (ver [Docker: db/](#docker-db)):

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

Para ver todo esto ya funcionando sin integrarlo primero a tu propio proyecto, corre el [`SampleHost`](#correr-el-samplehost) de este mismo repo.

> 💡 **Patrón opcional: esperar la base de datos con reintentos al arrancar.** Si tu API y tu SQL Server corren en contenedores de Docker independientes, sin ningún `depends_on`/`healthcheck` que los ordene (el mismo criterio que usan `docker/api/` y `docker/db/` de este repo — cada stack se puede levantar por separado, ver [Docker](#arquitectura-docker)), nada garantiza que la base de datos ya acepte conexiones cuando arranca tu proceso. `SampleHost`/`Infrastructure/DatabaseInitialization.cs` resuelve esto con un reintento (10 intentos, 5s de espera cada uno) antes de continuar, y de paso aplica migraciones pendientes solo en Development (`dotnet ef database update` sigue siendo necesario en Producción). Es un patrón real y funcional, no solo un ejemplo — pero también es completamente opcional: no es parte de `AddAutoDiscoveryIAM()`/`UseAutoDiscoveryIAM()`, y un equipo cuyo propio orquestador ya garantice el orden de arranque (o que simplemente prefiera aplicar migraciones a mano antes de levantar la app) puede omitirlo sin perder nada. Cópialo de `samples/AutoDiscoveryIAM.SampleHost/Infrastructure/DatabaseInitialization.cs` si te sirve tal cual.

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

Para probar una versión nueva en tu proyecto real antes de publicarla, empaquétala a un feed local con una versión preliminar única (`0.2.0-local.1`, `0.2.0-local.2`...) e instálala desde ahí, como se explica en [Empaquetar la librería desde tu fork y probarla en otro proyecto](#empaquetar-la-librería-desde-tu-fork).

### Correr el SampleHost

[< Instalación](#instalación)

---

Restaura y compila:

```bash
dotnet build
```

Aplica las migraciones (requiere una cadena de conexión válida en `appsettings.json`/`.env`):

```bash
dotnet ef database update --project samples/AutoDiscoveryIAM.SampleHost --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
```

Corre la API de ejemplo (Swagger UI en la raíz, Scalar en `/scalar`):

```bash
dotnet run --project samples/AutoDiscoveryIAM.SampleHost
```

Accede vía Swagger UI (raíz, `https://localhost:7001/`) o Scalar (`/scalar`) — ambas viven en `samples/AutoDiscoveryIAM.SampleHost`, no en la librería. Login vía API con las [credenciales de desarrollo por defecto](#credenciales-de-desarrollo-por-defecto):

Todas las respuestas de la API (login incluido) van envueltas en el sobre `ApiResponse<T>` -- `success`/`data`/`error`/`errorInfo`.

```bash
curl -X POST "https://localhost:7001/auth/login" \
     -H "Content-Type: application/json" \
     -d '{
       "email": "admin@example.com",
       "password": "AdminPass123!"
     }'
```

**Resultado esperado:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2026-08-02T15:30:00Z",
    "refreshToken": "8f14e45f-ceea-467e-9e1e-c9d3a3f2b1a0",
    "requiresTwoFactor": false,
    "twoFactorChallengeToken": null
  },
  "error": null,
  "errorInfo": null
}
```

> **Nota:** Si el usuario tiene 2FA activo, `requiresTwoFactor` sale `true` y `token`/`expiresAt`/`refreshToken` salen `null` hasta verificar el segundo factor vía `POST /auth/2fa/verify` con el `twoFactorChallengeToken` recibido aquí. Con credenciales incorrectas, `success` sale `false` y `data` sale `null` -- el detalle del error va en `error`/`errorInfo`.

Para reiniciar la base de datos local desde cero:

Elimina la base de datos por completo (todas las tablas, todos los datos).

```bash
dotnet ef database drop --project samples/AutoDiscoveryIAM.SampleHost --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext --force
```

> **Nota:** `--force` omite la confirmación interactiva -- este comando es destructivo e irreversible, úsalo solo contra tu base de datos de desarrollo local, nunca contra una compartida o de producción.

Vuelve a aplicar las migraciones desde cero:

```bash
dotnet ef database update --project samples/AutoDiscoveryIAM.SampleHost --context FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.AutoDiscoveryIAMDbContext
```

### Empaquetar la librería desde tu fork

[< Instalación](#instalación)

---

Si clonaste o forkeaste este repo (código fuente, sin que el paquete esté publicado todavía en NuGet.org), el comando de [Instalación](#instalación-1) (`dotnet add package FrugoCorp.AspNetCore.AutoDiscoveryIAM`) **no va a funcionar tal cual** — ese comando asume que el paquete ya está en NuGet.org, y todavía no lo está. Puedes generar tu propio `.nupkg` local y consumirlo desde un proyecto de prueba aparte exactamente igual que si viniera de un feed real. Este es el mismo flujo que ya se validó en vivo antes de considerar la librería lista para usarse.

**Requisito: SDK de .NET 10 instalado.** Verifica con `dotnet --version`; si no lo tienes, instálalo desde la [página oficial de descargas de .NET](https://dotnet.microsoft.com/download) antes de continuar.

#### Genera el paquete

Hacia una carpeta que sirva como "feed" local (puede ser cualquier carpeta en tu máquina, dentro o fuera de este repo):

```bash
dotnet pack src/AutoDiscoveryIAM/AutoDiscoveryIAM.csproj -c Release -o "C:\ruta\a\tu\feed-local"
```

> ⚠️ Si en vez de correr `dotnet pack` navegas manualmente a `src/AutoDiscoveryIAM/bin/Release/net10.0/`, vas a encontrar 4 archivos sueltos (`.dll`, `.pdb`, `.xml`, `.deps.json`) — esos son la compilación cruda, **no** el paquete instalable, y copiarlos a mano a otro proyecto no funciona (le faltarían todas las dependencias de la librería: EF Core, JWT, FluentValidation, etc.). El único archivo que necesitas es el `.nupkg` que genera `dotnet pack` en la carpeta que le indiques con `-o` — ese sí trae listada la lista completa de dependencias para que NuGet las resuelva solo.

#### Declara el feed local

En tu proyecto de prueba, agrega un `NuGet.config` que declare esa carpeta como fuente:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="feed-local" value="C:\ruta\a\tu\feed-local" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

#### Instala desde tu feed local

Igual que en "Instalación" pero sin depender de una publicación real: [Agrega la librería a tu proyecto](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#agrega-la-libreria-a-tu-proyecto)

A partir de aquí, el resto es exactamente lo mismo que en [Instalación](#instalación-1): registrar `AddAutoDiscoveryIAM()`/`UseAutoDiscoveryIAM()`, elegir tu proveedor de base de datos, generar tus migraciones y aplicar la configuración mínima obligatoria.

#### Si repites la misma versión (limpia la caché de NuGet)

Si vuelves a correr `dotnet pack` con la misma versión después de cambiar algo en la librería, limpia la copia en caché de NuGet de **este paquete específico** antes de reinstalar, para que tu proyecto de prueba tome el contenido nuevo y no el que ya tenía guardado. Corre esto en una terminal (PowerShell en Windows) — no es parte de tu código, es limpieza de la caché de NuGet de tu máquina:

```powershell
Remove-Item -Recurse -Force "$env:USERPROFILE\.nuget\packages\frugocorp.aspnetcore.autodiscoveryiam\<version>"
```

Reemplaza `<version>` por la versión real que empaquetaste (ej. `0.1.0`). Esto borra solo la copia en caché de esta librería — no toca la caché de ningún otro proyecto .NET en tu máquina.

> Si PowerShell responde `No se encuentra la ruta de acceso ... porque no existe`, no es un error que te bloquee — significa que esa caché ya estaba limpia (por ejemplo, si ya la habías borrado antes). Puedes seguir directo al siguiente paso.

**Después de esto** (haya borrado algo o no), vuelve a instalar el paquete para que tu proyecto tome el contenido nuevo desde tu feed local: [Agrega la librería a tu proyecto](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#agrega-la-libreria-a-tu-proyecto)

#### El build funciona pero el editor sigue marcando error

Si repites la misma versión del paquete (ver punto anterior) y luego ves un tipo de la librería subrayado en rojo con *"The type or namespace name '...' could not be found"* — hay dos cachés **distintas e independientes** que pueden estar desactualizadas, y limpiar una no limpia la otra:

1. **El build server de MSBuild/Roslyn** (`VBCSCompiler`) — un proceso en segundo plano que el SDK de .NET deja vivo entre compilaciones para que la siguiente sea más rápida. Si repites la misma versión de un paquete, a veces sigue con el ensamblado viejo cargado en memoria y no se entera del cambio. Se soluciona con:

   ````code-block
   ```bash
   dotnet build-server shutdown
   ```
   ````

   Es seguro y no afecta nada más: no borra archivos, no toca NuGet, no afecta ninguna app que ya esté corriendo (`dotnet run`, `dotnet watch`, etc. son procesos de ejecución, no de compilación). Es una acción de toda la máquina, no solo de este proyecto — el único "costo" es que la próxima compilación de cualquier proyecto tuyo tarda un poco más al arrancar el compilador de nuevo.

2. **El servidor de lenguaje de tu editor** (OmniSharp o C# Dev Kit en VS Code) — es un proceso **separado** del anterior, con su propia copia en memoria del ensamblado para IntelliSense. Apagar el build server de MSBuild no lo toca. Si ya confirmaste que `dotnet build` desde terminal compila bien pero el editor sigue marcando el error, el problema es este, no el anterior. Arréglalo sin cerrar el proyecto:

   - `Ctrl+Shift+P` → **"Developer: Reload Window"** (recarga VS Code y todas sus extensiones).
   - Si sigue el subrayado: con la extensión clásica **C#**, `Ctrl+Shift+P` → **"OmniSharp: Restart OmniSharp"**; con **C# Dev Kit**, busca algo como **"Restart Language Server"**.
   - Cerrar y volver a abrir VS Code por completo también funciona (equivalente a "Reload Window", pero más lento).

## Ejemplos

[< Índice Tutoriales](#índice-tutoriales)

---

### Vertical Slice en tu propia app

[< Ejemplos](#ejemplos)

---

La librería está construida internamente con **Vertical Slice Architecture** (ver [¿Qué incluye?](#qué-incluye)) — un Command/Query por carpeta, con su propio `Handler`, `Repository`, `DI` y `EndPoint`, sin capa MVC/Controllers y sin MediatR. Nada te obliga a seguir el mismo patrón en tu propia app (un `DbContext` + minimal APIs normales funcionan igual de bien), pero si tu equipo no tiene ya una arquitectura definida, te damos un ejemplo completo y funcional para que la adoptes si te sirve — queda naturalmente compatible con el resto de la librería, y es la misma arquitectura que ya viste en [Reutilizar auditoría y soft-delete en tus propias entidades](#reutilizar-auditoría-y-soft-delete).

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

## Docker

[< Índice Tutoriales](#índice-tutoriales)

---

### Docker: api/

[< Docker](#docker)

---

#### Desarrollo (por defecto)

Entra a la carpeta:

```bash
cd docker/api
```

Construye y levanta el contenedor:

```bash
docker-compose up --build
```

Docker Compose fusiona automáticamente `docker-compose.yml` (base) con
`docker-compose.override.yml` (que sí existe en este directorio) cuando no se especifica
`-f` explícitamente. `docker-compose.override.yml` define `target: dev` para la API, así que
este comando **siempre levanta la imagen de desarrollo** (`dotnet watch run`, con el código
fuente montado como volumen), sin importar lo que diga `docker-compose.yml` por sí solo.

---

#### Producción

Para levantar solo la imagen de producción (`target: final`, sin `dotnet watch` ni montaje de
código fuente), hay que excluir explícitamente el override:

Entra a la carpeta:

```bash
cd docker/api
```

Construye y levanta solo la imagen de producción, excluyendo el override de desarrollo:

```bash
docker-compose -f docker-compose.yml up --build -d
```

`docker-compose.yml` fija `target: final` de forma explícita para este servicio — antes no lo
hacía, y como el `Dockerfile` tiene la etapa `dev` como última etapa textual, Docker usaba `dev`
como destino implícito incluso al excluir el override. Con `target: final` explícito, este
comando siempre construye y levanta la imagen de producción real.

---

#### Configuración (`.env`)

Copiar `.env.template` a `.env` y completar los valores antes de levantar cualquiera de los dos
modos. Para qué sirve cada variable, ver los comentarios en `docker-compose.yml` (el archivo que
sí se trackea, a diferencia de `.env`) — no se repiten aquí para no mantener la misma explicación
en dos lugares.

---

#### Reutilizar esta carpeta en otro proyecto

Pensada para copiarse tal cual (`docker/api/` completa: `Dockerfile`, `docker-compose.yml`,
`.override.yml`, `.env.template`) a cualquier otro proyecto .NET:

- **Solo con ajustar `.env`** (`API_BUILD_CONTEXT`, `API_DOCKERFILE_PATH`, `API_PROJECT_PATH`,
  `API_PROJECT_NAME`, puertos, feed local de NuGet si aplica) ya apunta al proyecto nuevo — el
  `Dockerfile` es genérico, no tiene el nombre de ningún proyecto ni paquete hardcodeado.
- **Lo único que sí hay que buscar y reemplazar a mano** son los nombres que Docker Compose no
  permite templatizar con variables (confirmado en la práctica: Compose valida las llaves del YAML
  contra su esquema antes de sustituir variables) — el nombre del servicio (`adiam-api`) en los 3 archivos. La red `edge-net` **no** se renombra:
  es la red compartida de todo el servidor que crea `nginx-server-docker`, y la usan todos los
  proyectos que ese proxy publica.

---

#### Base de datos (no incluida)

Este `docker-compose` **no** levanta ningún contenedor de SQL Server — solo el contenedor de la
API. Necesitas una SQL Server alcanzable desde el contenedor (tu propia instancia local, un
servidor remoto, o el contenedor desechable de [`../db/`](#docker-db) pensado justo para
esto), apuntada vía `ConnectionStrings__DefaultConnection` en `appsettings.json` o como variable
de entorno. Sin esto, el contenedor arranca, pero la conexión a base de datos
falla en el arranque (reintentos con backoff, ver `Program.cs` del `SampleHost`) y la app no queda
operativa.

---

#### Proxy reverso compartido

Este `docker-compose.yml` no publica ningún puerto al host, a propósito: la única forma de llegar
al contenedor es por la red Docker `edge-net`. Esa red **no** la crea este stack (`external: true`):
la crea el proxy reverso compartido del servidor, [`nginx-server-docker`](https://github.com/frugocorp/nginx-server-docker), un repositorio
aparte que se encarga del TLS, de los certificados y de las plantillas `.conf` que hacen
`proxy_pass` hacia `adiam-api:API_PORT_CONTAINER`. Por eso ese stack tiene que estar levantado
**antes** que este: si la red no existe, `docker compose up` falla con un error claro en vez de
crear una red propia.

Si necesitas pegarle al contenedor directo para depurar, agrega un `ports:` temporal en un archivo
de Compose adicional (`docker compose -f docker-compose.yml -f tu-archivo.yml up`), sin versionarlo,
y bórralo después.

##### `UseForwardedHeaders()` es obligatorio detrás de un proxy que termina TLS

Como el tráfico llega por un proxy que termina TLS y reenvía tráfico plano al contenedor,
tu propio `Program.cs` necesita `app.UseForwardedHeaders();` **antes** de `app.UseAutoDiscoveryIAM();`
— sin esto, `Request.Scheme` siempre se ve como `http` (lo que tu API recibe internamente), aunque
el sitio sea `https` desde afuera, y eso rompe cosas como las URLs absolutas que genera Swagger
UI/Scalar. Ver el aviso 💡 correspondiente en la sección [Configura Program.cs](https://autodiscoveryiam.frugocorp.com/tutorials/getting-started/installation#configura-programcs)
— `AddAutoDiscoveryIAM()` ya configura qué headers confiar y desde qué rango
de red (`172.16.0.0/12`, el que Docker asigna por defecto a sus redes bridge), pero la llamada a
`UseForwardedHeaders()` en sí queda a propósito en tu propio `Program.cs`, porque es una decisión de
topología de red específica de cada despliegue.

##### Hardening recomendado para tu propia configuración de nginx

Si usas tu propio stack de nginx (en vez de `nginx-server-docker`) para exponer este contenedor
con TLS, unos puntos verificados en la práctica que vale la pena aplicar en tu `.conf`:

- **`server_tokens off;`** — sin esto, nginx expone su versión exacta en el header `Server` y en las
  páginas de error por default, facilitando que alguien busque CVEs específicos de esa versión.
- **`add_header ... always;`** en cada header de seguridad (`Strict-Transport-Security`,
  `X-Frame-Options`, `X-Content-Type-Options`, `Referrer-Policy`, `Permissions-Policy`) — sin
  `always`, nginx solo los manda en respuestas 2xx/3xx, justo cuando no van en un 4xx/5xx (que es
  cuando más importan, con alguien sondeando el servidor).
- **Lista explícita de cifrados** (perfil "Intermediate" de Mozilla) en vez de alias viejos tipo
  `EECDH`/`EDH`, que ya no distinguen cifrados débiles:
  ````code-block
  ```text
  ssl_ciphers "ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305";
  ```
  ````
  TLSv1.3 no necesita listarse: sus cifrados los negocia OpenSSL aparte y ya son todos fuertes por diseño.
- **Cuidado con `HSTS preload` sobre un certificado wildcard compartido entre varios proyectos**:
  activar `preload` en `Strict-Transport-Security` aplica esa política a **todo** el dominio en los
  navegadores que ya la vieron (Chromium la trae precargada de fábrica), y es casi irreversible —
  sacarlo de la lista de Chromium tarda meses. Si algún otro subdominio del mismo wildcard aún no
  tiene https listo, queda roto. Considera dejar solo `max-age` + `includeSubdomains` hasta que
  todos los proyectos que comparten el certificado estén listos para `preload` a la vez.
- **Rate limiting a nivel de borde** (`limit_req_zone`, declarado fuera de cualquier `server{}`, y
  `limit_req` dentro del `location`) — defensa en profundidad: la app (`RateLimiting`, con el
  limitador nativo de ASP.NET Core, ver [Configuración](#configuración)) ya limita por endpoint,
  pero eso corre *después* de que la petición ya atravesó todo el pipeline de .NET. Una zona en el
  borde frena floods genéricos antes de que le cuesten CPU/DB a la API.
- **Certificado servido = hoja + cadena intermedia concatenados**, nunca solo la hoja — sin la
  cadena intermedia, clientes con validación estricta (curl sin bundle de CAs del sistema, libs
  Java/Android viejas) pueden fallar el handshake, aunque Chrome/Firefox lo disimulen completando la
  cadena solos vía AIA. **Si usas Let's Encrypt** (como permite [`nginx-server-docker`](https://github.com/frugocorp/nginx-server-docker)),
  esto ya viene resuelto automáticamente (`fullchain.pem`/`chain.pem` generados solos, sin fusionar
  nada a mano) — lo de abajo solo aplica si ya tienes un certificado descargado de tu VPS/proveedor
  cloud/comprado aparte. Si tu proveedor te entrega la hoja y los intermedios por separado, en
  PowerShell (Windows) puedes fusionarlos así — usa siempre rutas **absolutas** con
  `[System.IO.File]`, porque `Set-Location`/`cd` no siempre actualiza el directorio de trabajo real
  que usan los métodos estáticos de .NET:
  ````code-block
  ```powershell
  $sslDir = "C:\ruta\a\tu\carpeta\ssl\TuDominio"

  # 1. Respaldo de la hoja original antes de tocarla (por si algo sale mal)
  Copy-Item "$sslDir\TuDominio_ssl_certificate.cer" "$sslDir\TuDominio_ssl_certificate.cer.leaf-only.bak"

  # 2. Fusionar los intermedios en un solo archivo de cadena (en orden, sin el root CA)
  [System.IO.File]::WriteAllText("$sslDir\_.TuDominio_ssl_TRUSTED_CHAIN_INTERMEDIATE.cer", (Get-Content "$sslDir\intermediate1.cer" -Raw))
  [System.IO.File]::AppendAllText("$sslDir\_.TuDominio_ssl_TRUSTED_CHAIN_INTERMEDIATE.cer", (Get-Content "$sslDir\intermediate2.cer" -Raw))

  # 3. Pegar la cadena intermedia al final del certificado hoja (fullchain in-place)
  [System.IO.File]::AppendAllText("$sslDir\TuDominio_ssl_certificate.cer", (Get-Content "$sslDir\_.TuDominio_ssl_TRUSTED_CHAIN_INTERMEDIATE.cer" -Raw))
  ```
  ````
  El archivo de cadena intermedia (paso 2) también sirve como valor de `ssl_trusted_certificate`
  (necesario para que `ssl_stapling_verify` pueda validar la respuesta OCSP) — debe apuntar solo a
  la cadena, nunca a la hoja.

Nada de esto es parte de `AddAutoDiscoveryIAM()`/`UseAutoDiscoveryIAM()`: la librería no dicta cómo
configuras tu proxy reverso, esto es guía práctica para quien decida usar nginx como el suyo propio.

### Docker: app/web/

[< Docker](#docker)

---

#### Desarrollo (por defecto)

Entra a la carpeta:

```bash
cd docker/app/web
```

Construye y levanta el contenedor:

```bash
docker-compose up --build
```

Docker Compose fusiona automáticamente `docker-compose.yml` (base) con
`docker-compose.override.yml` (que sí existe en este directorio) cuando no se especifica `-f`
explícitamente. `docker-compose.override.yml` define `target: dev` para el servicio, así que este
comando **siempre levanta la imagen de desarrollo** (`ng serve --host 0.0.0.0`, con el código
fuente montado como volumen, equivalente al `dotnet watch` de `docker/api/`).

---

#### Producción

Para levantar solo la imagen de producción (`target: final`, build real con SSR — sin `ng serve`
ni montaje de código fuente), hay que excluir explícitamente el override:

Entra a la carpeta:

```bash
cd docker/app/web
```

Construye y levanta solo la imagen de producción, excluyendo el override de desarrollo:

```bash
docker-compose -f docker-compose.yml up --build -d
```

La etapa `final` corre `node dist/<APP_PROJECT_NAME>/server/server.mjs` — el servidor Express que
genera `@angular/ssr`, no un servidor de archivos estáticos. Igual que en `docker/api/`,
`docker-compose.yml` fija `target: final` de forma explícita porque el `Dockerfile` tiene la
etapa `dev` como última etapa textual (Docker usaría `dev` como destino implícito si no se
especificara).

---

#### Configuración (`.env`)

Copiar `.env.template` a `.env` y completar los valores. Para qué sirve cada variable, ver los
comentarios en `docker-compose.yml`/`.env.template` — no se repiten aquí para no mantener la misma
explicación en dos lugares. Dos cosas puntuales que vale la pena resaltar:

- **`APP_ALLOWED_HOSTS`** — Angular 22+ valida el header `Host` como protección nativa contra
  SSRF (ver [angular.dev/best-practices/security](https://angular.dev/best-practices/security#preventing-server-side-request-forgery-ssrf))
  y responde `400 Bad Request` para cualquier host no listado aquí, **incluido** el que reenvía
  nginx como proxy reverso. Debe incluir tu dominio real y, si quieres poder probar este
  contenedor directo sin pasar por nginx, `localhost` también — **nunca con el puerto** (Angular
  valida contra el hostname ya separado del puerto, `localhost:8010` nunca hace match, solo
  `localhost`). Nunca uses `*`, desactiva la protección por completo.
- Este valor se duplica a mano respecto al dominio que declara la plantilla `.conf` de este sitio
  en `nginx-server-docker` — son repositorios independientes, sin variables compartidas entre sí
  a propósito.

---

#### Reutilizar esta carpeta en otro proyecto

Pensada para copiarse tal cual (`docker/app/web/` completa: `Dockerfile`, `docker-compose.yml`,
`.override.yml`, `.env.template`) a cualquier otro proyecto Angular con SSR (`@angular/ssr`):

- **Solo con ajustar `.env`** (`APP_BUILD_CONTEXT`, `APP_DOCKERFILE_PATH`, `APP_PROJECT_PATH`,
  `APP_PROJECT_NAME`, `APP_ALLOWED_HOSTS`, puertos) ya apunta al proyecto nuevo — el `Dockerfile`
  es genérico: no tiene el nombre del proyecto hardcodeado en ningún lado, solo asume la
  convención estándar del CLI (`dist/<PROJECT_NAME>/browser` + `dist/<PROJECT_NAME>/server/server.mjs`).
- **Lo único que sí hay que buscar y reemplazar a mano** es el nombre del servicio en los 3
  archivos de Compose — Docker Compose no permite templatizar nombres de servicio con variables
  (mismo motivo documentado en [Docker: api/](#docker-api)).
- Si el proyecto Angular **no** usa SSR (solo SPA estático), este `Dockerfile` no aplica tal
  cual — la etapa `final` necesita ajustarse para servir `dist/<PROJECT_NAME>/browser` con un
  servidor de archivos estáticos (o con el propio nginx directamente) en vez de correr
  `server.mjs`.

---

#### Proxy reverso compartido

Igual que `docker/api/`: este `docker-compose.yml` no publica ningún puerto al host, a propósito, y
el contenedor solo es alcanzable por la red externa `edge-net`, que crea
[`nginx-server-docker`](https://github.com/frugocorp/nginx-server-docker) (repositorio aparte). Ese proxy hace `proxy_pass` hacia
`APP_CONTAINER_NAME:APP_PORT_CONTAINER` con la plantilla `.conf` de este sitio, y tiene que estar
levantado antes que este stack.

### Docker: db/

[< Docker](#docker)

---

#### Qué es

Un contenedor de SQL Server (`mcr.microsoft.com/mssql/server:2025-latest`, con `sqlcmd`/`bcp` instalados) pensado **únicamente para desarrollo local** — para quien no tiene ya una instancia propia de SQL Server a la mano y no quiere instalarla directamente en su máquina. **No está pensado para producción**: no trae backups, réplicas, ni ningún endurecimiento de seguridad más allá de lo que ya trae la imagen oficial.

---

#### Levantarlo

Entra a la carpeta:

```bash
cd docker/db
```

Copia la plantilla de variables de entorno (completa los valores antes de levantarlo):

```bash
cp .env.template .env
```

Construye y levanta el contenedor en segundo plano:

```bash
docker-compose up --build -d
```

---

#### Variables de `.env`

| Variable                  | Para qué sirve                                                                         |
| -------------------------- | --------------------------------------------------------------------------------------- |
| `MSSQL_DB_NAME`             | Nombre de la base de datos a crear.                                                     |
| `MSSQL_SA_PASSWORD`         | Contraseña del usuario `sa`. Debe cumplir la política de contraseñas de SQL Server (mínimo 8 caracteres, mayúscula, minúscula, número y/o símbolo) o el contenedor falla al arrancar. |
| `MSSQL_ACCEPT_EULA`         | Debe ser `Y` — sin esto la imagen oficial de Microsoft no arranca.                       |
| `MSSQL_PID`                 | Edición de SQL Server (ej. `Developer`) — `Developer` es gratuita y suficiente para desarrollo local. |
| `MSSQL_TCP_PORT_HOST`       | Puerto expuesto en tu máquina (ej. `1433`).                                             |
| `MSSQL_TCP_PORT_CONTAINER`  | Puerto interno del contenedor (normalmente el mismo `1433`).                             |
| `MSSQL_VOLUME_PERSIST_DB`   | Ruta/volumen donde persisten los datos entre reinicios del contenedor.                   |

---

#### Conectar la API a este contenedor

Apunta `ConnectionStrings:DefaultConnection` (en tu `appsettings.json`, o `ConnectionStrings__DefaultConnection` como variable de entorno) a `localhost` y el puerto que hayas puesto en `MSSQL_TCP_PORT_HOST`:

```text
Server=localhost,1433;Initial Catalog={{MSSQL_DB_NAME}};User ID=sa;Password={{MSSQL_SA_PASSWORD}};TrustServerCertificate=True;
```

Si en vez de correr la API directo en tu máquina la corres también en Docker (ver [Docker: api/](#docker-api)), ambos contenedores necesitan estar en la misma red de Docker para que el nombre del servicio (`adiam-mssql`) resuelva entre ellos — hoy cada `docker-compose` de esta carpeta es independiente y no comparte red con `api/` automáticamente; conéctalos a la misma red externa si necesitas ambos contenedores hablándose entre sí.

## Referencias

[REGRESAR A APARTADOS](#apartados)

---

## Índice Referencias

- [Configuración y pruebas](#configuración-y-pruebas) -- Claves de configuración, pruebas, cobertura y stack técnico.
  - [Configuración](#configuración) -- Todas las claves de configuración de la librería: obligatorias, recomendadas y opcionales.
  - [Pruebas](#pruebas) -- Cómo están organizadas las pruebas del proyecto y cómo ejecutarlas.
  - [Configuración de coverage](#configuración-de-coverage) -- Cómo medir la cobertura de pruebas y generar sus reportes.
  - [Timeouts de las pruebas de performance](#timeouts-de-las-pruebas-de-performance) -- Por qué los umbrales de tiempo de las pruebas de performance son generosos y cuáles son.
  - [Stack técnico](#stack-técnico) -- Tecnologías sobre las que está construido el proyecto.
- [Desarrollo](#desarrollo) -- Credenciales, solución de problemas y perfiles de pruebas para trabajar en el repositorio.
  - [Credenciales de desarrollo por defecto](#credenciales-de-desarrollo-por-defecto) -- Usuarios y contraseñas que se crean por defecto en un entorno de desarrollo.
  - [Troubleshooting](#troubleshooting) -- Problemas comunes al ejecutar el proyecto y cómo resolverlos.
  - [RunSettings (perfiles de test)](#runsettings-perfiles-de-test) -- Perfiles de ejecución de pruebas y cuándo conviene usar cada uno.
  - [Sitio de documentación (Angular)](#sitio-de-documentación-angular) -- Cómo está organizado el sitio de documentación de AutoDiscoveryIAM, una app Angular con SSR.

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

### Pruebas

[< Configuración y pruebas](#configuración-y-pruebas)

---

Corre toda la suite, sin cobertura ni reporte HTML:

```bash
dotnet test
```

`run-tests-with-report.ps1` es la forma recomendada de correr la suite — envuelve `dotnet test` con un perfil `.runsettings` con nombre y genera un reporte HTML al final:

```powershell
.\run-tests-with-report.ps1 -RunsettingsType "normal"
```

Hay 4 perfiles ([`runsettings/*.runsettings`](#runsettings-perfiles-de-test)) porque cada uno resuelve una necesidad distinta — no es redundancia, cada combinación de filtro/cobertura/paralelismo tiene un costo de tiempo distinto:

| Perfil        | Tiempo aprox. | Qué corre                           | Cobertura                             | Paralelismo                                            | Cuándo usarlo                                                                    |
| ------------- | ------------- | ----------------------------------- | ------------------------------------- | ------------------------------------------------------ | -------------------------------------------------------------------------------- |
| `normal`      | 2-3 min       | Todo excepto `Category=Performance` | ❌ No                                 | Máximo (todos los cores)                               | Desarrollo diario — feedback más rápido mientras programas.                      |
| `coverage`    | 3-4 min       | Todo excepto `Category=Performance` | ✅ Sí (json/lcov/opencover/cobertura) | Moderado (2 hilos)                                     | Validar cobertura sin pagar el costo de las pruebas de performance.              |
| `performance` | 6-8 min       | Solo `Category=Performance`         | ❌ No                                 | Ninguno (1 hilo, para mediciones estables/comparables) | Benchmark y detectar regresiones de rendimiento.                                 |
| `ci-full`     | 10-12 min     | Todo (normales + performance)       | ✅ Sí (todos los formatos)            | Máximo                                                 | Validación final antes de merge/release — el que se corre antes de cerrar un PR. |

`run-tests-with-report.ps1` acepta estas banderas:

| Bandera                | Tipo                                                   | Default                  | Qué hace                                                                                                                                 |
| ---------------------- | ------------------------------------------------------ | ------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `-RunsettingsType`     | string                                                 | `"ci-full"`              | Cuál de los 4 perfiles de la tabla de arriba usar.                                                                                       |
| `-OpenReportInBrowser` | switch                                                 | apagado                  | Abre el reporte HTML generado apenas termina la corrida — sin esta bandera, el reporte se genera igual, solo no se abre solo.            |
| `-Browser`             | string                                                 | navegador default del SO | Ejecutable/nombre del navegador a usar en vez del default (ej. `"msedge"`) — solo tiene efecto junto con `-OpenReportInBrowser`.         |
| `-NoBuild`             | switch                                                 | apagado                  | Salta la recompilación previa (usa los binarios ya compilados).                                                                          |
| `-Verbosity`           | `quiet`\|`minimal`\|`normal`\|`detailed`\|`diagnostic` | `"normal"`               | Nivel de detalle del output de `dotnet test` (`detailed` incluye el output de `ITestOutputHelper`, útil para depurar un test que falla). |

Validación completa, con el reporte abierto al terminar:

```powershell
.\run-tests-with-report.ps1 -RunsettingsType "ci-full" -OpenReportInBrowser
```

Reintentar el reporte sin recompilar, viendo el detalle de cada test:

```powershell
.\run-tests-with-report.ps1 -RunsettingsType "normal" -NoBuild -Verbosity "detailed"
```

Los 3 scripts de test viven en la raíz con el mismo prefijo (`run-tests-*.ps1`) para encontrarse juntos en el explorador de archivos: `run-tests-with-report.ps1` (el de arriba), y `run-tests-coverage-run.ps1`/`run-tests-coverage-clean.ps1` (generación/limpieza de reportes de cobertura más acotada, ver [Configuración de coverage](#configuración-de-coverage)).

---

### Configuración de coverage

[< Configuración y pruebas](#configuración-y-pruebas)

---

La configuración de coverage vive organizada así:

```text
runsettings/
├── coverage.runsettings          # Configuración para desarrollo
├── ci-full.runsettings          # Configuración para CI
├── normal.runsettings           # Tests normales sin coverage
├── performance.runsettings      # Tests de performance
└── runsettings/
    └── coverage/                # Directorio de salida organizado
        ├── raw/                 # Datos raw de coverlet (GUID dirs)
        └── html/               # Reportes HTML generados
```

### Scripts de automatización

`run-tests-coverage-run.ps1` ejecuta tests con coverage y genera reportes HTML automáticamente:

```powershell
.\run-tests-coverage-run.ps1
```

`run-tests-coverage-clean.ps1` limpia directorios GUID y reportes anteriores:

```powershell
.\run-tests-coverage-clean.ps1
```

### Comandos manuales

Ejecutar tests con coverage:

```bash
dotnet test --settings runsettings/coverage.runsettings --verbosity minimal
```

Generar el reporte HTML a partir de los resultados:

```bash
reportgenerator -reports:"runsettings\runsettings\coverage\raw\*\coverage.cobertura.xml" -targetdir:"runsettings\runsettings\coverage\html" -reporttypes:"Html;HtmlSummary;Badges" -sourcedirs:"." -verbosity:Info
```

### Configuración de `coverage.runsettings`

```xml
<RunConfiguration>
  <ResultsDirectory>runsettings\runsettings\coverage</ResultsDirectory>
  <OutputDirectory>runsettings\runsettings\coverage</OutputDirectory>
</RunConfiguration>

<DataCollectionRunSettings>
  <DataCollectors>
    <DataCollector friendlyName="XPlat code coverage">
      <Configuration>
        <Format>json,cobertura,lcov,teamcity,opencover</Format>
        <Exclude>[*Tests]*,[*]Program</Exclude>
      </Configuration>
    </DataCollector>
  </DataCollectors>
</DataCollectionRunSettings>
```

### Directorios GUID

Coverlet genera automáticamente directorios con nombres GUID para evitar conflictos entre ejecuciones concurrentes de tests. Los scripts de arriba incluyen limpieza automática de directorios GUID antiguos para mantener el workspace organizado. Cada directorio GUID contiene:

- `coverage.cobertura.xml` — para ReportGenerator
- `coverage.json` — formato JSON
- `coverage.info` — formato LCOV
- `coverage.opencover.xml` — formato OpenCover

### Buenas prácticas

1. Preferir los scripts (`run-tests-coverage-run.ps1`/`run-tests-coverage-clean.ps1`) sobre los comandos manuales.
2. Limpiar regularmente con `run-tests-coverage-clean.ps1`.
3. Usar `ci-full.runsettings` en pipelines de CI.
4. Usar `coverage.runsettings` en desarrollo local.

### Archivos de configuración

- `coverage.runsettings` — desarrollo con coverage completo.
- `ci-full.runsettings` — CI/CD con coverage y configuración robusta.
- `normal.runsettings` — tests rápidos sin coverage.
- `performance.runsettings` — tests de performance con timeouts ajustados.

### Timeouts de las pruebas de performance

[< Configuración y pruebas](#configuración-y-pruebas)

---

#### 🎯 Filosofía de los timeouts

1. **Realismo de hardware**: pensados para equipos de desarrollo con capacidades medias, no servidores dedicados.
2. **Margen de seguridad**: absorben variaciones normales del sistema operativo y, sobre todo, la contención de CPU/memoria que agrega `ci-full` (cobertura instrumentada + toda la suite corriendo en paralelo).
3. **Estabilidad**: evitar fallos esporádicos por timing en vez de por un problema real de performance.
4. **Utilidad real**: los márgenes son generosos, pero siguen detectando una regresión grave (ej. si el hashing se volviera 10x más lento por un bug real).

#### 🔬 Parámetros de Argon2id (`Common/Security/PasswordHasher.cs`)

`Argon2.Hash(password)` de `Isopoh.Cryptography.Argon2` usa, por defecto:

- `m=65536` (64 MiB de memoria)
- `t=3` (3 iteraciones)
- `p=1` (1 hilo de paralelismo)

Argon2id es *memory-hard* a propósito (reserva memoria además de CPU, para dificultar ataques por GPU) — cuesta más por operación que un hash puramente basado en CPU, y ese costo es mayor todavía bajo cobertura de código instrumentada + ejecución paralela de toda la suite (perfil `ci-full`). `VerifyPassword` tiene un costo equivalente a `HashPassword`, ya que Argon2 recalcula el hash con los parámetros codificados en el string para comparar.

#### 📊 Umbrales actuales

| Prueba | Umbral | Qué mide |
| --- | --- | --- |
| `PasswordHasherTests.HashPassword_Performance_Should_Be_Reasonable` | 45,000ms por operación | Un solo `HashPassword`. |
| `LoginPerformanceTests.PasswordHasher_Argon2idPerformance_MaintainsAcceptableSpeed` | 20,000ms por operación | 20 iteraciones de `HashPassword` + `VerifyPassword`. |
| `LoginPerformanceTests` (caso individual de login) | 15,000ms | Un login completo (hash/verify + lógica de negocio). |
| `LoginPerformanceTests` (promedio por caso) | 1,200ms | Promedio sobre el conjunto de casos de login. |
| `LoginPerformanceTests` (concurrencia) | 120,000ms (2 min) | N requests de login concurrentes. |
| `LoginMassiveDataTests` (lote masivo) | 1,200,000ms (20 min) | 1000+ operaciones de login secuenciales. |
| `LoginMassiveDataTests` (promedio por caso) | 1,200ms | Promedio sobre el lote masivo. |
| `LoginEdgeCasesTests` (prevención de timing attacks) | máx(1,500ms, promedio × 1.5) | Desviación máxima aceptable entre intentos válidos/inválidos. |

Todas estas pruebas están etiquetadas `[Trait("Category", "Performance")]` — quedan excluidas del perfil `normal` (el de uso diario) y solo corren bajo `performance`/`ci-full`.

#### 🔬 Factores que afectan el timing en desarrollo

- **JIT warming**: la primera ejecución de un proceso es más lenta que las siguientes.
- **Garbage Collection**: pausas de Gen2 no siempre predecibles.
- **Antivirus/background tasks**: escaneo en tiempo real, indexado, actualizaciones.
- **Throttling térmico**: común en laptops bajo carga sostenida.
- **Presión de memoria**: Argon2id reserva ~64 MiB por operación; otros procesos compitiendo por memoria afectan el tiempo medido.
- **`ci-full` específicamente**: cobertura de código instrumentada + toda la suite (funcional + performance) corriendo en paralelo es, por diseño, el escenario de mayor contención — de ahí que los márgenes de estas pruebas sean sensiblemente mayores que el costo real de una sola operación en aislamiento.

#### 🎯 Desarrollo vs. producción

| Escenario | Umbral en desarrollo | Esperado en producción | Ratio |
| --- | --- | --- | --- |
| Login (1 operación Argon2id) | 15,000ms | 800-1,200ms | ~12-18x |
| Lote de 1000 logins | 1,200,000ms | 300,000ms | 4x |
| Requests concurrentes | 120,000ms | 30,000-40,000ms | ~3-4x |

Un servidor de producción típico (CPU/RAM dedicados, sin overhead de desarrollo, sin cobertura instrumentada) opera muy por debajo de estos márgenes — son deliberadamente generosos para absorber el peor caso de `ci-full` en hardware de desarrollo modesto, no una expectativa realista de producción.

#### 🔧 Monitoreo recomendado en producción

- APM con percentiles P95/P99 en el endpoint de login.
- Alerta si P95 > 1,500ms (Argon2id agrega una latencia base mayor que un hash puramente por CPU).
- Vigilar consumo de memoria bajo carga concurrente alta de login (Argon2id es memory-hard).

#### 📚 Referencias

- [Argon2 RFC 9106 — recomendaciones de parámetros](https://www.rfc-editor.org/rfc/rfc9106.html)
- [Isopoh.Cryptography.Argon2 (paquete usado en este proyecto)](https://www.nuget.org/packages/Isopoh.Cryptography.Argon2)

### Stack técnico

[< Configuración y pruebas](#configuración-y-pruebas)

---

- **.NET 10 (LTS)** minimal APIs, **Entity Framework Core** (SQL Server — la base de datos vive en su propia infraestructura, fuera del `docker-compose` de este repo, a propósito).
- **`Microsoft.AspNetCore.OpenApi`** (nativo) para la generación del documento OpenAPI; **Swashbuckle.AspNetCore.SwaggerUI** y **Scalar.AspNetCore** como renderizadores intercambiables, sin dependencias propias entre sí.
- **xUnit / NSubstitute / AwesomeAssertions** para pruebas; EF Core InMemory para tests de integración por defecto (no requiere infraestructura real para correr la suite completa).
- **Docker**, reverse proxy/TLS resuelto por un stack Nginx compartido externo ([`nginx-server-docker`](https://github.com/frugocorp/nginx-server-docker), ver [Docker](#arquitectura-docker)), no incluido en este repo.

## Desarrollo

[< Índice Referencias](#índice-referencias)

---

### Credenciales de desarrollo por defecto

[< Desarrollo](#desarrollo)

---

El seeder (`UserSeeder.cs`) crea este usuario admin con el rol `AdminAutoDiscoveryIAM` **solo si la app lo pide con `SeedAutoDiscoveryIAMDefaultDataAsync(includeDevelopmentUsers: true)`, como hace `samples/AutoDiscoveryIAM.SampleHost`, y `ASPNETCORE_ENVIRONMENT` es `Development` o `Testing`** — en cualquier otro ambiente (incluida Producción) no se crea, ver [Primer usuario con privilegios](https://autodiscoveryiam.frugocorp.com/docs/security-rbac/authorization#primer-usuario-con-privilegios).

- **Usuario:** `admin` · **Email:** `admin@example.com` · **Contraseña:** `AdminPass123!`

> ⚠️ Solo para desarrollo/testing local.

### Troubleshooting

[< Desarrollo](#desarrollo)

---

**`dotnet ef` responde "no se pudo ejecutar porque no se especificó el comando o archivo":**

Falta la herramienta global `dotnet-ef` (no viene con el SDK de .NET, se instala aparte una sola vez por máquina):

```bash
dotnet tool install --global dotnet-ef
```

**El usuario admin no existe tras `dotnet ef database update`:**

1. Confirmar el ambiente activo:

   ````code-block
   ```bash
   echo $ASPNETCORE_ENVIRONMENT
   ```
   ````

   Debe ser `Development` o `Testing` — en cualquier otro valor el seeder omite este usuario a propósito.

2. Confirmar que el seed corrió al arrancar la app (se ejecuta en cada arranque, no en la migración):

   ````code-block
   ```bash
   dotnet run --project samples/AutoDiscoveryIAM.SampleHost
   ```
   ````

3. Verificar en la base de datos:

   ````code-block
   ```sql
   SELECT * FROM Users WHERE Username = 'admin';
   SELECT * FROM Roles WHERE Name IN ('AdminAutoDiscoveryIAM', 'UserDefault');
   ```
   ````

---

### RunSettings (perfiles de test)

[< Desarrollo](#desarrollo)

---

#### 📁 Estructura

```text
runsettings/
├── normal.runsettings          # ⚡ Desarrollo diario (sin performance, sin coverage)
├── performance.runsettings     # 🏃 Solo pruebas de performance
├── security.runsettings        # 🔒 Solo pruebas de seguridad ofensiva
├── coverage.runsettings        # 📊 Coverage detallado (sin performance)
├── ci-full.runsettings         # 🚀 CI/CD completo (todo + coverage)
├── appsettings.template.json   # 📧 Plantilla del aviso por correo (sin secretos, versionado)
├── appsettings.json            # 📧 Config real del aviso por correo (gitignored, opcional)
├── README.md                   # 📖 Esta documentación
└── coverage/                   # 📁 Directorio de salida para reportes
```

---

#### 🚀 Comandos de Uso

##### Desarrollo Diario (Más Rápido)

Ejecución rápida sin coverage ni performance:

```powershell
dotnet test --settings runsettings/normal.runsettings
```

##### Coverage Analysis

Coverage detallado sin pruebas de performance:

```powershell
dotnet test --settings runsettings/coverage.runsettings
```

##### Pruebas de Performance

Solo pruebas de performance:

```powershell
dotnet test --settings runsettings/performance.runsettings
```

##### Pruebas de Seguridad

Solo pruebas de seguridad ofensiva (JWT tampering, IDOR, 2FA, CORS, etc.):

```powershell
dotnet test --settings runsettings/security.runsettings
```

Recomendado: usar el wrapper, que además corre el escaneo de dependencias vulnerables:

```powershell
.\run-tests-with-report.ps1 -RunsettingsType "security"
```

##### CI/CD Completo

Todas las pruebas + coverage completo (incluye seguridad, sin escaneo de dependencias):

```powershell
dotnet test --settings runsettings/ci-full.runsettings
```

---

#### ⚡ Scripts Recomendados

Agrega estos alias a tu perfil de PowerShell (`$PROFILE`):

```powershell
# Funciones de testing
function tn { dotnet test --settings runsettings/normal.runsettings }      # Test Normal
function tc { dotnet test --settings runsettings/coverage.runsettings }   # Test Coverage
function tp { dotnet test --settings runsettings/performance.runsettings } # Test Performance
function ts { dotnet test --settings runsettings/security.runsettings }   # Test Security
function ta { dotnet test --settings runsettings/ci-full.runsettings }    # Test All
```

---

#### 📊 Configuraciones por Archivo

##### 🟢 normal.runsettings

- **Filtro**: `Category!=Performance`
- **Coverage**: ❌ No
- **Paralelismo**: ✅ Máximo
- **Timeout**: 10 minutos
- **Uso**: Desarrollo diario, TDD

##### 🔵 coverage.runsettings

- **Filtro**: `Category!=Performance`
- **Coverage**: ✅ Sí (múltiples formatos)
- **Paralelismo**: ✅ Moderado (2 cores)
- **Timeout**: 30 minutos
- **Uso**: Análisis de cobertura

##### 🟡 performance.runsettings

- **Filtro**: `Category=Performance`
- **Coverage**: ❌ No
- **Paralelismo**: ❌ No (1 core)
- **Timeout**: 60 minutos
- **Uso**: Benchmarks y análisis de rendimiento

##### 🔒 security.runsettings

- **Filtro**: `Category=Security`
- **Coverage**: ❌ No (perfil de comportamiento, no de métricas)
- **Paralelismo**: 🟡 Moderado (2 cores) — hay pruebas de rate limiting/2FA sensibles a timing
- **Timeout**: 30 minutos
- **Uso**: Validar la suite de seguridad ofensiva de forma aislada. Ejecutada vía `run-tests-with-report.ps1`, además corre `dotnet list package --vulnerable --include-transitive` sobre la solución y guarda el resultado en `vulnerable-packages.log` dentro de la carpeta del run.

##### 🔴 ci-full.runsettings

- **Filtro**: Sin filtros (todo)
- **Coverage**: ✅ Sí (todos los formatos)
- **Paralelismo**: ✅ Máximo
- **Timeout**: 120 minutos
- **Uso**: CI/CD, validación completa

---

#### 🎯 Categorías de Pruebas

Las pruebas se categorizan usando el atributo `[Trait]`:

```csharp
[Fact]
[Trait("Category", "Performance")]
public void Performance_Test_Example()
{
    // Prueba de performance
}

[Fact] // Sin categoría = prueba normal
public void Normal_Test_Example()
{
    // Prueba normal
}
```

---

#### 📈 Reportes de Coverage

Los reportes se generan en:

- **Directorio**: `runsettings/coverage/`
- **Formatos**: Cobertura XML, JSON, LCOV, OpenCover
- **HTML**: Generar con ReportGenerator

##### Generar Reporte HTML

Después de ejecutar coverage:

```powershell
reportgenerator -reports:"runsettings/coverage/**/*.xml" -targetdir:"runsettings/coverage/html-report" -reporttypes:Html
```

---

#### 📧 Aviso por correo al terminar (opcional)

`run-tests-with-report.ps1` puede avisar por correo al terminar **cualquier** `-RunsettingsType` (normal, coverage, performance, security, ci-full) — útil para `ci-full`/`performance`, que tardan varios minutos.

**Activación**: copia `appsettings.template.json` como `appsettings.json` (en esta misma carpeta) y completa tus datos reales de SMTP (`Mail:Smtp`), remitente (`Mail:From`) y a quién debe llegar el aviso (`Mail:ToDev:Email`/`Name`). `appsettings.json` está en `.gitignore` — nunca se commitea, cada desarrollador configura el suyo. **Además del archivo, cada corrida necesita el flag `-WithEmailReport`** — el archivo por sí solo no envía nada, para no spamear el dev loop diario sin querer:

```powershell
.\run-tests-with-report.ps1 -RunsettingsType "ci-full" -WithEmailReport
```

El correo incluye el mismo resumen que se imprime en consola: resultados Unit/Integration (Superado/Omitido/Con error/Total/Duración), cobertura (si el perfil la genera) y los tramos de tiempo del wrapper, más el exit code y las rutas al reporte HTML y a la carpeta de resultados. **Se manda igual en éxito o en fallo** — el asunto y el cuerpo ya indican `EXITO`/`FALLO`, el envío nunca se condiciona al resultado de los tests. El envío es best-effort: si falla (SMTP mal configurado, sin red, etc.), se muestra una nota en consola y además queda registrado en `email-notification.log` (dentro de la carpeta de resultados de esa corrida) — nunca cambia el exit code de la corrida de tests.

`-WithoutAttachments` (combinable con `-WithEmailReport`) manda solo el resumen en texto, sin adjuntar el log/TRX/zip de cobertura — útil para diagnosticar si el relay SMTP está descartando el correo por tamaño/adjuntos:

```powershell
.\run-tests-with-report.ps1 -RunsettingsType "ci-full" -WithEmailReport -WithoutAttachments
```

Si los adjuntos superan 15MB en total, el script los omite automáticamente (igual manda el resumen, con una nota) en vez de arriesgarse a que el relay rechace el correo completo.

---

#### 🛠️ Troubleshooting

##### Error: "No tests were found"

- Verifica que las pruebas tengan las categorías correctas
- Comprueba que el filtro coincida con tus pruebas

##### Coverage vacío

- Asegúrate de que `coverlet.collector` esté instalado
- Verifica que los patrones Include/Exclude sean correctos

##### Performance lenta

- Usa `normal.runsettings` para desarrollo diario
- Reserva `ci-full.runsettings` para validación completa

---

#### 🔧 Configuración Personalizada

Para crear configuraciones personalizadas, copia uno de los archivos existentes y modifica:

- `TestCaseFilter`: Para filtrar pruebas específicas
- `MaxCpuCount`: Para controlar paralelismo
- `Include/Exclude`: Para ajustar coverage
- `TestSessionTimeout`: Para ajustar timeouts

---

#### 📚 Referencias

- [Configurar ejecución de pruebas | Microsoft Docs](https://docs.microsoft.com/es-es/dotnet/core/testing/configure-run-tests)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [xUnit Configuration](https://xunit.net/docs/configuration-files)

### Sitio de documentación (Angular)

[< Desarrollo](#desarrollo)

---

El sitio de documentación (`docs/site/AutoDiscoveryIAM`) es una app Angular con SSR, servida en desarrollo por [`docker/app/web/`](#docker-appweb). Su estructura interna sigue Feature-Driven Development (FDD) combinado con Bounded Contexts (DDD) — pensada para escalar a cualquier tamaño de sitio sin que `src/app/` se vuelva una carpeta plana ingobernable:

```
src/app/
├── core/                            # Singletons globales de toda la app (Auth, interceptores HTTP,
│                                     # guards de sesión, config global) — una sola instancia, se
│                                     # importa una vez en el arranque, nunca por feature.
├── shared/                          # UI Kit global (botones, modales base, inputs, pipes/directivas
│                                     # genéricas) — reutilizable por cualquier bounded context, sin
│                                     # lógica de negocio de ninguno en particular.
│
└── bounded-contexts/                # Término de Eric Evans (DDD) — cada subcarpeta es un límite de
    │                                 # dominio con su propio lenguaje/modelo, independiente de los demás.
    │
    ├── <bounded-context-a>/         # ─── BOUNDED CONTEXT: <nombre del dominio> ───
    │   ├── core/                    # Interceptores/servicios/guards exclusivos de este contexto
    │   ├── shared/                  # Componentes visuales exclusivos de este contexto (no globales)
    │   └── features/                # Slices funcionales (Feature-Driven Development) de este contexto
    │       ├── <feature-slice-1>/   # Slice: una pieza de funcionalidad cohesiva y autocontenida
    │       └── <feature-slice-2>/   # Slice: otra pieza de funcionalidad, independiente de la anterior
    │
    ├── <bounded-context-b>/         # ─── BOUNDED CONTEXT: <otro dominio> ───
    │   ├── core/
    │   ├── shared/
    │   └── features/
    │       └── <feature-slice-1>/
    │
    └── <bounded-context-n>/         # ... tantos bounded contexts como dominios reales tenga el sitio
```

**Regla de anidamiento** (el mismo patrón se repite en cada nivel, a propósito): `core/` es lo transversal/singleton **de ese nivel** — global si está en la raíz de `app/`, exclusivo del bounded context si está dentro de uno; `shared/` es UI reutilizable **de ese nivel**, sin lógica de negocio de ningún slice en particular; `features/` es donde vive el trabajo real, un slice por caso de uso — mismo espíritu que `Features/<Entity>/Commands|Queries/<Slice>/` en el backend (Vertical Slice, ver [Ejemplo: arquitectura Vertical Slice en tu propia app](#vertical-slice-en-tu-propia-app)), aplicado del lado de Angular. Vale la pena mantener los ejemplos de la estructura (`<bounded-context-a>`, `<feature-slice-1>`, etc.) aunque sean genéricos — sin un ejemplo concreto de cuántos niveles anidar y dónde, la regla es fácil de mal-aplicar (ej. meter un componente de un solo feature dentro de `shared/` del bounded context, o un servicio de negocio dentro del `shared/` global).

**Cuándo crear un bounded context nuevo vs. un feature slice nuevo dentro de uno existente**: un bounded context nuevo es cuando el dominio tiene su propio lenguaje/modelo que no comparte con los demás (ej. "Facturación" y "Catálogo de productos" casi nunca coinciden en qué significa "item" — uno lo ve como una línea de cobro, el otro como un producto con inventario); un feature slice nuevo es cuando ya estás dentro del mismo dominio, solo agregando otro caso de uso al mismo lenguaje/modelo. La pregunta práctica: si dos partes del sitio necesitaran modelos de datos incompatibles para lo que a simple vista "suena igual", son bounded contexts distintos, no dos features del mismo.
<!-- autodiscoveryiam:doc_body_end -->
<!-- autodiscoveryiam:doc_end -->
