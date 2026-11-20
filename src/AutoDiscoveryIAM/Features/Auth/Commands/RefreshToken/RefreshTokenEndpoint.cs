// -----------------------------------------------------------------------
// <copyright file="RefreshTokenEndpoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Endpoint para renovación de tokens JWT usando refresh tokens.
/// </summary>
public static class RefreshTokenEndpoint
{
  public static void MapRefreshTokenEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/auth/refresh", HandleRefreshAsync)
    .WithName("RefreshToken")
    .WithTags("Auth")
    .WithSummary("Renueva el JWT usando un refresh token.")
    .WithDescription("Cambia un refresh token válido por un nuevo par de tokens (access + refresh). El refresh token usado queda revocado (rotación automática): reutilizarlo falla con 401.")
    .Produces<RefreshTokenResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .WithMetadata(
      new PermissionsNotRequiredAttribute("Refresh token endpoint - no permissions required for token renewal"),
      new RefreshTokenOperationDocs());
  }

  private static async Task<IResult> HandleRefreshAsync(
    RefreshTokenRequestDto requestDto,
    RefreshTokenHandler handler,
    RefreshTokenCommandValidator validator,
    HttpContext context,
    CancellationToken cancellationToken)
  {
    // Convertir DTO a Command con información de contexto HTTP
    var command = requestDto.ToCommand(context);

    // Validar comando
    var validation = await validator.ValidateAsync(command, cancellationToken);

    if (!validation.IsValid)
    {
      var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
      var response = new RefreshTokenResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
      return Results.BadRequest(response);
    }

    // Ejecutar comando
    var result = await handler.Handle(command, cancellationToken);

    if (result.Success)
    {
      var response = new RefreshTokenResponse(true, result.Data);
      return Results.Ok(response);
    }
    else
    {
      var response = new RefreshTokenResponse(false, null, result.Error, result.ErrorInfo);
      return Results.Json(response, statusCode: result.ErrorInfo?.Status ?? 500);
    }
  }
}
