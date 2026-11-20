// -----------------------------------------------------------------------
// <copyright file="VerifyTwoFactorOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor.Docs;

/// <summary>
/// Datos de documentación de <c>VerifyTwoFactorEndpoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class VerifyTwoFactorOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new VerifyTwoFactorResponse(
    true,
    new LoginResponseDto(
      "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      DateTime.UtcNow.AddMinutes(60),
      "refresh-token-ejemplo-1234567890"));

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status409Conflict.StatusCode] = ErrorInfo.Status409Conflict,
  };
}
