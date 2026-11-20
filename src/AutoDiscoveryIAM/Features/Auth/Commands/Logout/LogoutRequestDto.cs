// -----------------------------------------------------------------------
// <copyright file="LogoutRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.IdentityModel.Tokens.Jwt;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;

/// <summary>
/// DTO para los datos requeridos en Logout.
/// </summary>
/// <param name="RefreshToken">Token de renovación a invalidar (opcional si está autenticado).</param>
/// <param name="RevokeAllTokens">Si debe revocar todos los tokens del usuario. Default: false.</param>
public record LogoutRequestDto(
  string? RefreshToken = null,
  bool RevokeAllTokens = false)
{
  /// <summary>
  /// Convierte el DTO a Command incluyendo información de contexto HTTP.
  /// </summary>
  public LogoutCommand ToCommand(HttpContext context, long? userId = null)
  {
    var ipAddress = context.Connection.RemoteIpAddress?.ToString();
    var userAgent = context.Request.Headers.UserAgent.ToString();
    var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    var expClaim = context.User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
    DateTime? expiresAtUtc = long.TryParse(expClaim, out var expSeconds)
      ? DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime
      : null;

    return new LogoutCommand(
      RefreshToken,
      userId,
      RevokeAllTokens,
      ipAddress,
      userAgent,
      jti,
      expiresAtUtc);
  }
}
