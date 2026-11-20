// -----------------------------------------------------------------------
// <copyright file="JwtSettings.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;

/// <summary>
/// <see cref="Key"/>/<see cref="Issuer"/>/<see cref="Audience"/> son propios de cada instalación --
/// sin default posible, validados al arranque por <see cref="JwtSettingsValidator"/>.
/// <see cref="ExpiryMinutes"/> sí tiene un valor razonable por si el equipo que instale el paquete
/// no lo configura.
/// </summary>
public class JwtSettings
{
  public string Key { get; set; } = string.Empty;
  public string Issuer { get; set; } = string.Empty;
  public string Audience { get; set; } = string.Empty;
  public int ExpiryMinutes { get; set; } = 15;

  /// <summary>
  /// Bytes de la clave de firma HMAC-SHA256, en UTF-8 tanto al firmar como al validar. ASCII
  /// convertiría cada carácter no ASCII en '?' y bajaría la entropía real de la clave.
  /// </summary>
  internal byte[] GetSigningKeyBytes() => Encoding.UTF8.GetBytes(Key);
}
