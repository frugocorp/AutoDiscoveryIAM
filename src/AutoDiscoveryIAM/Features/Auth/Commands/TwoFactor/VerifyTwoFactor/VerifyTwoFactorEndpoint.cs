// -----------------------------------------------------------------------
// <copyright file="VerifyTwoFactorEndpoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor;

/// <summary>
/// Endpoint para completar el login validando el segundo factor de autenticación.
/// </summary>
public static class VerifyTwoFactorEndpoint
{
  public static void MapVerifyTwoFactorEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/auth/2fa/verify", HandleVerifyAsync)
      .WithName("VerifyTwoFactor")
      .WithTags("Auth")
      .WithSummary("Verifica el segundo factor y completa el login.")
      .WithDescription("Completa el login validando el código de 2FA (email o Authenticator) asociado a un token de desafío emitido por /auth/login. Devuelve el mismo par de tokens (access + refresh) que un login exitoso.")
      .Produces<VerifyTwoFactorResponse>(StatusCodes.Status200OK, "application/json")
      .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
      .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
      .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
      .WithMetadata(
        new PermissionsNotRequiredAttribute("Endpoint de autenticacion - verificacion de 2FA, no requiere permisos"),
        new VerifyTwoFactorOperationDocs());
  }

  private static async Task<IResult> HandleVerifyAsync(
    VerifyTwoFactorRequestDto requestDto,
    VerifyTwoFactorHandler handler,
    VerifyTwoFactorCommandValidator validator,
    HttpContext context,
    CancellationToken cancellationToken)
  {
    var command = requestDto.ToCommand(context);

    var validation = await validator.ValidateAsync(command, cancellationToken);
    if (!validation.IsValid)
    {
      var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
      var response = new VerifyTwoFactorResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
      return Results.BadRequest(response);
    }

    var result = await handler.Handle(command, cancellationToken);

    if (result.Success)
    {
      var response = new VerifyTwoFactorResponse(true, result.Data);
      return Results.Ok(response);
    }

    var failureResponse = new VerifyTwoFactorResponse(false, null, result.Error, result.ErrorInfo);
    return Results.Json(failureResponse, statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
  }
}
