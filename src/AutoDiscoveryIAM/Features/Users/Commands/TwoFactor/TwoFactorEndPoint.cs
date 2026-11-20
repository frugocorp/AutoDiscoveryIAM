using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.TwoFactor.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.TwoFactor;

/// <summary>
/// Endpoints de autogestión de 2FA (habilitar email, enrolar/confirmar Authenticator, deshabilitar).
/// </summary>
public static class TwoFactorEndPoint
{
  public static void MapTwoFactorEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapEnableEmailEndpoint();
    app.MapEnrollAuthenticatorEndpoint();
    app.MapConfirmAuthenticatorEndpoint();
    app.MapDisableEndpoint();
  }

  private static void MapEnableEmailEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/users/me/2fa/email/enable", async (
      EnableEmailTwoFactorRequestDto dto,
      EnableEmailTwoFactorRequestValidator validator,
      IAuthContextService authContext,
      ITwoFactorService service,
      CancellationToken cancellationToken) =>
    {
      var validation = await validator.ValidateAsync(dto, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        return Results.BadRequest(new TwoFactorResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
      }

      var userPublicId = authContext.GetCurrentUserPublicId();
      if (userPublicId == null)
      {
        return Results.Json(new TwoFactorResponse(false, null, "Usuario no autenticado.", ErrorInfo.Status401Unauthorized), statusCode: ErrorInfo.Status401Unauthorized.StatusCode);
      }

      var result = await service.EnableEmailAsync(userPublicId.Value, dto.CurrentPassword, dto.Code, cancellationToken);
      if (result.Success)
      {
        return Results.Ok(new TwoFactorResponse(true, result.Data));
      }

      return Results.Json(new TwoFactorResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .RequireAuthorization()
    .WithName("EnableEmailTwoFactor")
    .WithTags("Users")
    .WithSummary("Activa 2FA por correo para el usuario autenticado.")
    .WithDescription("Requiere confirmar la contrasena actual. El correo ya esta verificado desde la activacion de cuenta, no requiere codigo adicional.")
    .WithMetadata(new PermissionsNotRequiredAttribute("Endpoint self-service: requiere autenticacion, no permiso RBAC especifico"))
    .Produces<TwoFactorResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .WithMetadata(new EnableEmailTwoFactorOperationDocs());
  }

  private static void MapEnrollAuthenticatorEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/users/me/2fa/authenticator/enroll", async (
      EnrollAuthenticatorRequestDto dto,
      EnrollAuthenticatorRequestValidator validator,
      IAuthContextService authContext,
      ITwoFactorService service,
      CancellationToken cancellationToken) =>
    {
      var validation = await validator.ValidateAsync(dto, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        return Results.BadRequest(new EnrollAuthenticatorResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
      }

      var userPublicId = authContext.GetCurrentUserPublicId();
      if (userPublicId == null)
      {
        return Results.Json(new EnrollAuthenticatorResponse(false, null, "Usuario no autenticado.", ErrorInfo.Status401Unauthorized), statusCode: ErrorInfo.Status401Unauthorized.StatusCode);
      }

      var result = await service.EnrollAuthenticatorAsync(userPublicId.Value, dto.CurrentPassword, dto.Code, cancellationToken);
      if (result.Success)
      {
        return Results.Ok(new EnrollAuthenticatorResponse(true, result.Data));
      }

      return Results.Json(new EnrollAuthenticatorResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .RequireAuthorization()
    .WithName("EnrollAuthenticatorTwoFactor")
    .WithTags("Users")
    .WithSummary("Genera un secreto TOTP pendiente de confirmacion para el usuario autenticado.")
    .WithDescription("Requiere confirmar la contrasena actual. Devuelve el secreto Base32 y la URI otpauth:// para que el cliente arme el QR. No activa 2FA hasta confirmar con /confirm.")
    .WithMetadata(new PermissionsNotRequiredAttribute("Endpoint self-service: requiere autenticacion, no permiso RBAC especifico"))
    .Produces<EnrollAuthenticatorResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .WithMetadata(new EnrollAuthenticatorTwoFactorOperationDocs());
  }

  private static void MapConfirmAuthenticatorEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/users/me/2fa/authenticator/confirm", async (
      ConfirmAuthenticatorRequestDto dto,
      ConfirmAuthenticatorRequestValidator validator,
      IAuthContextService authContext,
      ITwoFactorService service,
      CancellationToken cancellationToken) =>
    {
      var validation = await validator.ValidateAsync(dto, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        return Results.BadRequest(new TwoFactorResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
      }

      var userPublicId = authContext.GetCurrentUserPublicId();
      if (userPublicId == null)
      {
        return Results.Json(new TwoFactorResponse(false, null, "Usuario no autenticado.", ErrorInfo.Status401Unauthorized), statusCode: ErrorInfo.Status401Unauthorized.StatusCode);
      }

      var result = await service.ConfirmAuthenticatorAsync(userPublicId.Value, dto.Code, cancellationToken);
      if (result.Success)
      {
        return Results.Ok(new TwoFactorResponse(true, result.Data));
      }

      return Results.Json(new TwoFactorResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .RequireAuthorization()
    .WithName("ConfirmAuthenticatorTwoFactor")
    .WithTags("Users")
    .WithSummary("Confirma el enrolamiento de Authenticator y activa 2FA.")
    .WithDescription("Valida el codigo de 6 digitos contra el secreto pendiente generado por /enroll.")
    .WithMetadata(new PermissionsNotRequiredAttribute("Endpoint self-service: requiere autenticacion, no permiso RBAC especifico"))
    .Produces<TwoFactorResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
    .WithMetadata(new ConfirmAuthenticatorTwoFactorOperationDocs());
  }

  private static void MapDisableEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/users/me/2fa/disable", async (
      DisableTwoFactorRequestDto dto,
      DisableTwoFactorRequestValidator validator,
      IAuthContextService authContext,
      ITwoFactorService service,
      CancellationToken cancellationToken) =>
    {
      var validation = await validator.ValidateAsync(dto, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        return Results.BadRequest(new TwoFactorResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
      }

      var userPublicId = authContext.GetCurrentUserPublicId();
      if (userPublicId == null)
      {
        return Results.Json(new TwoFactorResponse(false, null, "Usuario no autenticado.", ErrorInfo.Status401Unauthorized), statusCode: ErrorInfo.Status401Unauthorized.StatusCode);
      }

      var result = await service.DisableAsync(userPublicId.Value, dto.CurrentPassword, dto.Code, cancellationToken);
      if (result.Success)
      {
        return Results.Ok(new TwoFactorResponse(true, result.Data));
      }

      return Results.Json(new TwoFactorResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .RequireAuthorization()
    .WithName("DisableTwoFactor")
    .WithTags("Users")
    .WithSummary("Desactiva 2FA para el usuario autenticado, sin importar el metodo activo.")
    .WithDescription("Requiere confirmar la contrasena actual.")
    .WithMetadata(new PermissionsNotRequiredAttribute("Endpoint self-service: requiere autenticacion, no permiso RBAC especifico"))
    .Produces<TwoFactorResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .WithMetadata(new DisableTwoFactorOperationDocs());
  }
}
