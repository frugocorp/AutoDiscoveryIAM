// -----------------------------------------------------------------------
// <copyright file="RefreshTokenOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken.Docs;

/// <summary>
/// Datos de documentación de <c>RefreshTokenEndpoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class RefreshTokenOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new RefreshTokenResponse(
    true,
    new RefreshTokenResponseDto(
      "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "AbCdEf123456789...",
      "Bearer",
      3600,
      DateTime.UtcNow.AddHours(1)));

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
  };
}
