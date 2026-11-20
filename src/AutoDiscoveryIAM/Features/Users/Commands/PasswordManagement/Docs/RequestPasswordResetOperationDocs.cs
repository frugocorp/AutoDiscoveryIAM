// -----------------------------------------------------------------------
// <copyright file="RequestPasswordResetOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement.Docs;

/// <summary>
/// Datos de documentación de la ruta "RequestPasswordReset" de <c>PasswordManagementEndPoint</c>
/// para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class RequestPasswordResetOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new PasswordManagementResponse(
    true,
    new PasswordManagementStatusDto("Contraseña actualizada correctamente."));

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
  };
}
