using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;

public static class UpdateBulkRolePermissionsEndPoint
{
    public static void MapUpdateBulkRolePermissionsEndPoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/roles/{rolePublicId:guid}/permissions", async (
          Guid rolePublicId,
          UpdateBulkRolePermissionsRequestDto request,
          UpdateBulkRolePermissionsHandler handler,
                    UpdateBulkRolePermissionsCommandValidator validator,
          CancellationToken cancellationToken) =>
        {
            var command = new UpdateBulkRolePermissionsCommand(rolePublicId, request.PermissionPublicIds, request.DryRun);
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                var errorMsg = string.Join("; ", validation.Errors.Select(error => error.ErrorMessage));
                return Results.BadRequest(new UpdateBulkRolePermissionsResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
            }

            var result = await handler.Handle(command, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new UpdateBulkRolePermissionsResponse(true, result.Data));
            }

            return Results.Json(new UpdateBulkRolePermissionsResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .WithName("UpdateBulkRolePermissions")
        .WithTags("Roles")
        .WithSummary("Sincroniza el conjunto de permisos de un rol.")
        .WithDescription("Reconcilia los permisos de un rol contra el estado final deseado: asigna los nuevos, reactiva los eliminados y desasigna los que sobran. Con DryRun=true calcula el resultado sin persistir cambios.")
        .RequireAuthorization()
        .Accepts<UpdateBulkRolePermissionsRequestDto>("application/json")
        .Produces<UpdateBulkRolePermissionsResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .WithMetadata(
          new RequirePermissionsAttribute("Actualización masiva de permisos por rol"),
          new UpdateBulkRolePermissionsOperationDocs());
    }
}
