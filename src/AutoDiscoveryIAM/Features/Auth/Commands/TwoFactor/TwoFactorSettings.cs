// -----------------------------------------------------------------------
// <copyright file="TwoFactorSettings.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;

/// <summary>
/// Configuración de autenticación de dos factores (email y Authenticator/TOTP).
/// </summary>
public sealed class TwoFactorSettings
{
  /// <summary>
  /// Longitud del código numérico de un solo uso enviado por email.
  /// </summary>
  public int CodeLength { get; set; } = 6;

  /// <summary>
  /// Minutos de vigencia del código enviado por email.
  /// </summary>
  public int CodeExpiryMinutes { get; set; } = 5;

  /// <summary>
  /// Minutos de vigencia del token de desafío emitido por LoginHandler cuando el usuario tiene 2FA activo.
  /// </summary>
  public int ChallengeTokenExpiryMinutes { get; set; } = 10;

  /// <summary>
  /// Nombre del emisor mostrado en apps Authenticator (Google/Microsoft) al enrolar TOTP.
  /// </summary>
  public string Issuer { get; set; } = "AutoDiscoveryIAM";
}
