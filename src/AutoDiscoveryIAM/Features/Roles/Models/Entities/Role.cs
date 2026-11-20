// -----------------------------------------------------------------------
// <copyright file="Role.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;

/// <summary>
/// Entidad Role para el sistema RBAC. Define roles que pueden ser asignados a usuarios
/// y que agrupan permisos específicos.
/// </summary>
public class Role : AuditableEntity
{
  /// <summary>
  /// Identificador único interno (autoincremental).
  /// </summary>
  public long Id { get; set; }

  /// <summary>
  /// Nombre único del rol (ej: "Admin", "User", "Moderator").
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Descripción opcional del rol y sus responsabilidades.
  /// </summary>
  public string? Description { get; set; }

  /// <summary>
  /// Indica si el rol está activo y puede ser asignado a usuarios.
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Indica si este es el rol que se asigna automáticamente a un usuario recién auto-registrado
  /// (registro local o alta por login externo) cuando no hay ningún admin de por medio. Como mucho
  /// una fila puede tenerlo en <c>true</c> (índice único filtrado, ver <see cref="RoleConfiguration"/>).
  /// Deliberadamente no existe ningún endpoint para marcar/desmarcar este flag -- si un admin lo
  /// activa por error sobre el rol equivocado, todo usuario que se auto-registre a partir de ese
  /// momento hereda esos permisos. Se marca por acceso directo a la base de datos (mismo criterio
  /// que la asignación del primer <c>IsAdmin</c>/<c>IsDevelopment</c>/<c>IsSuAdmin</c>).
  /// Un rol con este flag en <c>true</c> nunca puede tener asignado, vía <c>RolePermission</c>, un
  /// permiso cuyo endpoint esté marcado <c>[RequiresDevelopmentAccess]</c> o
  /// <c>[RequiresSuAdminAccess]</c> -- esos atributos se evalúan contra los flags del propio
  /// <c>User</c> (<c>IsAdmin</c>/<c>IsDevelopment</c>/<c>IsSuAdmin</c>), nunca contra rol/permiso, así
  /// que asignarlos aquí sería inerte hoy y un riesgo latente si esa evaluación cambia a futuro.
  /// </summary>
  public bool IsDefaultForNewUser { get; set; } = false;

  /// <summary>
  /// Token de concurrencia optimista — SQL Server lo actualiza automáticamente en
  /// cada UPDATE; un update concurrente sobre una fila desactualizada falla con 409 en vez de
  /// pisar silenciosamente los cambios de otro usuario.
  /// </summary>
  public byte[] RowVersion { get; set; } = Array.Empty<byte>();

  // Relaciones Many-to-Many

  /// <summary>
  /// Relación Many-to-Many con User a través de UserRole.
  /// </summary>
  public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

  /// <summary>
  /// Relación Many-to-Many con Permission a través de RolePermission.
  /// </summary>
  public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
