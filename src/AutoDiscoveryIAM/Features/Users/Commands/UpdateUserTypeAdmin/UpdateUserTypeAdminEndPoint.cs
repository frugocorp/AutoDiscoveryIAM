// -----------------------------------------------------------------------
// <copyright file="UpdateUserTypeAdminEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;

public static class UpdateUserTypeAdminEndPoint
{
  public static void MapUpdateUserTypeAdminEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPut("/users/{publicId:guid}/admin-type", async (
        [AsParameters] UpdateUserTypeAdminRequestDto request,
        UpdateUserTypeAdminHandler handler,
        UpdateUserTypeAdminCommandValidator validator,
        CancellationToken cancellationToken) =>
    {
      var command = new UpdateUserTypeAdminCommand(request.PublicId, request.Body);
      var validation = await validator.ValidateAsync(command, cancellationToken);

      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        var response = new UpdateUserTypeAdminResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
        return Results.BadRequest(response);
      }

      var result = await handler.Handle(command, cancellationToken);

      if (result.Success)
      {
        var response = new UpdateUserTypeAdminResponse(true, result.Data);
        return Results.Ok(response);
      }
      else
      {
        var response = new UpdateUserTypeAdminResponse(false, null, result.Error, result.ErrorInfo);
        // Devuelve el status code correcto según el ErrorInfo.
        return Results.Json(
                response,
                statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
      }
    })
    .RequireAuthorization()
    .WithName("UpdateUserTypeAdmin")
    .WithTags("Users")
    .WithSummary("Promueve o revoca el tipo Administrador de un usuario.")
    .WithDescription("Cambia el flag IsAdmin de un usuario por PublicId. Requiere acceso SuAdmin/Development. Falla con 422 si el usuario está eliminado o si ya tiene el estado Admin solicitado.")
    .Accepts<UpdateUserTypeAdminBodyDto>("application/json")
    .Produces<UpdateUserTypeAdminResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.SuperAdminAccessRequired.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
    .WithMetadata(
        new RequiresSuAdminAccessAttribute("Promoción de usuarios a Administrador requiere permisos elevados"),
        new UpdateUserTypeAdminOperationDocs());
  }
}
