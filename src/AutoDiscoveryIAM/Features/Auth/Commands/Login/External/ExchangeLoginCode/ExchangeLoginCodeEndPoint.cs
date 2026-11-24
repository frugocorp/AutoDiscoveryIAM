// -----------------------------------------------------------------------
// <copyright file="ExchangeLoginCodeEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode;

/// <summary>
/// Endpoint de canje del código de un solo uso que entrega <c>GoogleCallback</c> (u otro proveedor
/// externo a futuro) vía <c>#code=...</c> cuando <c>Authentication:ExternalLoginRedirectUri</c>
/// está configurado. Mismo patrón "authorization code" que usan Auth0/Okta/Cognito para este
/// hand-off de sesión hacia una SPA sin exponer el token real en la URL.
/// </summary>
public static class ExchangeLoginCodeEndPoint
{
  public static void MapExchangeLoginCodeEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/auth/external-login/exchange", HandleExchangeAsync)
    .WithName("ExchangeLoginCode")
    .WithTags("Auth")
    .WithSummary("Canjea un código de un solo uso de login externo por el JWT/refresh token reales.")
    .WithDescription("Lo llama la SPA justo al cargar la ruta a la que la redirigió Authentication:ExternalLoginRedirectUri (#code=...). El código es de un solo uso y vence rápido (Authentication:ExchangeCodeExpirySeconds, default 60s) -- reutilizarlo, o mandar uno inválido/vencido, falla con 401.")
    .Accepts<ExchangeLoginCodeRequestDto>("application/json")
    .Produces<ExchangeLoginCodeResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .WithMetadata(
      new PermissionsNotRequiredAttribute("Autenticación social - canje de código, no requiere permisos"),
      new ExchangeLoginCodeOperationDocs());
  }

  private static async Task<IResult> HandleExchangeAsync(
    ExchangeLoginCodeRequestDto requestDto,
    ExchangeLoginCodeHandler handler,
    ExchangeLoginCodeCommandValidator validator,
    IRefreshTokenDelivery refreshTokenDelivery,
    HttpContext context,
    CancellationToken cancellationToken)
  {
    var command = new ExchangeLoginCodeCommand(requestDto.Code);

    var validation = await validator.ValidateAsync(command, cancellationToken);
    if (!validation.IsValid)
    {
      var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
      var invalidResponse = new ExchangeLoginCodeResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
      return Results.BadRequest(invalidResponse);
    }

    var result = await handler.Handle(command, cancellationToken);

    if (result.Success)
    {
      var data = result.Data! with { RefreshToken = refreshTokenDelivery.Deliver(context, result.Data!.RefreshToken) };
      return Results.Ok(new ExchangeLoginCodeResponse(true, data));
    }

    var errorResponse = new ExchangeLoginCodeResponse(false, null, result.Error, result.ErrorInfo);
    return Results.Json(errorResponse, statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
  }
}
