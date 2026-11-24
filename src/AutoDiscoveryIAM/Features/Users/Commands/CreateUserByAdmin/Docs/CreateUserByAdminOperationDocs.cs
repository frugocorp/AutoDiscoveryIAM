// -----------------------------------------------------------------------
// <copyright file="CreateUserByAdminOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin.Docs;

/// <summary>
/// Datos de documentación de <c>CreateUserByAdminEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class CreateUserByAdminOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription =>
    "Usuario creado con el rol por defecto: activo, pendiente de activación por correo o invitado a definir su contraseña.";

  protected override object SuccessExample { get; } = new ApiResponse<CreateUserByAdminResponseDto>(
    true,
    new CreateUserByAdminResponseDto(
      Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
      "usuario@ejemplo.com",
      "EjemploDeUsuario",
      UserStatusCatalog.Active,
      ActivationEmailSent: false,
      PasswordSetupEmailSent: false,
      MustChangePassword: true),
    null,
    null);

  protected override object RequestExample { get; } = new CreateUserByAdminRequestDto(
    Username: "EjemploDeUsuario",
    Email: "usuario@ejemplo.com",
    Password: "Password*123!",
    Activate: true,
    MustChangePassword: true);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status403Forbidden.StatusCode] = ErrorInfo.Status403Forbidden,
    [ErrorInfo.Status409Conflict.StatusCode] = ErrorInfo.Status409Conflict,
    [ErrorInfo.Status424Dependency.StatusCode] = ErrorInfo.Status424Dependency,
  };
}
