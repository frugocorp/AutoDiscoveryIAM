// -----------------------------------------------------------------------
// <copyright file="RolePermissionSeeder.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;

/// <summary>
/// Seeder para datos iniciales de la tabla RolePermissions.
/// </summary>
internal static class RolePermissionSeeder
{
  /// <summary>
  /// Asigna todos los permisos esenciales (<see cref="PermissionSeeder.DefaultPermissions"/>) al rol
  /// <see cref="RoleSeeder.AdminAutoDiscoveryIamRoleName"/> en tiempo de ejecución, vía el
  /// <see cref="AutoDiscoveryIAMDbContext"/> -- agnóstico de proveedor, invocable desde
  /// <c>SeedAutoDiscoveryIAMDefaultDataAsync()</c>. Solo inserta las asignaciones que no existen.
  /// Requiere que <see cref="RoleSeeder.SeedDataAsync"/> y <see cref="PermissionSeeder.SeedDataAsync"/>
  /// ya hayan corrido. <see cref="RoleSeeder.UserDefaultRoleName"/> no recibe ningún permiso aquí --
  /// nace vacío, el equipo dev le agrega los que su app necesite.
  /// </summary>
  public static async Task SeedDataAsync(AutoDiscoveryIAMDbContext dbContext, CancellationToken cancellationToken = default)
  {
    var adminRoleId = await dbContext.Roles
      .Where(r => r.Name == RoleSeeder.AdminAutoDiscoveryIamRoleName)
      .Select(r => r.Id)
      .FirstOrDefaultAsync(cancellationToken);

    if (adminRoleId == 0)
    {
      return;
    }

    var essentialKeys = PermissionSeeder.DefaultPermissions.Select(p => p.Key).ToArray();

    var permissionIds = await dbContext.Permissions
      .Where(p => essentialKeys.Contains(p.Key))
      .Select(p => p.Id)
      .ToListAsync(cancellationToken);

    // IgnoreQueryFilters(): el índice único de (RoleId, PermissionId) no distingue filas borradas
    // suavemente, así que reinsertar una asignación ya existente (aunque esté borrada) chocaría
    // contra la restricción -- hay que detectarla y reactivarla en vez de reinsertarla.
    var existingAssignments = (await dbContext.RolePermissions
      .IgnoreQueryFilters()
      .Where(rp => rp.RoleId == adminRoleId && permissionIds.Contains(rp.PermissionId))
      .ToListAsync(cancellationToken))
      .ToDictionary(rp => rp.PermissionId);

    var hasChanges = false;
    foreach (var permissionId in permissionIds)
    {
      if (existingAssignments.TryGetValue(permissionId, out var existing))
      {
        if (existing.IsDeleted)
        {
          existing.IsDeleted = false;
          hasChanges = true;
        }

        continue;
      }

      dbContext.RolePermissions.Add(new RolePermission
      {
        RoleId = adminRoleId,
        PermissionId = permissionId,
        AssignedAt = DateTime.UtcNow,
      });
      hasChanges = true;
    }

    if (!hasChanges)
    {
      return;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
  }
}
