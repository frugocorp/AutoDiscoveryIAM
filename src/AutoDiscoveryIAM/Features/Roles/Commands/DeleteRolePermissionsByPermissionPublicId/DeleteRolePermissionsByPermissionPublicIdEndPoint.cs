using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;

public static class DeleteRolePermissionsByPermissionPublicIdEndPoint
{
    public static void MapDeleteRolePermissionsByPermissionPublicIdEndPoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/roles/{rolePublicId:guid}/permissions/{permissionPublicId:guid}", async (
          Guid rolePublicId,
          Guid permissionPublicId,
          DeleteRolePermissionsByPermissionPublicIdHandler handler,
                    DeleteRolePermissionsByPermissionPublicIdCommandValidator validator,
          CancellationToken cancellationToken) =>
        {
            var command = new DeleteRolePermissionsByPermissionPublicIdCommand(rolePublicId, permissionPublicId);
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                var errorMsg = string.Join("; ", validation.Errors.Select(error => error.ErrorMessage));
                return Results.BadRequest(new DeleteRolePermissionsByPermissionPublicIdResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
            }

            var result = await handler.Handle(command, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new DeleteRolePermissionsByPermissionPublicIdResponse(true, result.Data));
            }

            return Results.Json(new DeleteRolePermissionsByPermissionPublicIdResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .WithName("DeleteRolePermissionsByPermissionPublicId")
        .WithTags("Roles")
        .WithSummary("Desasigna un permiso de un rol.")
        .WithDescription("Elimina logicamente (soft-delete) la relacion activa entre un rol y un permiso por PublicId. Requiere el permiso RBAC del endpoint.")
        .RequireAuthorization()
        .Produces<DeleteRolePermissionsByPermissionPublicIdResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
        .WithMetadata(
          new RequirePermissionsAttribute("Desasignación administrativa de permisos a roles"),
          new DeleteRolePermissionsByPermissionPublicIdOperationDocs());
    }
}
