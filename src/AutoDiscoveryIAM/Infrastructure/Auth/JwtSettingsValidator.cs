// -----------------------------------------------------------------------
// <copyright file="JwtSettingsValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;

/// <summary>
/// Valida <see cref="JwtSettings"/> al arranque (<c>ValidateOnStart</c>): <see cref="JwtSettings.Key"/>,
/// <see cref="JwtSettings.Issuer"/> y <see cref="JwtSettings.Audience"/> son propios de cada
/// instalación (propias de cada instalación) y no tienen un default razonable -- sin este validador, omitirlos no
/// rompe el arranque de la app, pero deja el login/la emisión de tokens silenciosamente rota (issuer/
/// audience vacíos en el token, o una clave de firma vacía) hasta el primer intento real de login.
/// </summary>
public class JwtSettingsValidator : IValidateOptions<JwtSettings>
{
  /// <summary>
  /// Longitud mínima de <see cref="JwtSettings.Key"/> en bytes UTF-8 (32 bytes = 256 bits):
  /// mínimo real recomendado por RFC 7518 para una clave HMAC-SHA256, no un valor arbitrario.
  /// </summary>
  private const int MinimumKeyLength = 32;

  /// <summary>
  /// Vida máxima del access token, en minutos. Un token no se puede revocar antes de expirar más que
  /// por logout o cambio de contraseña, así que una vida larga amplía la ventana de un token robado.
  /// </summary>
  private const int MaximumExpiryMinutes = 1440;

  /// <inheritdoc />
  public ValidateOptionsResult Validate(string? name, JwtSettings options)
  {
    ArgumentNullException.ThrowIfNull(options);

    var missing = new List<string>();
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(options.Key))
    {
      missing.Add(nameof(JwtSettings.Key));
    }
    else if (options.GetSigningKeyBytes().Length < MinimumKeyLength)
    {
      errors.Add($"JwtSettings:Key debe tener al menos {MinimumKeyLength} bytes (tiene {options.GetSigningKeyBytes().Length}) -- una clave corta es forzable por fuerza bruta offline y debilita la firma HMAC de todos los tokens emitidos.");
    }

    if (options.ExpiryMinutes < 1 || options.ExpiryMinutes > MaximumExpiryMinutes)
    {
      errors.Add($"JwtSettings:ExpiryMinutes debe estar entre 1 y {MaximumExpiryMinutes} (tiene {options.ExpiryMinutes}).");
    }

    if (string.IsNullOrWhiteSpace(options.Issuer))
    {
      missing.Add(nameof(JwtSettings.Issuer));
    }

    if (string.IsNullOrWhiteSpace(options.Audience))
    {
      missing.Add(nameof(JwtSettings.Audience));
    }

    if (missing.Count > 0)
    {
      errors.Add($"JwtSettings:{string.Join(", JwtSettings:", missing)} no {(missing.Count == 1 ? "está configurado" : "están configurados")} -- son propios de cada instalación, sin valor por default.");
    }

    return errors.Count == 0
      ? ValidateOptionsResult.Success
      : ValidateOptionsResult.Fail(errors);
  }
}
