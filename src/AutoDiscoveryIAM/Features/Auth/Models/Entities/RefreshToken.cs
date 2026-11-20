// -----------------------------------------------------------------------
// <copyright file="RefreshToken.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations.Schema;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;

/// <summary>
/// Entidad para manejar refresh tokens con rotación automática y invalidación.
/// </summary>
public class RefreshToken : AuditableEntity
{
  public long Id { get; set; }

  /// <summary>
  /// Hash (SHA-256) del token de renovación. No se persiste el valor en claro.
  /// </summary>
  public string Token { get; set; } = string.Empty;

  /// <summary>
  /// Valor en claro del token, solo disponible en memoria al momento de emitirlo
  /// (login/refresh); no se persiste en BD.
  /// </summary>
  [NotMapped]
  public string? PlaintextToken { get; set; }

  /// <summary>
  /// ID del usuario propietario del token.
  /// </summary>
  public long UserId { get; set; }

  /// <summary>
  /// Fecha y hora de expiración del refresh token.
  /// </summary>
  public DateTime ExpiresAt { get; set; }

  /// <summary>
  /// Indica si el token ha sido revocado/invalidado.
  /// </summary>
  public bool IsRevoked { get; set; } = false;

  /// <summary>
  /// Fecha y hora en que se revocó el token (si aplica).
  /// </summary>
  public DateTime? RevokedAt { get; set; }

  /// <summary>
  /// Dirección IP desde donde se generó el token.
  /// </summary>
  public string? IpAddress { get; set; }

  /// <summary>
  /// User-Agent del cliente que generó el token.
  /// </summary>
  public string? UserAgent { get; set; }

  /// <summary>
  /// Token que reemplazó a este (para trazabilidad en rotación).
  /// </summary>
  public string? ReplacedByToken { get; set; }

  /// <summary>
  /// Razón de la revocación del token.
  /// </summary>
  public string? RevocationReason { get; set; }

  /// <summary>
  /// Token de concurrencia optimista — sin esto, dos peticiones /auth/refresh
  /// realmente simultáneas con el mismo token podían leer IsRevoked=false ambas antes de que
  /// cualquiera guardara, y las dos terminaban rotando el token con éxito (bypass de la
  /// detección de reuso). Con RowVersion, la segunda en guardar falla con
  /// DbUpdateConcurrencyException.
  /// </summary>
  public byte[] RowVersion { get; set; } = Array.Empty<byte>();

  // Navegación
  public virtual User User { get; set; } = null!;

  /// <summary>
  /// Verifica si el token está activo (no expirado y no revocado).
  /// </summary>
  public bool IsActive => !IsRevoked && TimeHelpers.GetCurrentMexicoTime() < ExpiresAt;

  /// <summary>
  /// Verifica si el token ha expirado.
  /// </summary>
  public bool IsExpired => TimeHelpers.GetCurrentMexicoTime() >= ExpiresAt;
}
