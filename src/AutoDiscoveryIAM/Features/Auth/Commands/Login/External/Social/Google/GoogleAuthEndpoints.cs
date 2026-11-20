// -----------------------------------------------------------------------
// <copyright file="GoogleAuthEndpoints.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Contracts;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Social.Google.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Social.Google;

public static class GoogleAuthEndpoints
{
    private const string GoogleStateCookieName = "google_oauth_state";

    public static void MapGoogleAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGoogleLoginEndpoint();
        app.MapGoogleCallbackEndpoint();
        app.MapGoogleLinkEndpoint();
        app.MapGoogleUnlinkEndpoint();
    }

    private static void MapGoogleLoginEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/google", HandleGoogleRedirect)
        .WithName("GoogleLogin")
        .WithTags("Auth")
        .WithSummary("Inicia login social con Google (OAuth 2.0 / OIDC).")
        .WithDescription("Redirige (302) a la pantalla de autorizacion de Google, con un state de seguridad que se valida en el callback. No devuelve un cuerpo JSON: el flujo continua en el navegador del usuario.")
        .Produces(StatusCodes.Status302Found)
        .Produces<ApiResponse<object>>(ErrorInfo.Status503Unavailable.StatusCode, "application/json")
        .WithMetadata(
          new PermissionsNotRequiredAttribute("Autenticación social - no requiere permisos"),
          new GoogleLoginOperationDocs());
    }

    private static void MapGoogleCallbackEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/google/callback", HandleGoogleCallbackAsync)
        .WithName("GoogleCallback")
        .WithTags("Auth")
        .WithSummary("Procesa el callback OAuth de Google.")
        .WithDescription("Valida el code y el state devueltos por Google, vincula o crea el usuario correspondiente y emite un JWT propio si la cuenta esta activa. Si Authentication:ExternalLoginRedirectUri esta configurado, en vez de responder JSON redirige (302) a esa URL -- en exito con un codigo de un solo uso (#code=...) que la SPA canjea en POST /auth/external-login/exchange, en error con el detalle en el fragmento (#error=...&errorDescription=...); si no esta configurado, responde JSON directo (default, compatible con versiones anteriores).")
        .Produces<LoginResponse>(StatusCodes.Status200OK, "application/json")
        .Produces(StatusCodes.Status302Found)
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status424Dependency.StatusCode, "application/json")
        .WithMetadata(
          new PermissionsNotRequiredAttribute("Autenticación social - callback sin permisos"),
          new GoogleCallbackOperationDocs());
    }

    private static void MapGoogleLinkEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/google/link", HandleGoogleLinkAsync)
        .RequireAuthorization()
        .WithName("GoogleLink")
        .WithTags("Auth")
        .WithSummary("Vincula una cuenta Google al usuario autenticado.")
        .WithDescription("Intercambia un code de Google y vincula esa identidad externa al usuario actual. Falla con 409 si la cuenta Google ya esta vinculada a otro usuario, o si el usuario actual ya tiene otra cuenta Google vinculada.")
        .Produces<GoogleLinkResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status424Dependency.StatusCode, "application/json")
        .WithMetadata(
          new PermissionsNotRequiredAttribute("Requiere autenticación pero no permiso RBAC específico"),
          new GoogleLinkOperationDocs());
    }

    private static void MapGoogleUnlinkEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/auth/google/unlink", HandleGoogleUnlinkAsync)
        .RequireAuthorization()
        .WithName("GoogleUnlink")
        .WithTags("Auth")
        .WithSummary("Desvincula la cuenta Google del usuario autenticado.")
        .WithDescription("Elimina la relación entre el usuario actual y su login externo de Google. Falla con 404 si no existe una vinculación activa.")
        .Produces<GoogleLinkResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .WithMetadata(
          new PermissionsNotRequiredAttribute("Requiere autenticación pero no permiso RBAC específico"),
          new GoogleUnlinkOperationDocs());
    }

    private static IResult HandleGoogleRedirect(HttpContext context, ExternalLoginProviderResolver resolver)
    {
        var provider = resolver.Resolve(ExternalLoginProviders.Google);
        if (provider == null)
        {
            var response = new LoginResponse(false, null, "Proveedor Google no configurado.", ErrorInfo.Status503Unavailable);
            return Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var state = Guid.NewGuid().ToString("N");
        context.Response.Cookies.Append(
          GoogleStateCookieName,
          state,
          new CookieOptions
          {
              HttpOnly = true,
              Secure = true,
              SameSite = SameSiteMode.Lax,
              Expires = DateTimeOffset.UtcNow.AddMinutes(10),
          });

        return Results.Redirect(provider.BuildAuthorizationUrl(state));
    }

    private static async Task<IResult> HandleGoogleCallbackAsync(
      HttpContext context,
      string? code,
      string? state,
      string? error,
      ExternalLoginOrchestrator orchestrator,
      IExternalLoginExchangeCodeRepository exchangeCodeRepository,
      IOptions<ExternalLoginSettings> externalLoginSettings,
      CancellationToken cancellationToken)
    {
        var settings = externalLoginSettings.Value;

        var preValidationResult = ValidateCallbackRequest(context, code, state, error, settings);
        if (preValidationResult != null)
        {
            return preValidationResult;
        }

        var result = await orchestrator.HandleCallbackAsync(
          ExternalLoginProviders.Google,
          code!,
          context.Connection.RemoteIpAddress?.ToString(),
          context.Request.Headers.UserAgent.ToString(),
          cancellationToken);

        if (!result.Success)
        {
            return BuildErrorResult(result.Error, result.ErrorInfo, StatusCodes.Status500InternalServerError, settings);
        }

        return await BuildSuccessResultAsync(result.Data!, settings, exchangeCodeRepository, cancellationToken);
    }

    private static IResult? ValidateCallbackRequest(HttpContext context, string? code, string? state, string? error, ExternalLoginSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            // El texto de "error" no se devuelve: cualquiera puede armar el enlace al callback con un
            // texto propio, que terminaría mostrado por el frontend como si viniera de la API.
            return BuildErrorResult("Google no completo el inicio de sesion.", ErrorInfo.Status401Unauthorized, StatusCodes.Status401Unauthorized, settings);
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return BuildErrorResult("Callback de Google invalido: falta codigo o state.", ErrorInfo.Status400Validation, StatusCodes.Status400BadRequest, settings);
        }

        var cookieState = context.Request.Cookies[GoogleStateCookieName];
        context.Response.Cookies.Delete(GoogleStateCookieName);
        if (string.IsNullOrWhiteSpace(cookieState) || !string.Equals(cookieState, state, StringComparison.Ordinal))
        {
            return BuildErrorResult("State invalido en callback de Google.", ErrorInfo.Status401Unauthorized, StatusCodes.Status401Unauthorized, settings);
        }

        return null;
    }

    /// <summary>
    /// Sin <see cref="ExternalLoginSettings.ExternalLoginRedirectUri"/> configurado, responde JSON
    /// directo (default, compatible con el comportamiento anterior a este cambio -- util para
    /// probar el flujo sin una app cliente real, ej. Swagger/Postman). Configurado, redirige (302)
    /// con el error en el fragmento (<c>#error=...&amp;errorDescription=...</c>) -- no hay ningun
    /// credencial que proteger en un error, a diferencia del caso de exito (ver
    /// <see cref="BuildSuccessResultAsync"/>), asi que aqui no hace falta un codigo de intercambio.
    /// </summary>
    private static IResult BuildErrorResult(string? error, ErrorInfo? errorInfo, int fallbackStatusCode, ExternalLoginSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ExternalLoginRedirectUri))
        {
            var response = new LoginResponse(false, null, error, errorInfo);
            return Results.Json(response, statusCode: errorInfo?.StatusCode ?? fallbackStatusCode);
        }

        var fragment = $"error={Uri.EscapeDataString(errorInfo?.Key ?? ErrorInfo.Status500Unknown.Key)}&errorDescription={Uri.EscapeDataString(error ?? string.Empty)}";
        return Results.Redirect($"{settings.ExternalLoginRedirectUri}#{fragment}");
    }

    /// <summary>
    /// Sin <see cref="ExternalLoginSettings.ExternalLoginRedirectUri"/> configurado, responde JSON
    /// directo con el token/refreshToken reales (comportamiento anterior a este cambio). Configurado,
    /// en vez de poner el token/refreshToken directo en la URL (inseguro: quedaria visible en el
    /// historial del navegador mientras la SPA no limpie la URL), genera un código de un solo uso
    /// y vida cortísima (<see cref="IExternalLoginExchangeCodeRepository"/>, patrón "authorization
    /// code" de OAuth2 -- el mismo que usan Auth0/Okta/Cognito para este mismo hand-off) y redirige
    /// solo con ese código (<c>#code=...</c>). La SPA lo canjea de inmediato por el token real vía
    /// <c>POST /auth/external-login/exchange</c>, en el cuerpo de una respuesta JSON normal -- el
    /// token nunca toca ninguna URL.
    /// </summary>
    private static async Task<IResult> BuildSuccessResultAsync(
      LoginResponseDto data,
      ExternalLoginSettings settings,
      IExternalLoginExchangeCodeRepository exchangeCodeRepository,
      CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.ExternalLoginRedirectUri))
        {
            return Results.Ok(new LoginResponse(true, data));
        }

        var plaintextCode = await exchangeCodeRepository.CreateAsync(
          data,
          TimeSpan.FromSeconds(settings.ExchangeCodeExpirySeconds),
          cancellationToken);

        return Results.Redirect($"{settings.ExternalLoginRedirectUri}#code={Uri.EscapeDataString(plaintextCode)}");
    }

    private static async Task<IResult> HandleGoogleLinkAsync(
      GoogleLinkRequestDto request,
      HttpContext context,
      ExternalLoginOrchestrator orchestrator,
      CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            var badRequest = new GoogleLinkResponse(false, null, "El campo code es requerido.", ErrorInfo.Status400Validation);
            return Results.BadRequest(badRequest);
        }

        var userPublicId = ResolveCurrentUserPublicId(context.User);
        if (userPublicId == null)
        {
            var unauthorized = new GoogleLinkResponse(false, null, "Usuario no autenticado.", ErrorInfo.Status401Unauthorized);
            return Results.Json(unauthorized, statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await orchestrator.LinkExternalIdentityAsync(
          ExternalLoginProviders.Google,
          request.Code,
          userPublicId.Value,
          cancellationToken);

        if (!result.Success)
        {
            var errorResponse = new GoogleLinkResponse(false, null, result.Error, result.ErrorInfo);
            return Results.Json(errorResponse, statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        }

        return Results.Ok(new GoogleLinkResponse(true, result.Data));
    }

    private static async Task<IResult> HandleGoogleUnlinkAsync(
      HttpContext context,
      ExternalLoginOrchestrator orchestrator,
      CancellationToken cancellationToken)
    {
        var userPublicId = ResolveCurrentUserPublicId(context.User);
        if (userPublicId == null)
        {
            var unauthorized = new GoogleLinkResponse(false, null, "Usuario no autenticado.", ErrorInfo.Status401Unauthorized);
            return Results.Json(unauthorized, statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await orchestrator.UnlinkExternalIdentityAsync(
          ExternalLoginProviders.Google,
          userPublicId.Value,
          cancellationToken);

        if (!result.Success)
        {
            var errorResponse = new GoogleLinkResponse(false, null, result.Error, result.ErrorInfo);
            return Results.Json(errorResponse, statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        }

        return Results.Ok(new GoogleLinkResponse(true, result.Data));
    }

    private static Guid? ResolveCurrentUserPublicId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue("PublicId") ?? user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}

public record GoogleLinkRequestDto(string Code);

public record GoogleLinkResponseDto(string Message);

public record GoogleLinkResponse : FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse.ApiResponse<ExternalLinkOperationResult>
{
    public GoogleLinkResponse(bool success, ExternalLinkOperationResult? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo)
    {
    }
}
