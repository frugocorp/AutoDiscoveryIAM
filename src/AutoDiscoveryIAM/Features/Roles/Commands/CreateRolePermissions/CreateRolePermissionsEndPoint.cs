using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;

public static class CreateRolePermissionsEndPoint
{
    public static void MapCreateRolePermissionsEndPoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/roles/{rolePublicId:guid}/permissions/{permissionPublicId:guid}", async (
          Guid rolePublicId,
          Guid permissionPublicId,
          CreateRolePermissionsHandler handler,
                    CreateRolePermissionsCommandValidator validator,
          CancellationToken cancellationToken) =>
        {
            var command = new CreateRolePermissionsCommand(rolePublicId, permissionPublicId);
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                var errorMsg = string.Join("; ", validation.Errors.Select(error => error.ErrorMessage));
                return Results.BadRequest(new CreateRolePermissionsResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
            }

            var result = await handler.Handle(command, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new CreateRolePermissionsResponse(true, result.Data));
            }

            return Results.Json(new CreateRolePermissionsResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .WithName("CreateRolePermissions")
        .WithTags("Roles")
        .WithSummary("Asigna un permiso a un rol.")
        .WithDescription("Crea la relación entre un rol y un permiso por PublicId, o la reactiva si estaba eliminada logicamente. Requiere el permiso RBAC del endpoint.")
        .RequireAuthorization()
        .Produces<CreateRolePermissionsResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
        .WithMetadata(
          new RequirePermissionsAttribute("Asignación administrativa de permisos a roles"),
          new CreateRolePermissionsOperationDocs());
    }
}
