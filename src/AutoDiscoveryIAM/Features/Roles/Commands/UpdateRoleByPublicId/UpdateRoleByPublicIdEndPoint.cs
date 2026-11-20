// -----------------------------------------------------------------------
// <copyright file="UpdateRoleByPublicIdEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;

public static class UpdateRoleByPublicIdEndpoint
{
  public static void MapUpdateRoleByPublicIdEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPut("/roles/{publicId:guid}", async (
        [AsParameters] UpdateRoleByPublicIdRequestDto request,
        UpdateRoleByPublicIdHandler handler,
        UpdateRoleByPublicIdCommandValidator validator,
        CancellationToken cancellationToken) =>
    {
      var command = new UpdateRoleByPublicIdCommand(request.PublicId, request.Body);
      var validation = await validator.ValidateAsync(command, cancellationToken);

      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        var response = new UpdateRoleByPublicIdResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
        return Results.BadRequest(response);
      }

      var result = await handler.Handle(command, cancellationToken);

      if (result.Success)
      {
        var response = new UpdateRoleByPublicIdResponse(true, result.Data);
        return Results.Ok(response);
      }
      else
      {
        var response = new UpdateRoleByPublicIdResponse(false, null, result.Error, result.ErrorInfo);
        // Devuelve el status code correcto según el ErrorInfo.
        return Results.Json(
                response,
                statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
      }
    })
    .RequireAuthorization()
    .WithName("UpdateRoleByPublicId")
    .WithTags("Roles")
    .WithSummary("Actualiza nombre, descripción y estado de un rol.")
    .WithDescription("Reemplaza nombre, descripción y estado activo/inactivo de un rol existente identificado por su PublicId.")
    .Accepts<UpdateRoleByPublicIdBodyDto>("application/json")
    .Produces<UpdateRoleByPublicIdResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status403Forbidden.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
    .WithMetadata(
        new RequirePermissionsAttribute("Por seguridad todos los endpoints requieren permisos por defecto, tomando una metodología Secure by Default, pero se utiliza el atributo RequirePermissionsAttribute para documentar explícitamente la necesidad de permisos específicos y porque son requeridos."),
        new UpdateRoleByPublicIdOperationDocs());
  }
}
