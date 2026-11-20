// -----------------------------------------------------------------------
// <copyright file="VerifyTwoFactorRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor;

/// <summary>
/// DTO para los datos requeridos en la verificación del segundo factor.
/// </summary>
public record VerifyTwoFactorRequestDto(string ChallengeToken, string Code)
{
  public VerifyTwoFactorCommand ToCommand(HttpContext context)
  {
    var ipAddress = context.Connection.RemoteIpAddress?.ToString();
    var userAgent = context.Request.Headers.UserAgent.ToString();

    return new VerifyTwoFactorCommand(ChallengeToken, Code, ipAddress, userAgent);
  }
}
