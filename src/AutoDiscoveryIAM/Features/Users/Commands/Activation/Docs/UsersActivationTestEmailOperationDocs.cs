// -----------------------------------------------------------------------
// <copyright file="UsersActivationTestEmailOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation.Docs;

/// <summary>
/// Datos de documentación de la ruta <c>UsersActivationTestEmail</c> (<c>ActivationEndPoint</c>) para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class UsersActivationTestEmailOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new ApiResponse<ActivationTestEmailResponseDto>(
    true,
    new ActivationTestEmailResponseDto("Correo de prueba enviado."));

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.SuperAdminAccessRequired.StatusCode] = ErrorInfo.SuperAdminAccessRequired,
    [ErrorInfo.Status424Dependency.StatusCode] = ErrorInfo.Status424Dependency,
  };
}
