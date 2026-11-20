// -----------------------------------------------------------------------
// <copyright file="EnrollAuthenticatorTwoFactorOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.TwoFactor.Docs;

public sealed class EnrollAuthenticatorTwoFactorOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new EnrollAuthenticatorResponse(
    true,
    new EnrollAuthenticatorResponseDto(
      "JBSWY3DPEHPK3PXP",
      "otpauth://totp/AutoDiscoveryIAM:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=AutoDiscoveryIAM&digits=6&period=30"));

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
  };
}
