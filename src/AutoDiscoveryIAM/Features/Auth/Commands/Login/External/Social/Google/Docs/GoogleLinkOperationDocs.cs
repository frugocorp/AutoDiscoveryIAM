// -----------------------------------------------------------------------
// <copyright file="GoogleLinkOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Social.Google.Docs;

/// <summary>
/// Datos de documentación de la ruta <c>GoogleLink</c> (<c>GoogleAuthEndpoints</c>) para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class GoogleLinkOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new ApiResponse<ExternalLinkOperationResult>(
    true,
    new ExternalLinkOperationResult("Cuenta Google vinculada correctamente."),
    null,
    null);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status409Conflict.StatusCode] = ErrorInfo.Status409Conflict,
    [ErrorInfo.Status424Dependency.StatusCode] = ErrorInfo.Status424Dependency,
  };
}
