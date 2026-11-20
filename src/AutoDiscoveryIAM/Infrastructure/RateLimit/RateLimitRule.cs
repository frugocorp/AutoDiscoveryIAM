// -----------------------------------------------------------------------
// <copyright file="RateLimitRule.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;

/// <summary>
/// Una regla de límite de peticiones por IP del cliente (elemento de <c>RateLimiting:Rules</c>):
/// como máximo <see cref="Limit"/> peticiones a <see cref="Endpoint"/> en cada ventana fija de
/// <see cref="Period"/>.
/// </summary>
public sealed class RateLimitRule
{
  /// <summary>
  /// Rutas a las que aplica: <c>*</c> (todas, un solo contador por IP) o <c>metodo:/ruta</c>, donde
  /// el método puede ser <c>*</c> y la ruta admite <c>*</c> como comodín (ej. <c>post:/auth/login*</c>).
  /// Sin distinguir mayúsculas.
  /// </summary>
  public string Endpoint { get; set; } = "*";

  /// <summary>
  /// Duración de la ventana: un número seguido de <c>s</c>, <c>m</c>, <c>h</c> o <c>d</c>
  /// (ej. <c>30s</c>, <c>1m</c>, <c>1h</c>, <c>1d</c>).
  /// </summary>
  public string Period { get; set; } = "1m";

  /// <summary>
  /// Peticiones permitidas por ventana. Debe ser mayor que cero.
  /// </summary>
  public int Limit { get; set; }

  /// <summary>
  /// Interpreta <paramref name="period"/> (ver <see cref="Period"/>).
  /// </summary>
  internal static bool TryParsePeriod(string? period, out TimeSpan window)
  {
    window = TimeSpan.Zero;
    if (string.IsNullOrWhiteSpace(period) || period.Length < 2)
    {
      return false;
    }

    var trimmed = period.Trim();
    if (!int.TryParse(trimmed.AsSpan(0, trimmed.Length - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
    {
      return false;
    }

    window = char.ToLowerInvariant(trimmed[^1]) switch
    {
      's' => TimeSpan.FromSeconds(amount),
      'm' => TimeSpan.FromMinutes(amount),
      'h' => TimeSpan.FromHours(amount),
      'd' => TimeSpan.FromDays(amount),
      _ => TimeSpan.Zero,
    };

    return window > TimeSpan.Zero;
  }
}
