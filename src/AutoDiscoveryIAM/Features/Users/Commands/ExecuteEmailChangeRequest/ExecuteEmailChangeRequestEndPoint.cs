// -----------------------------------------------------------------------
// <copyright file="ExecuteEmailChangeRequestEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ExecuteEmailChangeRequest.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ExecuteEmailChangeRequest;

/// <summary>
/// Tercer y ultimo paso del flujo de cambio de email: ejecucion real, reservada a
/// SuAdmin.
/// </summary>
public static class ExecuteEmailChangeRequestEndPoint
{
  public static void MapExecuteEmailChangeRequestEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/users/email-change-requests/{publicId:guid}/execute", async (
        Guid publicId,
        ExecuteEmailChangeRequestHandler handler,
        CancellationToken cancellationToken) =>
    {
      var command = new ExecuteEmailChangeRequestCommand(publicId);
      var result = await handler.Handle(command, cancellationToken);
      if (result.Success)
      {
        return Results.Ok(new ExecuteEmailChangeRequestResponse(true, result.Data));
      }

      return Results.Json(
        new ExecuteEmailChangeRequestResponse(false, null, result.Error, result.ErrorInfo),
        statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .RequireAuthorization()
    .WithName("ExecuteEmailChangeRequest")
    .WithTags("Users")
    .WithSummary("Ejecuta el cambio real de email de una solicitud ya aprobada.")
    .WithDescription("Tercer y ultimo paso del flujo de cambio de email: cambia User.Email al valor solicitado, reinicia IsEmailVerified y reenvia el correo de activacion. Requiere que la solicitud este en estado 'approved_for_execution'. Reservado a SuAdmin por ser la accion mas sensible del flujo.")
    .Produces<ExecuteEmailChangeRequestResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.SuperAdminAccessRequired.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status424Dependency.StatusCode, "application/json")
    .WithMetadata(
        new RequiresSuAdminAccessAttribute("Ejecuta el cambio real de email de un usuario — la accion mas sensible del flujo de cambio de email."),
        new ExecuteEmailChangeRequestOperationDocs());
  }
}
