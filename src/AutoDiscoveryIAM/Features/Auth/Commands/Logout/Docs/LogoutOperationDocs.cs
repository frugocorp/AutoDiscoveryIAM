// -----------------------------------------------------------------------
// <copyright file="LogoutOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout.Docs;

/// <summary>
/// Datos de documentación de <c>LogoutEndpoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class LogoutOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new LogoutResponse(
    true,
    new LogoutResponseDto(
      "Logout exitoso. 1 token(s) invalidado(s).",
      1,
      DateTime.UtcNow));

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
  };
}
