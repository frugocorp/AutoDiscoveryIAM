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
    .WithDescription("Cambia un refresh token válido por un nuevo par de tokens (access + refresh). El refresh token usado queda revocado (rotación automática): reutilizarlo falla con 401 y revoca toda la sesión del usuario. Con RefreshTokenSettings:ReuseGracePeriodSeconds, reusarlo dentro de esos segundos desde el mismo cliente (misma IP y User-Agent) devuelve el mismo par nuevo (reintentos y refresh simultáneos); desde otro cliente revoca toda la sesión. Con RefreshTokenSettings:Delivery en Cookie o Both, el token también se lee de la cookie HttpOnly cuando el cuerpo no lo trae (la petición debe traer entonces el encabezado X-Requested-With, con cualquier valor; sin él responde 400 sin rotar), y el nuevo se entrega en esa cookie.")
    .Produces<RefreshTokenResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .WithMetadata(
      new PermissionsNotRequiredAttribute("Refresh token endpoint - no permissions required for token renewal"),
      new RefreshTokenOperationDocs());
  }

  private static async Task<IResult> HandleRefreshAsync(
    RefreshTokenRequestDto? requestDto,
    RefreshTokenHandler handler,
    RefreshTokenCommandValidator validator,
    IRefreshTokenDelivery refreshTokenDelivery,
    HttpContext context,
    CancellationToken cancellationToken)
  {
    var cookieToken = refreshTokenDelivery.ReadFromCookie(context);
    if (cookieToken != null
        && string.IsNullOrEmpty(requestDto?.RefreshToken)
        && string.IsNullOrEmpty(context.Request.Headers[RefreshTokenDelivery.RequiredCookieRequestHeader]))
    {
      // El navegador adjunta la cookie aunque la petición la inicie otro sitio (con SameSite=None): sin
      // el encabezado propio, que solo manda un cliente que pasó el preflight de CORS, no se rota nada.
      // La cookie no se borra, para que esa petición ajena tampoco cierre la sesión.
      var rejected = new RefreshTokenResponse(
        false,
        null,
        $"Con el refresh token en cookie, la peticion debe traer el encabezado {RefreshTokenDelivery.RequiredCookieRequestHeader}.",
        ErrorInfo.Status400Validation);
      return Results.BadRequest(rejected);
    }

    // Convertir DTO a Command con información de contexto HTTP
    var command = requestDto.ToCommand(context, cookieToken);

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
      var data = result.Data! with { RefreshToken = refreshTokenDelivery.Deliver(context, result.Data!.RefreshToken) };
      var response = new RefreshTokenResponse(true, data);
      return Results.Ok(response);
    }
    else
    {
      // Un token rechazado ya no sirve: se borra la cookie para que el navegador no lo siga mandando.
      refreshTokenDelivery.Clear(context);
      var response = new RefreshTokenResponse(false, null, result.Error, result.ErrorInfo);
      return Results.Json(response, statusCode: result.ErrorInfo?.Status ?? 500);
    }
  }
}
