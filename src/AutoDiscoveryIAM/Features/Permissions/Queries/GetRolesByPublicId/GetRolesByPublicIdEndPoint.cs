using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId;

public static class GetRolesByPublicIdEndPoint
{
    public static void MapGetRolesByPublicIdEndPoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/permissions/{permissionPublicId:guid}/roles", async (
          Guid permissionPublicId,
          GetRolesByPublicIdHandler handler,
          CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(new GetRolesByPublicIdQuery(permissionPublicId), cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new GetRolesByPublicIdResponse(true, result.Data));
            }

            return Results.Json(new GetRolesByPublicIdResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .WithName("GetPermissionRolesByPublicId")
        .WithTags("Permissions")
        .WithSummary("Obtiene los roles que tienen un permiso.")
        .WithDescription("Lista los roles activos que tienen asignado un permiso por PublicId. Requiere el permiso RBAC del endpoint.")
        .RequireAuthorization()
        .Produces<GetRolesByPublicIdResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
        .WithMetadata(
          new RequirePermissionsAttribute("Consulta administrativa de roles por permiso"),
          new GetRolesByPublicIdOperationDocs());
    }
}
