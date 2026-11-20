using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;

public static class UpdateBulkUserRolesEndPoint
{
  public static void MapUpdateBulkUserRolesEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPut("/users/{userPublicId:guid}/roles", async (
      Guid userPublicId,
      UpdateBulkUserRolesRequestDto request,
      UpdateBulkUserRolesHandler handler,
      UpdateBulkUserRolesCommandValidator validator,
      CancellationToken cancellationToken) =>
    {
      var command = new UpdateBulkUserRolesCommand(userPublicId, request.RolePublicIds, request.DryRun);
      var validation = await validator.ValidateAsync(command, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(error => error.ErrorMessage));
        return Results.BadRequest(new UpdateBulkUserRolesResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
      }

      var result = await handler.Handle(command, cancellationToken);
      if (result.Success)
      {
        return Results.Ok(new UpdateBulkUserRolesResponse(true, result.Data));
      }

      return Results.Json(new UpdateBulkUserRolesResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .WithName("UpdateBulkUserRoles")
    .WithTags("Users")
    .WithSummary("Sincroniza el conjunto de roles de un usuario.")
    .WithDescription("Reconcilia los roles de un usuario contra el estado final deseado: asigna los nuevos, reactiva los eliminados y desasigna los que sobran. Con DryRun=true calcula el resultado sin persistir cambios.")
    .RequireAuthorization()
    .Accepts<UpdateBulkUserRolesRequestDto>("application/json")
    .Produces<UpdateBulkUserRolesResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .WithMetadata(
      new RequirePermissionsAttribute("Actualización masiva de roles por usuario"),
      new UpdateBulkUserRolesOperationDocs());
  }
}
