// -----------------------------------------------------------------------
// <copyright file="Permission.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;

/// <summary>
/// Entidad Permission para el sistema RBAC. Define permisos específicos que pueden ser
/// asignados a roles. Soporta generación automática y trazabilidad completa.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Permission es el nombre correcto del dominio RBAC")]
public class Permission : AuditableEntity
{
  /// <summary>
  /// Identificador único interno (autoincremental).
  /// </summary>
  public long Id { get; set; }

  /// <summary>
  /// Clave única del permiso en formato {feature}.{action_snake_case}.
  /// Ejemplo: "users.create_user", "roles.assign_roles_to_user".
  /// </summary>
  public string Key { get; set; } = string.Empty;

  /// <summary>
  /// Descripción opcional del permiso y su propósito.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Grupo o categoría del permiso para organización.
  /// </summary>
  public string? Group { get; set; }

  /// <summary>
  /// Indica si el permiso está activo y puede ser asignado.
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Indica si el permiso fue generado automáticamente.
  /// </summary>
  public bool IsSystemGenerated { get; set; } = false;

  // Metadatos técnicos de descubrimiento y trazabilidad

  /// <summary>
  /// Ruta HTTP del endpoint (ej: "/users/{id}").
  /// </summary>
  public string? Route { get; set; }

  /// <summary>
  /// Método HTTP del endpoint (GET, POST, PUT, DELETE).
  /// </summary>
  public string? HttpMethod { get; set; }

  /// <summary>
  /// Namespace completo de la clase que define el endpoint.
  /// </summary>
  public string? FullNamespace { get; set; }

  /// <summary>
  /// Ruta completa del archivo fuente que define el endpoint.
  /// </summary>
  public string? SourceFilePath { get; set; }

  /// <summary>
  /// Nombre del método endpoint en el archivo fuente.
  /// </summary>
  public string? EndpointMethodName { get; set; }

  /// <summary>
  /// Tipo CQRS del endpoint (Command o Query).
  /// </summary>
  public string? CqrsType { get; set; }

  // Campos de auditoría para trazabilidad de cambios

  /// <summary>
  /// Clave anterior del permiso antes de un cambio.
  /// </summary>
  public string? PreviousKey { get; set; }

  /// <summary>
  /// Ruta del archivo anterior antes de un renombrado.
  /// </summary>
  public string? PreviousFilePath { get; set; }

  /// <summary>
  /// Nombre del método anterior antes de un cambio.
  /// </summary>
  public string? PreviousMethodName { get; set; }

  /// <summary>
  /// Fecha y hora de la última sincronización.
  /// </summary>
  public DateTime? LastSyncAt { get; set; }

  // 🆕 NUEVO: Metadatos de autorización almacenados

  /// <summary>
  /// Indica si el endpoint está marcado con [PermissionsNotRequired].
  /// </summary>
  public bool HasPermissionsNotRequired { get; set; } = false;

  /// <summary>
  /// Razón por la que el endpoint no requiere permisos.
  /// </summary>
  public string? PermissionsNotRequiredReason { get; set; }

  /// <summary>
  /// Indica si el endpoint requiere acceso de desarrollo.
  /// </summary>
  public bool RequiresDevelopmentAccess { get; set; } = false;

  /// <summary>
  /// Razón por la que el endpoint requiere acceso de desarrollo.
  /// </summary>
  public string? RequiresDevelopmentAccessReason { get; set; }

  /// <summary>
  /// Indica si el endpoint requiere acceso de super admin.
  /// </summary>
  public bool RequiresSuAdminAccess { get; set; } = false;

  /// <summary>
  /// Razón por la que el endpoint requiere acceso de super admin.
  /// </summary>
  public string? RequiresSuAdminAccessReason { get; set; }

  /// <summary>
  /// Indica si el endpoint tiene explícitamente [RequirePermissions].
  /// </summary>
  public bool HasRequirePermissions { get; set; } = false;

  /// <summary>
  /// Nota del atributo RequirePermissions.
  /// </summary>
  public string? RequirePermissionsNote { get; set; }

  // Relaciones

  /// <summary>
  /// Relación Many-to-Many con Role a través de RolePermission.
  /// </summary>
  public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
