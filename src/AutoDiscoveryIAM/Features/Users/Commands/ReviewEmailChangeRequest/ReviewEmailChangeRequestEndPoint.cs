// -----------------------------------------------------------------------
// <copyright file="ReviewEmailChangeRequestEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest.Docs;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest;

/// <summary>
/// Segundo paso del flujo de cambio de email: un Admin aprueba para ejecucion o
/// rechaza. No aplica el cambio de email — eso lo hace <c>ExecuteEmailChangeRequest</c> (SuAdmin).
/// </summary>
public static class ReviewEmailChangeRequestEndPoint
{
  public static void MapReviewEmailChangeRequestEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPut("/users/email-change-requests/{publicId:guid}/review", async (
        Guid publicId,
        ReviewEmailChangeRequestBodyDto dto,
        ReviewEmailChangeRequestHandler handler,
        ReviewEmailChangeRequestCommandValidator validator,
        CancellationToken cancellationToken) =>
    {
      var command = new ReviewEmailChangeRequestCommand(publicId, dto.Approve, dto.RejectionReason);
      var validation = await validator.ValidateAsync(command, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        return Results.BadRequest(new ReviewEmailChangeRequestResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
      }

      var result = await handler.Handle(command, cancellationToken);
      if (result.Success)
      {
        return Results.Ok(new ReviewEmailChangeRequestResponse(true, result.Data));
      }

      return Results.Json(
        new ReviewEmailChangeRequestResponse(false, null, result.Error, result.ErrorInfo),
        statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .RequireAuthorization()
    .WithName("ReviewEmailChangeRequest")
    .WithTags("Users")
    .WithSummary("Revisa (aprueba para ejecucion o rechaza) una solicitud de cambio de email.")
    .WithDescription("Segundo paso del flujo de cambio de email: requiere el permiso de revision (tipicamente asignado al rol AdminAutoDiscoveryIAM). Aprobar no aplica el cambio real — solo lo deja listo para que un SuAdmin lo ejecute via ExecuteEmailChangeRequest. Rechazar requiere indicar RejectionReason.")
    .Accepts<ReviewEmailChangeRequestBodyDto>("application/json")
    .Produces<ReviewEmailChangeRequestResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status403Forbidden.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
    .WithMetadata(new ReviewEmailChangeRequestOperationDocs());
  }
}
