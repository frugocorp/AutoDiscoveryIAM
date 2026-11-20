---
[slug]: audit-soft-delete
[title]: Reutilizar auditoría y soft-delete
[description]: Cómo reutilizar la auditoría y el soft-delete de la librería en tus propias entidades.
[keywords]: auditoría, soft delete, AuditableEntity, Entity Framework Core
---

# Reutilizar auditoría y soft-delete en tus propias entidades

Las 8 entidades del package heredan `AuditableEntity` (`PublicId`, `CreatedAt`/`By`, `UpdatedAt`/`By`, `DeletedAt`/`By`, `RestoredAt`/`By`, `IsDeleted`), rellenados automáticamente en cada `SaveChanges`/`SaveChangesAsync`. Esa clase base y el mecanismo que la rellena (`AuditableEntitySaveChangesInterceptor`, un `SaveChangesInterceptor` nativo de EF Core) son públicos — tus propias entidades pueden heredar la misma clase y obtener el mismo comportamiento, sin reimplementar nada.

Para tus propias entidades de negocio necesitas tu propio `DbContext`, apuntando a la misma base de datos. Como la librería ya reclamó su propio nombre para el suyo (`AutoDiscoveryIAMDbContext`, ver [Configura Program.cs](/tutorials/getting-started/installation#configura-programcs)), el nombre genérico `ApplicationDbContext` queda libre para el tuyo. Hay dos formas válidas de construirlo — la librería no te obliga a ninguna, ver [Elige tu propio DbContext: uno o dos](/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos) para el detalle completo de ambas y su tradeoff. La plantilla de abajo usa la opción recomendada para empezar (un solo `DbContext`, heredando el de la librería):

````code-block
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
````

> Se propone (no obligatorio) el directorio `src/(--PROYECTO:pascal--)/Data/ApplicationDbContext.cs` para este archivo — el mismo patrón de carpeta `Data/` que usa la librería internamente.

- `ApplySoftDeleteQueryFilters()` aplica `WHERE !IsDeleted` a cualquier entidad de tu modelo que herede `AuditableEntity`, por convención — no hace falta escribir `HasQueryFilter` a mano por cada una. Como `ApplicationDbContext` ya trae el modelo de la librería (herencia), esta llamada también reaplica (sin cambiar nada) el mismo filtro a `User`/`Role`/etc. — inofensivo, es la misma condición que ya traían.
- No necesitas cablear el interceptor de auditoría a mano: `AutoDiscoveryIAMDbContext` ya lo hace en su propio `OnConfiguring`, usando el `ICurrentAuditUserService` que le llega por constructor (registrado en DI por `AddAutoDiscoveryIAM()`) — al heredar, `ApplicationDbContext` lo hereda también. `GetCurrentUser().Id` regresa `null` automáticamente si no hay usuario autenticado en esa petición (ej. un seed en background sin request HTTP).

Con `ApplicationDbContext` ya registrado, genera y aplica su migración igual que harías con cualquier `DbContext` propio (nota el `--context` explícito, para no confundirlo con el de la librería si tienes los dos registrados):

Genera la migración inicial de tu propio `DbContext`:

````code-block
```bash
dotnet ef migrations add InitialCreate --context ApplicationDbContext
```
````

Aplícala a la base de datos:

````code-block
```bash
dotnet ef database update --context ApplicationDbContext
```
````

Para ver cómo construir un endpoint completo (Command + Query) sobre esta entidad `UserProfile`, siguiendo la misma arquitectura Vertical Slice que usa la librería internamente, ver [Ejemplo: arquitectura Vertical Slice en tu propia app](/tutorials/examples/vertical-slice-example).

---

## Elige tu propio DbContext: uno o dos

`User`, `Role`, `Permission` y el resto de las entidades de la librería son **cerradas a modificación, abiertas a extensión** (Open/Closed Principle): nunca las heredas ni les agregas propiedades — ni aunque uses el modo de un solo `DbContext` de abajo, que sí las trae al mismo modelo. Si necesitas más datos por usuario ("estado civil", una foto, lo que sea), la extensión válida es una entidad tuya, propia, con un FK hacia `User.Id` — como `UserProfile` en la plantilla de arriba, o el ejemplo completo en [`samples/SimpleCode/Features/UserProfiles/`](https://github.com/frugocorp/AutoDiscoveryIAM/tree/master/samples/SimpleCode/Features/UserProfiles).

Dicho eso, cómo relacionas esa entidad tuya con las de la librería sí es una decisión tuya — la librería ayuda con ambos caminos, no te obliga a ninguno:

**Opción 1 — Un solo `DbContext`** (`ApplicationDbContext : AutoDiscoveryIAMDbContext`, la plantilla de arriba). Recomendada para empezar, sobre todo si tu equipo tiene poca experiencia con EF Core: tu modelo y el de la librería quedan en la misma base de datos y el mismo `ModelBuilder`, así que consultas cruzadas (`.Include(p => p.User)`, joins, navegación) funcionan de forma nativa, con un único `SaveChangesAsync` para ambos lados (reutiliza `IAutoDiscoveryIAMUnitOfWork` de la librería, no necesitas tu propio Unit of Work). El costo: tu esquema queda ligado al ciclo de vida del package — una actualización de `AutoDiscoveryIAM` que cambie su esquema interno puede obligarte a generar una migración nueva en tu proyecto aunque tú no hayas tocado nada. Requiere dos cosas que la plantilla de arriba ya incluye: que el constructor de `AutoDiscoveryIAMDbContext` reciba `DbContextOptions` sin el genérico (ya es así), y registrar el alias `services.AddScoped<AutoDiscoveryIAMDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>())` **antes** de `AddAutoDiscoveryIAM()` — sin ese alias, el resto de la librería (repositorios, health check) no puede resolver `AutoDiscoveryIAMDbContext` desde DI.

**Opción 2 — Dos `DbContext` separados** (`ApplicationDbContext : DbContext`, independiente). Mejor si tu equipo ya tiene experiencia con EF Core y prefiere desacoplar del todo el ciclo de vida/migraciones de tu esquema del de la librería. El costo: sin joins nativos entre tus entidades y las de la librería, porque EF Core no permite un LINQ join entre dos instancias de `DbContext` distintas.

````code-block
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
````

Con dos contextos separados, dos operaciones antes triviales necesitan un patrón explícito:

**Escribir en ambos a la vez, de forma atómica (todo o nada).** No es el flujo normal — la mayoría de las operaciones solo leen de un lado (ej. verificar permisos) y escriben del otro (ej. crear un perfil), y eso no necesita coordinación porque la lectura no modifica nada. Si de verdad necesitas que una sola operación falle o se aplique completa en ambos lados, comparte la misma transacción física entre los dos contextos (ambos apuntan a la misma base de datos):

````code-block
```csharp
await using var transaction = await autoDiscoveryIAMDbContext.Database.BeginTransactionAsync(cancellationToken);
applicationDbContext.Database.SetDbConnection(autoDiscoveryIAMDbContext.Database.GetDbConnection());
await applicationDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);

// ... cambios en ambos contextos ...
await autoDiscoveryIAMDbContext.SaveChangesAsync(cancellationToken);
await applicationDbContext.SaveChangesAsync(cancellationToken);

await transaction.CommitAsync(cancellationToken);
```
````

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
