// -----------------------------------------------------------------------
// <copyright file="RolePermission.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;

/// <summary>
/// Entidad de unión Many-to-Many entre Role y Permission.
/// Define qué permisos están asignados a cada rol y cuándo.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "RolePermission es el nombre correcto del dominio RBAC")]
public class RolePermission : AuditableEntity
{
  /// <summary>
  /// Identificador único interno (autoincremental).
  /// </summary>
  public long Id { get; set; }

  /// <summary>
  /// Identificador del rol.
  /// </summary>
  public long RoleId { get; set; }

  /// <summary>
  /// Identificador del permiso.
  /// </summary>
  public long PermissionId { get; set; }

  /// <summary>
  /// Fecha y hora cuando se asignó el permiso al rol.
  /// </summary>
  public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

  // Relaciones

  /// <summary>
  /// Rol al que se asigna el permiso.
  /// </summary>
  public virtual Role Role { get; set; } = null!;

  /// <summary>
  /// Permiso que se asigna al rol.
  /// </summary>
  public virtual Permission Permission { get; set; } = null!;
}
