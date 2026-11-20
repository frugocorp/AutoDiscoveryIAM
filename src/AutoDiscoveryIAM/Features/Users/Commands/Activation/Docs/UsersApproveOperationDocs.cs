// -----------------------------------------------------------------------
// <copyright file="UsersApproveOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation.Docs;

/// <summary>
/// Datos de documentación de la ruta <c>UsersApprove</c> (<c>ActivationEndPoint</c>) para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class UsersApproveOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new ActivationAdminResponse(
    true,
    new ActivationStatusDto(
      "usuario@example.com",
      false,
      "Correo de activacion reenviado correctamente.",
      "https://api.autodiscoveryiam.com/login"));

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.InsufficientPermissions.StatusCode] = ErrorInfo.InsufficientPermissions,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
  };
}
