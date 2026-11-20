---
[slug]: userprofiles-example
[title]: Ejemplo copiable: UserProfiles
[description]: Una feature UserProfiles completa, lista para copiar a tu proyecto y adaptarla.
[keywords]: ejemplo copiable, UserProfiles, feature, Vertical Slice
---

# Ejemplo copiable: feature UserProfiles

Esta carpeta **no es un proyecto que compile por sí solo** — son archivos de referencia, listos para copiar dentro de tu propio proyecto y renombrar. No forma parte de la solución (`.sln`), no se compila, no se prueba, y no afecta a `samples/AutoDiscoveryIAM.SampleHost` ni a ningún otro proyecto de este repo.

Es la versión completa del ejemplo explicado paso a paso en [Ejemplo: arquitectura Vertical Slice en tu propia app](/tutorials/examples/vertical-slice-example) — ahí se explica el *por qué* de cada archivo; aquí está el *qué*, completo, incluyendo las piezas que esa página solo describe (Validator, OpenAPI, Logging, Catalogs, Services).

`UserProfile` no es una entidad cualquiera: tiene un FK real (`UserId`) y una navegación (`User`) hacia el `User` de la librería. Es la respuesta en código a "¿cómo agrego más datos por usuario (estado civil, ocupación, etc.) si `User` es cerrado a modificación?" — ver [Elige tu propio DbContext: uno o dos](/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos).

## Qué copiar y cómo

Nota importante de estructura: `Data/` vive como **hermana** de `Features/` en esta misma carpeta `samples/SimpleCode/` — no dentro de `Features/UserProfiles/`. `ApplicationDbContext` es transversal (lo va a usar cualquier feature que agregues después, no solo UserProfiles), así que nunca debe quedar anidado dentro de una sola feature — mismo patrón que usa la propia librería (`src/AutoDiscoveryIAM/Data/` es hermana de `src/AutoDiscoveryIAM/Features/`, no está dentro de `Features/Roles/`). Por la misma razón, `Features/FeaturesDependencyInjection.cs` y `Features/FeaturesEndpointMapping.cs` viven como hermanos de `Features/UserProfiles/`, no dentro — son el punto único de registro de **todas** tus features, no solo de UserProfiles.

1. Copia `Data/ApplicationDbContext.cs` a tu propio proyecto, como hermana de tu `Features/`.
2. Copia `Features/UserProfiles/` dentro de tu propio `Features/`.
3. Copia `Features/FeaturesDependencyInjection.cs` y `Features/FeaturesEndpointMapping.cs` a tu propio `Features/` (si ya tienes los tuyos porque instalaste otra feature antes, solo agrégales la línea de UserProfiles).
4. Cambia el namespace `(--PROYECTO:pascal--)` por el de tu propio proyecto (buscar y reemplazar).
5. Registra tu `ApplicationDbContext` (contexto único, hereda `AutoDiscoveryIAMDbContext`) en tu `Program.cs` **en vez de** registrar `AutoDiscoveryIAMDbContext` por separado -- ver [Elige tu propio DbContext: uno o dos](/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos) para el porqué de cada línea:

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

6. Genera y aplica la migración de tu `ApplicationDbContext` (mismos comandos `dotnet ef` de siempre, apuntando a `ApplicationDbContext` en vez de a `AutoDiscoveryIAMDbContext` -- ver [Aplica las migraciones](/tutorials/getting-started/installation#aplica-las-migraciones)).

## Qué es obligatorio y qué es opcional

Este ejemplo incluye todas las piezas completas a propósito — es más fácil borrar lo que no quieras que reconstruirlo desde un ejemplo simplificado.

- **Obligatorio** (sin esto el slice no compila): `../../Data/ApplicationDbContext.cs`, `Models/Entities/`, `Commands/CreateUserProfile/CreateUserProfileCommand.cs`, `CreateUserProfileRepository.cs`, `CreateUserProfileHandler.cs`, `CreateUserProfileDI.cs`, `CreateUserProfileRequestDto.cs`, `CreateUserProfileResponseDto.cs`, `CreateUserProfileResponse.cs`, `CreateUserProfileEndPoint.cs` (y sus equivalentes en `Queries/GetUserProfiles/`), `UserProfilesDI.cs`, `UserProfilesEndPointMapping.cs`, `../FeaturesDependencyInjection.cs`, `../FeaturesEndpointMapping.cs`.
- **Opcional — Validator + Catalogs** (`CreateUserProfileCommandValidator.cs`, `Infrastructure/Validation/`, `Models/Catalogs/MaritalStatusCatalog.cs`): requiere el paquete `FluentValidation`. El campo `MaritalStatus` se valida contra una lista cerrada de valores (`MaritalStatusCatalog.GetAll()`) en vez de aceptar cualquier texto — mismo patrón que usa la librería para sus propios catálogos cerrados (ver `Features/Users/Models/Catalogs/UserStatusCatalog.cs`). Si prefieres un campo de texto libre sin catálogo, borra `Infrastructure/Validation/`, `Models/Catalogs/`, quita la validación de `MaritalStatus` en el Validator, y simplifica el resto si no quieres validación en absoluto.
- **Opcional — OpenAPI** (carpetas `Docs/` en cada slice, y la línea `.WithMetadata(new ...OperationDocs())` en cada `EndPoint.cs`): requiere `AddOpenApi(...)` configurado en tu `Program.cs`. Si no vas a documentar la API con OpenAPI, borra las carpetas `Docs/` y esas líneas.
- **Opcional — Logging** (`Infrastructure/Logging/`, el parámetro `IUserProfileLogger logger` en los Handlers, y su registro en `UserProfilesDI.cs`): no requiere ningún paquete adicional (usa `Microsoft.Extensions.Logging`, ya incluido), pero puedes quitarlo igual si prefieres no tener logging por feature.
- **Opcional — Services/UserProfileQuery** (`Services/UserProfileQuery/`): consultas genéricas del dominio UserProfiles (buscar un perfil por `PublicId`, verificar si existe), reutilizables por cualquier slice futuro (`UpdateUserProfile`, `DeleteUserProfile`, etc.) sin reescribir la misma consulta en cada uno — mismo patrón que `Features/Roles/Services/RoleQuery/` de la librería. **Ninguno de los 2 slices de este ejemplo lo usa todavía** (`CreateUserProfile` resuelve el `UserId` directo en su propio Repository, y `GetUserProfiles` ya tiene su propia consulta paginada en el suyo) — está aquí como el lugar ya preparado para cuando agregues un slice que sí lo necesite. Si no planeas agregar más slices a UserProfiles, puedes borrarlo sin que nada se rompa.

## Por qué no hay un `Data/UoW/` propio en este ejemplo

A diferencia de un ejemplo con dos `DbContext` separados, aquí `CreateUserProfileHandler` inyecta y usa directamente `IAutoDiscoveryIAMUnitOfWork` (de la librería, `FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW`) — no necesitas construir tu propio Unit of Work. En modo contexto único, `ApplicationDbContext` **es** (por herencia) un `AutoDiscoveryIAMDbContext`, así que `IAutoDiscoveryIAMUnitOfWork.SaveChangesAsync()` guarda también tus propias entidades (`UserProfile`), porque es literalmente la misma instancia de `DbContext` detrás de ambos. Si en cambio usaras dos `DbContext` separados, sí necesitarías tu propio Unit of Work para el tuyo (ver [Elige tu propio DbContext: uno o dos](/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos)).
