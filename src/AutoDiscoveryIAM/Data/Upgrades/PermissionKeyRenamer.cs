// -----------------------------------------------------------------------
// <copyright file="PermissionKeyRenamer.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Upgrades;

/// <summary>
/// Ayudante para pasos de actualización que renombran un slice de la librería: cambia la clave de un
/// permiso sin que los roles pierdan su asignación. Sin esto, el sync marcaría la clave anterior como
/// obsoleta y crearía una nueva sin asignar, y los usuarios perderían acceso sin aviso.
/// </summary>
internal static class PermissionKeyRenamer
{
  /// <summary>
  /// Renombra <paramref name="oldKey"/> a <paramref name="newKey"/>. Si la clave nueva ya existe (por
  /// ejemplo, porque alguien sincronizó antes de actualizar), mueve a ella las asignaciones de la
  /// anterior y desactiva la anterior. No hace nada si la clave anterior no existe (base nueva).
  /// </summary>
  /// <param name="dbContext">Contexto de la librería.</param>
  /// <param name="oldKey">Clave actual.</param>
  /// <param name="newKey">Clave nueva.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>Tarea de la operación; el ejecutor guarda los cambios.</returns>
  public static async Task RenameAsync(AutoDiscoveryIAMDbContext dbContext, string oldKey, string newKey, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(dbContext);
    ArgumentException.ThrowIfNullOrWhiteSpace(oldKey);
    ArgumentException.ThrowIfNullOrWhiteSpace(newKey);

    var permissions = await dbContext.Permissions
      .IgnoreQueryFilters()
      .Where(permission => permission.Key == oldKey || permission.Key == newKey)
      .ToListAsync(cancellationToken);

    var oldPermission = permissions.FirstOrDefault(permission => permission.Key == oldKey);
    if (oldPermission is null)
    {
      return;
    }

    var newPermission = permissions.FirstOrDefault(permission => permission.Key == newKey);
    if (newPermission is null)
    {
      oldPermission.PreviousKey = oldKey;
      oldPermission.Key = newKey;
      return;
    }

    await MoveAssignmentsAsync(dbContext, oldPermission.Id, newPermission.Id, cancellationToken);

    newPermission.IsActive = true;
    newPermission.PreviousKey ??= oldKey;
    oldPermission.IsActive = false;
  }

  /// <summary>
  /// Mueve las asignaciones activas de un permiso a otro: crea (o reactiva) la del permiso nuevo en
  /// cada rol que aún no la tenga y borra suavemente la del permiso anterior.
  /// </summary>
  private static async Task MoveAssignmentsAsync(AutoDiscoveryIAMDbContext dbContext, long oldPermissionId, long newPermissionId, CancellationToken cancellationToken)
  {
    var assignments = await dbContext.RolePermissions
      .IgnoreQueryFilters()
      .Where(rolePermission => rolePermission.PermissionId == oldPermissionId || rolePermission.PermissionId == newPermissionId)
      .ToListAsync(cancellationToken);

    var rolesWithNewKey = assignments
      .Where(rolePermission => rolePermission.PermissionId == newPermissionId && !rolePermission.IsDeleted)
      .Select(rolePermission => rolePermission.RoleId)
      .ToHashSet();

    foreach (var assignment in assignments.Where(rolePermission => rolePermission.PermissionId == oldPermissionId && !rolePermission.IsDeleted))
    {
      if (!rolesWithNewKey.Contains(assignment.RoleId))
      {
        var existingDeleted = assignments.FirstOrDefault(rolePermission => rolePermission.PermissionId == newPermissionId && rolePermission.RoleId == assignment.RoleId);
        if (existingDeleted is not null)
        {
          existingDeleted.IsDeleted = false;
        }
        else
        {
          dbContext.RolePermissions.Add(new() { RoleId = assignment.RoleId, PermissionId = newPermissionId });
        }
      }

      assignment.IsDeleted = true;
    }
  }
}
