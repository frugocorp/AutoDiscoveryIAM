// -----------------------------------------------------------------------
// <copyright file="GoogleLoginOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Social.Google.Docs;

/// <summary>
/// Datos de documentación de la ruta <c>GoogleLogin</c> (<c>GoogleAuthEndpoints</c>) para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class GoogleLoginOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status302Found;

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status503Unavailable.StatusCode] = ErrorInfo.Status503Unavailable,
  };
}
