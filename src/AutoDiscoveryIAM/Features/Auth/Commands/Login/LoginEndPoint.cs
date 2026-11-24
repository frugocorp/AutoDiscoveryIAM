// -----------------------------------------------------------------------
// <copyright file="LoginEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;

/// <summary>
/// Endpoint Minimal API para login de usuario.
/// </summary>
public static class LoginEndPoint
{
  public static void MapLoginEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/auth/login", async (
      LoginRequestDto requestDto,
      LoginHandler handler,
      LoginCommandValidator validator,
      IRefreshTokenDelivery refreshTokenDelivery,
      HttpContext context,
      CancellationToken cancellationToken) =>
    {
      // Convertir DTO a Command con información de contexto HTTP
      var command = requestDto.ToCommand(context);

      var validation = await validator.ValidateAsync(command, cancellationToken);

      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        var response = new LoginResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
        return Results.BadRequest(response);
      }

      var result = await handler.Handle(command, cancellationToken);
      if (result.Success)
      {
        var data = result.Data! with { RefreshToken = refreshTokenDelivery.Deliver(context, result.Data!.RefreshToken) };
        var response = new LoginResponse(true, data);
        return Results.Ok(response);
      }
      else
      {
        var response = new LoginResponse(false, null, result.Error, result.ErrorInfo);
        return Results.Json(
          response,
          statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
      }
    })
    .WithName("Login")
    .WithTags("Auth")
    .WithSummary("Autentica un usuario y genera su JWT.")
    .WithDescription("Valida email y contraseña. Si el usuario tiene 2FA activo, no emite tokens todavía: responde con un challenge que debe completarse en /auth/2fa/verify. Bloquea la cuenta temporalmente tras varios intentos fallidos.")
    .Accepts<LoginRequestDto>("application/json")
    .Produces<LoginResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .WithMetadata(
        new PermissionsNotRequiredAttribute("Endpoint de autenticación - login inicial, no requiere permisos"),
        new LoginOperationDocs());
  }
}
