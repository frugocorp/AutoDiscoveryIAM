using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using TuProyecto.Features.UserProfiles.Queries.GetUserProfiles.Docs;

namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

public static class GetUserProfilesEndPoint
{
  public static void MapGetUserProfilesEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapGet("/user-profiles", async (
        GetUserProfilesHandler handler,
        CancellationToken cancellationToken,
        int pageNumber = PaginationDefaults.DefaultPageNumber,
        int pageSize = PaginationDefaults.DefaultPageSize) =>
    {
      var query = new GetUserProfilesQuery(pageNumber, pageSize);
      var result = await handler.Handle(query, cancellationToken);
      var response = new GetUserProfilesResponse(result.Success, result.Data, result.Error, result.ErrorInfo);
      return Results.Ok(response);
    })
    .WithName("GetUserProfiles")
    .WithTags("UserProfiles")
    .WithSummary("Lista los perfiles de usuario, paginado, con el username/email del usuario dueño de cada perfil.")
    .Produces<GetUserProfilesResponse>(StatusCodes.Status200OK, "application/json")
    // [PermissionsNotRequired] es solo para que este ejemplo funcione sin sembrar un Permission/Role
    // propio primero -- ver el mismo comentario en CreateUserProfileEndPoint.cs y "Autorización con
    // niveles de acceso" en el README de AutoDiscoveryIAM.
    .WithMetadata(new PermissionsNotRequiredAttribute("Ejemplo ilustrativo -- quitar en una app real"))
    // Requiere OpenAPI configurado -- si no lo usas, quita esta línea y la carpeta Docs/ completa.
    .WithMetadata(new GetUserProfilesOperationDocs());
  }
}
