using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;

public static class DeleteUserRolesByPublicIdEndPoint
{
    public static void MapDeleteUserRolesByPublicIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/users/{userPublicId:guid}/roles/{rolePublicId:guid}", async (
          Guid userPublicId,
          Guid rolePublicId,
          DeleteUserRolesByPublicIdHandler handler,
                    DeleteUserRolesByPublicIdCommandValidator validator,
          CancellationToken cancellationToken) =>
        {
            var command = new DeleteUserRolesByPublicIdCommand(userPublicId, rolePublicId);
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                var errorMsg = string.Join("; ", validation.Errors.Select(error => error.ErrorMessage));
                return Results.BadRequest(new DeleteUserRolesByPublicIdResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
            }

            var result = await handler.Handle(command, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new DeleteUserRolesByPublicIdResponse(true, result.Data));
            }

            return Results.Json(new DeleteUserRolesByPublicIdResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .WithName("DeleteUserRolesByPublicId")
        .WithTags("Users")
        .WithSummary("Desasigna un rol de un usuario.")
        .WithDescription("Elimina logicamente (soft-delete) la relacion activa entre un usuario y un rol por PublicId. Requiere el permiso RBAC del endpoint.")
        .RequireAuthorization()
        .Produces<DeleteUserRolesByPublicIdResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
        .WithMetadata(
          new RequirePermissionsAttribute("Desasignación administrativa de roles a usuarios"),
          new DeleteUserRolesByPublicIdOperationDocs());
    }
}
