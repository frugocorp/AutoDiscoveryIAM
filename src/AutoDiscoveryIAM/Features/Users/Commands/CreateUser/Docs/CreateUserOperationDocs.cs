// -----------------------------------------------------------------------
// <copyright file="CreateUserOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser.Docs;

/// <summary>
/// Datos de documentación de <c>CreateUserEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class CreateUserOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription =>
    "Usuario registrado. La cuenta queda pendiente de activación hasta confirmar el correo.";

  protected override object SuccessExample { get; } = new ApiResponse<CreateUserResponseDto>(
    true,
    new CreateUserResponseDto(
       Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
      "usuario-ejemplo@ejemplo.com",
      "EjemploDeUsuario",
      UserStatusCatalog.Pending),
    null,
    null);

  protected override object RequestExample { get; } = new CreateUserCommand(
      Username: "EjemploDeUsuario",
      Email: "usuario@ejemplo.com",
      Password: "Password*123!");

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status409Conflict.StatusCode] = ErrorInfo.Status409Conflict,
    [ErrorInfo.Status424Dependency.StatusCode] = ErrorInfo.Status424Dependency,
  };
}
