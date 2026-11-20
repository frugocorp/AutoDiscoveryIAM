// -----------------------------------------------------------------------
// <copyright file="UserRole.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

/// <summary>
/// Entidad de unión Many-to-Many entre User y Role.
/// Define qué roles están asignados a cada usuario y cuándo.
/// </summary>
public class UserRole : AuditableEntity
{
  /// <summary>
  /// Identificador único interno (autoincremental).
  /// </summary>
  public long Id { get; set; }

  /// <summary>
  /// Identificador del usuario.
  /// </summary>
  public long UserId { get; set; }

  /// <summary>
  /// Identificador del rol.
  /// </summary>
  public long RoleId { get; set; }

  /// <summary>
  /// Fecha y hora cuando se asignó el rol al usuario.
  /// </summary>
  public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Fecha y hora opcional cuando expira la asignación del rol.
  /// </summary>
  public DateTime? ExpiresAt { get; set; }

  // Relaciones

  /// <summary>
  /// Usuario al que se asigna el rol.
  /// </summary>
  public virtual User User { get; set; } = null!;

  /// <summary>
  /// Rol que se asigna al usuario.
  /// </summary>
  public virtual Role Role { get; set; } = null!;
}
