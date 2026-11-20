using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId;

public static class GetPermissionsByPublicIdEndPoint
{
  public static void MapGetPermissionsByPublicIdEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapGet("/users/{userPublicId:guid}/permissions", async (
      Guid userPublicId,
      GetPermissionsByPublicIdHandler handler,
      CancellationToken cancellationToken) =>
    {
      var result = await handler.Handle(new GetPermissionsByPublicIdQuery(userPublicId), cancellationToken);
      if (result.Success)
      {
        return Results.Ok(new GetPermissionsByPublicIdResponse(true, result.Data));
      }

      return Results.Json(new GetPermissionsByPublicIdResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .WithName("GetUserPermissionsByPublicId")
    .WithTags("Users")
    .WithSummary("Obtiene los permisos efectivos de un usuario.")
    .WithDescription("Lista los permisos efectivos de un usuario por PublicId, resueltos a partir de sus roles activos. Requiere el permiso RBAC del endpoint.")
    .RequireAuthorization()
    .Produces<GetPermissionsByPublicIdResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
    .WithMetadata(
      new RequirePermissionsAttribute("Consulta administrativa de permisos por usuario"),
      new GetPermissionsByPublicIdOperationDocs());
  }
}
