// -----------------------------------------------------------------------
// <copyright file="RefreshTokenService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.Extensions.Options;
using RefreshTokenEntity = FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Configuración para RefreshTokenService.
/// </summary>
public class RefreshTokenSettings
{
  public const string SectionName = "RefreshTokenSettings";

  /// <summary>
  /// Tiempo de vida del refresh token en días. Default: 7 días.
  /// </summary>
  public int ExpiryDays { get; set; } = 7;

  /// <summary>
  /// Longitud del token en bytes. Default: 32 bytes (256 bits).
  /// </summary>
  public int TokenLength { get; set; } = 32;

  /// <summary>
  /// Máximo permitido para <see cref="ReuseGracePeriodSeconds"/>.
  /// </summary>
  public const int MaxReuseGracePeriodSeconds = 30;

  /// <summary>
  /// Ventana (segundos) en la que reusar un refresh token recién rotado devuelve el mismo par nuevo en
  /// vez de tratarse como robo y revocar toda la sesión: cubre el reintento por una respuesta perdida
  /// y dos refresh simultáneos desde el mismo cliente (misma IP y User-Agent que la rotación; desde otro
  /// cliente se trata como reuso). Default 0 (apagado). Máximo 30. Con varias instancias requiere un
  /// <c>IDistributedCache</c> y un anillo de claves de Data Protection compartidos.
  /// </summary>
  public int ReuseGracePeriodSeconds { get; set; }

  /// <summary>
  /// Dónde se entrega el refresh token: <c>Body</c> (default, en el JSON), <c>Cookie</c> (solo en una
  /// cookie HttpOnly, Secure) o <c>Both</c>. Con cookie, <c>POST /auth/refresh</c> y
  /// <c>POST /auth/logout</c> la leen si el cuerpo no trae el token.
  /// </summary>
  public RefreshTokenDeliveryMode Delivery { get; set; } = RefreshTokenDeliveryMode.Body;

  /// <summary>
  /// Atributos de la cookie cuando <see cref="Delivery"/> la usa.
  /// </summary>
  public RefreshTokenCookieSettings Cookie { get; set; } = new();
}

/// <summary>
/// Servicio para manejo de refresh tokens.
/// </summary>
public interface IRefreshTokenService
{
  RefreshTokenEntity GenerateRefreshToken(User user, string? ipAddress = null, string? userAgent = null);
  bool ValidateRefreshToken(RefreshTokenEntity token);
  string GenerateTokenString();
}

/// <summary>
/// Implementación del servicio de refresh tokens.
/// </summary>
public class RefreshTokenService(IOptions<RefreshTokenSettings> settings) : IRefreshTokenService
{
  private readonly RefreshTokenSettings _settings = settings.Value;

  /// <summary>
  /// Genera un nuevo refresh token para un usuario.
  /// </summary>
  public RefreshTokenEntity GenerateRefreshToken(User user, string? ipAddress = null, string? userAgent = null)
  {
    var tokenString = GenerateTokenString();

    return new RefreshTokenEntity
    {
      Token = RefreshTokenHasher.Hash(tokenString),
      PlaintextToken = tokenString,
      UserId = user.Id,
      ExpiresAt = TimeHelpers.GetCurrentMexicoTime().AddDays(_settings.ExpiryDays),
      IpAddress = ipAddress,
      UserAgent = userAgent,
      IsRevoked = false,
    };
  }

  /// <summary>
  /// Valida si un refresh token es válido (activo y no expirado).
  /// </summary>
  public bool ValidateRefreshToken(RefreshTokenEntity token)
  {
    return token.IsActive;
  }

  /// <summary>
  /// Genera una cadena de token segura usando criptografía.
  /// </summary>
  public string GenerateTokenString()
  {
    using var rng = RandomNumberGenerator.Create();
    var randomBytes = new byte[_settings.TokenLength];
    rng.GetBytes(randomBytes);
    return Convert.ToBase64String(randomBytes);
  }
}
