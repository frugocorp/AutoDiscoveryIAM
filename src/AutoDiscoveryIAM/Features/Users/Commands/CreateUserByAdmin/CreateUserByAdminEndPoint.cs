// -----------------------------------------------------------------------
// <copyright file="CreateUserByAdminEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.Provisioning;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;

/// <summary>
/// Endpoint de alta de usuarios por un administrador.
/// </summary>
public static class CreateUserByAdminEndPoint
{
  /// <summary>
  /// Mapea <c>POST /users/by-admin</c>.
  /// </summary>
  /// <param name="app">Constructor de rutas.</param>
  public static void MapCreateUserByAdminEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/users/by-admin", async (
        CreateUserByAdminRequestDto dto,
        IUserProvisioningService provisioning,
        CancellationToken cancellationToken) =>
    {
      // Mismo servicio que usa el código del consumidor: validación y alta en un solo camino.
      var result = await provisioning.CreateAsync(
        new ProvisionUserRequest(dto.Username, dto.Email, dto.Password, dto.Activate, dto.MustChangePassword),
        cancellationToken);
      if (result.Success)
      {
        var user = result.Data!;
        return Results.Ok(new CreateUserByAdminResponse(true, new CreateUserByAdminResponseDto(
          user.PublicId,
          user.Email,
          user.Username,
          user.Status,
          user.ActivationEmailSent,
          user.PasswordSetupEmailSent,
          user.MustChangePassword)));
      }

      return Results.Json(
        new CreateUserByAdminResponse(false, null, result.Error, result.ErrorInfo),
        statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .RequireAuthorization()
    .WithName("CreateUserByAdmin")
    .WithTags("Users")
    .WithSummary("Da de alta un usuario (administrador).")
    .WithDescription("Crea una cuenta local y le asigna el rol por defecto. Con password: activate=true la cuenta nace activa y no se envía correo; activate=false nace pendiente y el usuario recibe el correo de activación; el usuario debe cambiarla al entrar (hasta entonces responde 403 AUTH_PASSWORD_CHANGE_REQUIRED en los endpoints con permisos), salvo mustChangePassword=false. El correo queda sin verificar hasta que el usuario lo confirme. Sin password, la cuenta nace activa y el usuario recibe un solo correo para definir su contraseña (Registration:PasswordSetupLinkExpiryHours). Funciona aunque el registro público esté apagado. Un correo o nombre de usuario ya registrado responde 409.")
    .Accepts<CreateUserByAdminRequestDto>("application/json")
    .Produces<CreateUserByAdminResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status403Forbidden.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status424Dependency.StatusCode, "application/json")
    .WithMetadata(
        new RequirePermissionsAttribute("Alta de usuarios por un administrador."),
        new CreateUserByAdminOperationDocs());
  }
}
