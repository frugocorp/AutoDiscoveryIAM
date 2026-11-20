using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;

public static class UpdateBulkUserRolesByRolePublicIdEndPoint
{
    public static void MapUpdateBulkUserRolesByRolePublicIdEndPoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/roles/{rolePublicId:guid}/users", async (
          Guid rolePublicId,
          UpdateBulkUserRolesByRolePublicIdRequestDto request,
          UpdateBulkUserRolesByRolePublicIdHandler handler,
                    UpdateBulkUserRolesByRolePublicIdCommandValidator validator,
          CancellationToken cancellationToken) =>
        {
            var command = new UpdateBulkUserRolesByRolePublicIdCommand(rolePublicId, request.UserPublicIds, request.DryRun);
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                var errorMsg = string.Join("; ", validation.Errors.Select(error => error.ErrorMessage));
                return Results.BadRequest(new UpdateBulkUserRolesByRolePublicIdResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
            }

            var result = await handler.Handle(command, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new UpdateBulkUserRolesByRolePublicIdResponse(true, result.Data));
            }

            return Results.Json(new UpdateBulkUserRolesByRolePublicIdResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .WithName("UpdateBulkUserRolesByRolePublicId")
        .WithTags("Users")
        .WithSummary("Sincroniza el conjunto de usuarios de un rol.")
        .WithDescription("Reconcilia los usuarios asociados a un rol contra el estado final deseado: asigna los nuevos, reactiva los eliminados y desasigna los que sobran. Con DryRun=true calcula el resultado sin persistir cambios.")
        .RequireAuthorization()
        .Accepts<UpdateBulkUserRolesByRolePublicIdRequestDto>("application/json")
        .Produces<UpdateBulkUserRolesByRolePublicIdResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .WithMetadata(
          new RequirePermissionsAttribute("Actualización masiva de usuarios por rol"),
          new UpdateBulkUserRolesByRolePublicIdOperationDocs());
    }
}
