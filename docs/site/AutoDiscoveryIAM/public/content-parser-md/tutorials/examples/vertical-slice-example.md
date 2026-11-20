---
[slug]: vertical-slice-example
[title]: Vertical Slice en tu propia app
[description]: Cómo construir una feature con Vertical Slice Architecture en tu propia aplicación.
[keywords]: Vertical Slice Architecture, minimal APIs, ejemplo, feature
---

# Ejemplo: arquitectura Vertical Slice en tu propia app

La librería está construida internamente con **Vertical Slice Architecture** (ver [Qué incluye hoy](/docs/fundamentals/whats-included)) — un Command/Query por carpeta, con su propio `Handler`, `Repository`, `DI` y `EndPoint`, sin capa MVC/Controllers y sin MediatR. Nada te obliga a seguir el mismo patrón en tu propia app (un `DbContext` + minimal APIs normales funcionan igual de bien), pero si tu equipo no tiene ya una arquitectura definida, te damos un ejemplo completo y funcional para que la adoptes si te sirve — queda naturalmente compatible con el resto de la librería, y es la misma arquitectura que ya viste en [Reutilizar auditoría y soft-delete en tus propias entidades](/docs/security-rbac/audit-soft-delete).

El ejemplo usa la entidad `UserProfile` y el `ApplicationDbContext` de esa misma sección (contexto único, hereda `AutoDiscoveryIAMDbContext`): un Command (`CreateUserProfile`) y una Query (`GetUserProfiles`), con la misma estructura de archivos por carpeta que usa la librería (ver `src/AutoDiscoveryIAM/Features/Roles/Commands/CreateRole/` en este repo como referencia 1:1).

Lo que sigue explica **por qué** existe cada archivo por separado, simplificado a un solo campo (`Occupation`) para no distraer del punto de cada archivo — en la práctica tu API recibiría el `PublicId` del usuario, no su `Id` interno, y resolvería uno a partir del otro (ver el ejemplo completo para esa resolución). Si prefieres los archivos completos y listos para copiar (incluidas las 3 piezas opcionales — Validator, OpenAPI, Logging — que aquí solo se explican, ver [Extras opcionales: Validator, OpenAPI y Logging](/tutorials/getting-started/installation#extras-opcionales-validator-openapi-logging-y-diagnostico) — y la resolución `PublicId` → `Id` mencionada arriba), están en [`samples/SimpleCode/Features/UserProfiles/`](https://github.com/frugocorp/AutoDiscoveryIAM/tree/master/samples/SimpleCode/Features/UserProfiles) — no es un proyecto que compile como parte de este repo, es solo material de referencia para copiar y pegar en el tuyo.

## Command

`CreateUserProfile`, dividido en 8 archivos — cada uno con una sola responsabilidad, para poder cambiar una pieza (ej. la forma del JSON de entrada) sin tocar las demás (ej. la lógica de negocio). Un solo archivo con todo el código junto capturaría el request y regresaría el response igual de "funcional", pero mezclaría HTTP, validación, reglas de negocio y acceso a datos en un mismo lugar — difícil de probar en aislado y de mantener según crece.

### Contrato del Command

Por qué es su propio archivo: el registro de entrada — solo datos, sin lógica ni nada de HTTP. El Handler recibe esto, no el DTO de HTTP directamente, para que la lógica de negocio no dependa de la forma exacta del JSON que llega por la red.

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileCommand.cs`

````code-block
```csharp
namespace (--PROYECTO:pascal--).Features.UserProfiles.Commands.CreateUserProfile;

public record CreateUserProfileCommand(long UserId, string Occupation);
```
````

### Repository

Por qué es su propio archivo: es el único lugar que toca el `DbContext` directamente para este slice. Aísla el acceso a datos del Handler — así el Handler se puede probar con un mock de `ICreateUserProfileRepository`, sin necesitar una base de datos real.

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileRepository.cs` (interfaz + implementación en el mismo archivo — igual que hace la librería)

````code-block
```csharp
namespace (--PROYECTO:pascal--).Features.UserProfiles.Commands.CreateUserProfile;

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
````

### Handler

Por qué es su propio archivo: es la lógica de negocio del slice — valida reglas, arma la entidad, guarda, y regresa un `OperationResult`. Es lo único que una prueba unitaria necesita ejercitar directamente, sin pasar por HTTP ni por una base de datos real (con un mock del Repository).

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileHandler.cs`

````code-block
```csharp
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;

namespace (--PROYECTO:pascal--).Features.UserProfiles.Commands.CreateUserProfile;

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
````

> `IAutoDiscoveryIAMUnitOfWork` es de la librería (`FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW`), no algo que tengas que construir tú — en modo contexto único (la opción usada en este ejemplo) `ApplicationDbContext` **es**, por herencia, un `AutoDiscoveryIAMDbContext`, así que guardar aquí persiste también `UserProfile`. Si en cambio usaras dos `DbContext` separados (ver [Elige tu propio DbContext: uno o dos](/docs/security-rbac/audit-soft-delete#elige-tu-propio-dbcontext-uno-o-dos)), aquí no aplicaría — llamarías `_context.SaveChangesAsync()` directo, o tu propio Unit of Work si construiste uno.

### DI

Por qué es su propio archivo: registra el `Repository` y el `Handler` de este slice en el contenedor de dependencias. Agregar un slice nuevo no significa tocar un archivo central compartido por todos los demás — cada slice trae y mantiene su propio registro.

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileDI.cs`

````code-block
```csharp
namespace (--PROYECTO:pascal--).Features.UserProfiles.Commands.CreateUserProfile;

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
````

### RequestDto

Por qué es su propio archivo: la forma exacta del JSON que el cliente envía por HTTP — separada del Command para que un cambio en el contrato HTTP (ej. renombrar un campo del request) no obligue a tocar la lógica interna del Handler.

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileRequestDto.cs`

````code-block
```csharp
namespace (--PROYECTO:pascal--).Features.UserProfiles.Commands.CreateUserProfile;

public record CreateUserProfileRequestDto(long UserId, string Occupation);
```
````

### ResponseDto

Por qué es su propio archivo: los datos que sí se exponen al cliente en la respuesta — nunca la entidad interna directamente, para controlar explícitamente qué campos salen (ej. nunca el `Id` interno de base de datos, solo el `PublicId`).

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileResponseDto.cs`

````code-block
```csharp
namespace (--PROYECTO:pascal--).Features.UserProfiles.Commands.CreateUserProfile;

public record CreateUserProfileResponseDto(Guid PublicId, long UserId, string Occupation);
```
````

### Response

Por qué es su propio archivo: el envoltorio de respuesta tipado específicamente para este slice — nunca se expone `ApiResponse<T>` genérico directo en un endpoint, cada slice tiene el suyo propio.

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileResponse.cs`

````code-block
```csharp
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace (--PROYECTO:pascal--).Features.UserProfiles.Commands.CreateUserProfile;

public record CreateUserProfileResponse : ApiResponse<CreateUserProfileResponseDto>
{
  public CreateUserProfileResponse(bool success, CreateUserProfileResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
    : base(success, data, error, errorInfo) { }
}
```
````

> **Por qué esta estructura vale la pena** (no es burocracia porque sí): toda respuesta de la API — éxito o error — tiene siempre la misma forma (`Success`, `Data`, `Error`, `ErrorInfo`). Un frontend puede revisar `response.success` sin adivinar qué campos trae cada endpoint distinto, y ante un error, leer `errorInfo.key`/`errorInfo.statusCode` de forma programática (para tomar una decisión en código) en vez de parsear un mensaje de texto libre que puede cambiar de redacción. `ErrorInfo` además centraliza el catálogo de errores conocidos (`Status400Validation`, `Status404NotFound`, etc.) en un solo lugar, reutilizable en todos tus slices, en vez de que cada endpoint invente su propio formato de error. `OperationResult<T>`/`OperationResult` (fábrica estática `Ok`/`Fail`), `ApiResponse<T>` y `ErrorInfo` son públicos en `FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common`/`Common.ApiResponse` — puedes ver la estructura real completa en [`OperationResult.cs`](https://github.com/frugocorp/AutoDiscoveryIAM/blob/master/src/AutoDiscoveryIAM/Common/OperationResult.cs), [`ApiResponse.cs`](https://github.com/frugocorp/AutoDiscoveryIAM/blob/master/src/AutoDiscoveryIAM/Common/ApiResponse/ApiResponse.cs) y [`ErrorInfo.cs`](https://github.com/frugocorp/AutoDiscoveryIAM/blob/master/src/AutoDiscoveryIAM/Common/ErrorInfo.cs) — y reutilizarlos tal cual en tu propia app, en vez de inventar tu propio envoltorio de respuesta.


### EndPoint — ejemplo de uso de Metadata

Por qué es su propio archivo: conecta HTTP con el Handler — recibe el request, arma el Command, llama al Handler, y traduce el `OperationResult` a un código de estado HTTP real. Es también donde se declara, vía `.WithMetadata(...)`, qué nivel de autorización necesita el endpoint.

Este ejemplo usa `.WithMetadata(new PermissionsNotRequiredAttribute("..."))` a propósito, para que el endpoint funcione tal cual sin necesitar sembrar un `Permission`/`Role` propio primero — **no es la recomendación por default**, ver [Métodos de permisos usados en este ejemplo](/tutorials/examples/vertical-slice-example#metodos-de-permisos-usados-en-este-ejemplo) más abajo para las demás opciones (RBAC estándar, `[RequiresDevelopmentAccess]`, `[RequiresSuAdminAccess]`, etc.).

Ruta sugerida: `Features/UserProfiles/Commands/CreateUserProfile/CreateUserProfileEndPoint.cs`

````code-block
```csharp
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace (--PROYECTO:pascal--).Features.UserProfiles.Commands.CreateUserProfile;

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
````

## Query

`GetUserProfiles`, con la misma división de responsabilidades que `CreateUserProfile` — sin `RequestDto` (una consulta con parámetros simples de query string no necesita un DTO de body).

### Contrato del Query

Por qué es su propio archivo: el registro de entrada de la consulta — parámetros de paginación, sin lógica. Igual que el Command, mantiene al Handler independiente de cómo llegan esos parámetros por HTTP.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesQuery.cs`

````code-block
```csharp
namespace (--PROYECTO:pascal--).Features.UserProfiles.Queries.GetUserProfiles;

public record GetUserProfilesQuery(int PageNumber, int PageSize);
```
````

### Repository

Por qué es su propio archivo: mismo motivo que en el Command — único lugar que toca el `DbContext`, para poder probar el Handler con un mock. Aquí además es donde se ve el beneficio concreto del contexto único: `.Include(p => p.User)` es un join nativo de EF Core, posible porque `UserProfile` y `User` (de la librería) viven en el mismo modelo.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesRepository.cs`

````code-block
```csharp
using Microsoft.EntityFrameworkCore;

namespace (--PROYECTO:pascal--).Features.UserProfiles.Queries.GetUserProfiles;

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
````

### Handler

Por qué es su propio archivo: arma la lista paginada de resultados a partir de lo que devuelve el Repository. A diferencia del Command, aquí no hay `SaveChangesAsync` — una consulta no modifica datos.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesHandler.cs`

````code-block
```csharp
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

namespace (--PROYECTO:pascal--).Features.UserProfiles.Queries.GetUserProfiles;

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
````

> `PagedResultDto<T>` (de `FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination`) es el mismo envoltorio de paginación que usan las Queries de listado de la librería (ej. `GetRoles`) — también reutilizable en tu propia app. Nota también `p.User.Username` en el `Select` — viene del `Include(p => p.User)` del Repository, sin una segunda consulta ni un `DbContext` adicional.

### DI

Por qué es su propio archivo: mismo motivo que en el Command — registro del slice aislado del resto.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesDI.cs`

````code-block
```csharp
namespace (--PROYECTO:pascal--).Features.UserProfiles.Queries.GetUserProfiles;

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
````

### ResponseDto

Por qué es su propio archivo: mismo motivo que en el Command — controla explícitamente qué campos de `UserProfile` (y del `User` relacionado) salen en la respuesta.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesResponseDto.cs`

````code-block
```csharp
namespace (--PROYECTO:pascal--).Features.UserProfiles.Queries.GetUserProfiles;

public record GetUserProfilesResponseDto(Guid PublicId, string Username, string Occupation, DateTime CreatedAt);
```
````

### Response

Por qué es su propio archivo: mismo motivo que en el Command — envoltorio tipado propio de este slice, esta vez envolviendo un `PagedResultDto<T>` en vez de un DTO simple.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesResponse.cs`

````code-block
```csharp
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

namespace (--PROYECTO:pascal--).Features.UserProfiles.Queries.GetUserProfiles;

public record GetUserProfilesResponse : ApiResponse<PagedResultDto<GetUserProfilesResponseDto>>
{
  public GetUserProfilesResponse(bool success, PagedResultDto<GetUserProfilesResponseDto>? data, string? error = null, ErrorInfo? errorInfo = null)
    : base(success, data, error, errorInfo) { }
}
```
````


### EndPoint — ejemplo de uso de Metadata

Por qué es su propio archivo: mismo motivo que en el Command — conecta HTTP con el Handler y declara el nivel de autorización del endpoint.

Igual que en `CreateUserProfile`, usa `.WithMetadata(new PermissionsNotRequiredAttribute("..."))` a propósito para que el ejemplo funcione sin sembrar permisos primero — ver [Métodos de permisos usados en este ejemplo](/tutorials/examples/vertical-slice-example#metodos-de-permisos-usados-en-este-ejemplo) más abajo para las demás opciones.

Ruta sugerida: `Features/UserProfiles/Queries/GetUserProfiles/GetUserProfilesEndPoint.cs`

````code-block
```csharp
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace (--PROYECTO:pascal--).Features.UserProfiles.Queries.GetUserProfiles;

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
````

## Métodos de permisos usados en este ejemplo

Los dos endpoints de este ejemplo usan `[PermissionsNotRequired]` a propósito, para que el ejemplo funcione tal cual sin necesitar sembrar un `Permission`/`Role` propio primero. **Esa no es la única opción, ni la recomendada por default** — ver la tabla completa de atributos disponibles (`[RequiresDevelopmentAccess]`, `[RequiresSuAdminAccess]`, `[RequirePermissions]`, o sin atributo para RBAC estándar) en [Autorización con niveles de acceso](/docs/security-rbac/authorization). En tu app real, lo normal es dejar `CreateUserProfile`/`GetUserProfiles` **sin ningún atributo** (RBAC estándar, secure-by-default), sincronizar los permisos con `POST /permissions/sync` para que se creen `userprofiles.create_user_profile`/`userprofiles.get_user_profiles` (la feature `UserProfiles` solo se pasa a minúsculas, la acción a snake_case), y asignarlos al rol que corresponda — ver [Permisos para tus propios endpoints](/docs/security-rbac/authorization#permisos-para-tus-propios-endpoints).

Por último, registra la cadena completa — igual que hace la librería internamente (`AddAutoDiscoveryIAM()` → `AddFeaturesDI()` → `AddRolesDI()` → `AddCreateRoleDI()`, y lo mismo para el mapeo de endpoints):

````code-block
```csharp
// Cada slice registra lo suyo (ver CreateUserProfileDI.cs/GetUserProfilesDI.cs):
builder.Services.AddCreateUserProfileDI().AddGetUserProfilesDI();

// La feature completa registra todos sus slices en un solo lugar (ver UserProfilesDI.cs):
builder.Services.AddUserProfilesDI();
```
````

`AddCreateUserProfileDI()`/`AddGetUserProfilesDI()`/`AddUserProfilesDI()` van dentro de tu propio `Features/FeaturesDependencyInjection.cs` (el agregador, no `Program.cs` directo) — el equivalente para el mapeo de endpoints va en tu `Features/FeaturesEndpointMapping.cs`. `Program.cs` en sí **no cambia por cada feature que agregues**: solo necesita llamar `builder.Services.AddFeaturesDI()` y `app.MapAllFeatureEndpoints()` **una sola vez**, ya sea que tengas una feature o veinte — esas dos líneas ya están en el `Program.cs` completo de [Configura Program.cs](/tutorials/getting-started/installation#configura-programcs) más arriba (marcadas ahí como opcionales, por si todavía no tienes ninguna feature propia). Ver el ejemplo completo en [`samples/SimpleCode/Features/`](https://github.com/frugocorp/AutoDiscoveryIAM/tree/master/samples/SimpleCode/Features) para los 2 archivos concentradores ya armados.

---

<!-- ============================================================
     Fin de la macro-sección de librería. Lo que sigue es sobre este
     repositorio (cómo contribuir, correr el SampleHost, Docker,
     pruebas) -- no es información que un consumidor del paquete NuGet
     necesite para usar la librería en su propio proyecto.
     ============================================================ -->

---
