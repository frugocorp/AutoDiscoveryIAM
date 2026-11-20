using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId;

public static class GetPermissionsByPublicIdEndPoint
{
    public static void MapGetPermissionsByPublicIdEndPoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/roles/{rolePublicId:guid}/permissions", async (
          Guid rolePublicId,
          GetPermissionsByPublicIdHandler handler,
          CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetPermissionsByPublicIdQuery(rolePublicId), cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new GetPermissionsByPublicIdResponse(true, result.Data));
            }

            return Results.Json(new GetPermissionsByPublicIdResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .WithName("GetRolePermissionsByPublicId")
        .WithTags("Roles")
        .WithSummary("Obtiene los permisos asignados a un rol.")
        .WithDescription("Lista los permisos activos asignados a un rol por PublicId. Requiere el permiso RBAC del endpoint.")
        .RequireAuthorization()
        .Produces<GetPermissionsByPublicIdResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
        .WithMetadata(
          new RequirePermissionsAttribute("Consulta administrativa de permisos por rol"),
          new GetPermissionsByPublicIdOperationDocs());
    }
}
