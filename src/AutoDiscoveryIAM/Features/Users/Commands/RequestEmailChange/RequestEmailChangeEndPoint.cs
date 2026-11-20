// -----------------------------------------------------------------------
// <copyright file="RequestEmailChangeEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange;

/// <summary>
/// Primer paso del flujo de cambio de email: solicitud self-service del usuario
/// autenticado. No aplica el cambio — ver <c>ReviewEmailChangeRequest</c>/<c>ExecuteEmailChangeRequest</c>.
/// </summary>
public static class RequestEmailChangeEndPoint
{
  public static void MapRequestEmailChangeEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/users/me/email-change-requests", async (
        RequestEmailChangeRequestDto dto,
        IAuthContextService authContext,
        RequestEmailChangeHandler handler,
        RequestEmailChangeCommandValidator validator,
        CancellationToken cancellationToken) =>
    {
      var userPublicId = authContext.GetCurrentUserPublicId();
      if (userPublicId is null)
      {
        return Results.Json(
          new RequestEmailChangeResponse(false, null, "No autenticado.", ErrorInfo.Status401Unauthorized),
          statusCode: ErrorInfo.Status401Unauthorized.StatusCode);
      }

      var command = new RequestEmailChangeCommand(userPublicId.Value, dto.NewEmail);
      var validation = await validator.ValidateAsync(command, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        return Results.BadRequest(new RequestEmailChangeResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
      }

      var result = await handler.Handle(command, cancellationToken);
      if (result.Success)
      {
        return Results.Ok(new RequestEmailChangeResponse(true, result.Data));
      }

      return Results.Json(
        new RequestEmailChangeResponse(false, null, result.Error, result.ErrorInfo),
        statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .RequireAuthorization()
    .WithName("RequestEmailChange")
    .WithTags("Users")
    .WithSummary("Solicita un cambio de email para el usuario autenticado.")
    .WithDescription("Primer paso del flujo de cambio de email: el email es el username, por lo que no es autoservicio directo. Crea una solicitud en estado 'pending' a la espera de revision de un Admin — no aplica el cambio. Solo puede existir una solicitud pendiente por usuario a la vez.")
    .WithMetadata(new PermissionsNotRequiredAttribute("Endpoint self-service: cualquier usuario autenticado puede solicitar el cambio de su propio email"))
    .Accepts<RequestEmailChangeRequestDto>("application/json")
    .Produces<RequestEmailChangeResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
    .WithMetadata(new RequestEmailChangeOperationDocs());
  }
}
