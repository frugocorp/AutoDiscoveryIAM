using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;

public static class CreateUserRolesEndPoint
{
  public static void MapCreateUserRolesEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPut("/users/{userPublicId:guid}/roles/{rolePublicId:guid}", async (
      Guid userPublicId,
      Guid rolePublicId,
      CreateUserRolesHandler handler,
      CreateUserRolesCommandValidator validator,
      CancellationToken cancellationToken) =>
    {
      var command = new CreateUserRolesCommand(userPublicId, rolePublicId);
      var validation = await validator.ValidateAsync(command, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(error => error.ErrorMessage));
        return Results.BadRequest(new CreateUserRolesResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
      }

      var result = await handler.Handle(command, cancellationToken);
      if (result.Success)
      {
        return Results.Ok(new CreateUserRolesResponse(true, result.Data));
      }

      return Results.Json(new CreateUserRolesResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .WithName("CreateUserRoles")
    .WithTags("Users")
    .WithSummary("Asigna un rol a un usuario.")
    .WithDescription("Crea la relación entre un usuario y un rol por PublicId, o la reactiva si estaba eliminada logicamente. Requiere el permiso RBAC del endpoint.")
    .RequireAuthorization()
    .Produces<CreateUserRolesResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
    .WithMetadata(
      new RequirePermissionsAttribute("Asignación administrativa de roles a usuarios"),
      new CreateUserRolesOperationDocs());
  }
}
