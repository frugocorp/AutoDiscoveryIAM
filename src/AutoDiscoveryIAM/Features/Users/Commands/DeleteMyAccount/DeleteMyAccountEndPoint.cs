// -----------------------------------------------------------------------
// <copyright file="DeleteMyAccountEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount;

/// <summary>
/// Autoservicio: el propio usuario autenticado elimina (soft-delete) su cuenta.
/// </summary>
public static class DeleteMyAccountEndPoint
{
  public static void MapDeleteMyAccountEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapDelete("/users/me", async (
        [FromBody] DeleteMyAccountRequestDto? request,
        IAuthContextService authContext,
        DeleteMyAccountHandler handler,
        CancellationToken cancellationToken) =>
    {
      var userPublicId = authContext.GetCurrentUserPublicId();
      if (userPublicId is null)
      {
        return Results.Json(
            new DeleteMyAccountResponse(false, null, "No autenticado.", ErrorInfo.Status401Unauthorized),
            statusCode: ErrorInfo.Status401Unauthorized.StatusCode);
      }

      var result = await handler.Handle(new DeleteMyAccountCommand(userPublicId.Value, request?.CurrentPassword), cancellationToken);
      if (result.Success)
      {
        return Results.Ok(new DeleteMyAccountResponse(true, result.Data));
      }

      return Results.Json(
          new DeleteMyAccountResponse(false, null, result.Error, result.ErrorInfo),
          statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .RequireAuthorization()
    .WithName("DeleteMyAccount")
    .WithTags("Users")
    .WithSummary("Elimina (borrado lógico) la cuenta del usuario autenticado.")
    .WithDescription("Autoservicio: el propio usuario elimina (soft-delete) su propia cuenta, confirmando con su contraseña actual en el cuerpo (currentPassword). Una cuenta creada con Google que nunca tuvo contraseña primero la define con el restablecimiento de contraseña. Tras la eliminación, sus sesiones/tokens dejan de resolver un usuario válido en peticiones futuras.")
    .WithMetadata(new PermissionsNotRequiredAttribute("Endpoint self-service: cualquier usuario autenticado puede eliminar su propia cuenta"))
    .Produces<DeleteMyAccountResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .WithMetadata(new DeleteMyAccountOperationDocs());
  }
}
