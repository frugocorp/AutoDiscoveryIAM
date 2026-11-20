// -----------------------------------------------------------------------
// <copyright file="RestoreRoleByPublicIdEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId.Docs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;

public static class RestoreRoleByPublicIdEndPoint
{
  public static void MapRestoreRoleByPublicIdEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/roles/{publicId:guid}/restore", async (
        [AsParameters] RestoreRoleByPublicIdRequestDto request,
        RestoreRoleByPublicIdHandler handler,
        RestoreRoleByPublicIdCommandValidator validator,
        CancellationToken cancellationToken) =>
    {
      var command = new RestoreRoleByPublicIdCommand(request.PublicId);
      var validation = await validator.ValidateAsync(command, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        var response = new RestoreRoleByPublicIdResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
        return Results.BadRequest(new { errors = errorMsg });
      }
      var result = await handler.Handle(request, cancellationToken);
      if (result.Success)
      {
        var response = new RestoreRoleByPublicIdResponse(true, result.Data);
        return Results.Ok(response);
      }
      else
      {
        var response = new RestoreRoleByPublicIdResponse(false, null, result.Error, result.ErrorInfo);
        // Devuelve el status code correcto según el ErrorInfo.
        return Results.Json(
          response,
          statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
      }
    })
    .RequireAuthorization()
    .WithName("RestoreRoleByPublicId")
    .WithTags("Roles")
    .WithSummary("Restaura un rol eliminado lógicamente.")
    .WithDescription("Revierte el borrado lógico de un rol: solo funciona si el rol existe y está actualmente eliminado.")
    .Produces<RestoreRoleByPublicIdResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status403Forbidden.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
    .WithMetadata(new RestoreRoleByPublicIdOperationDocs());
  }
}