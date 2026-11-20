// -----------------------------------------------------------------------
// <copyright file="UserRoleSeeder.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;

/// <summary>
/// Seeder para datos iniciales de la tabla UserRoles.
/// </summary>
internal static class UserRoleSeeder
{
  /// <summary>
  /// Asigna el rol <see cref="RoleSeeder.AdminAutoDiscoveryIamRoleName"/> al usuario admin de
  /// bootstrap, solo en Development/Testing (mismo gate que <see cref="UserSeeder.SeedDataAsync"/> --
  /// ese usuario no existe fuera de esos ambientes), en tiempo de ejecución vía el
  /// <see cref="AutoDiscoveryIAMDbContext"/> -- agnóstico de proveedor, invocable desde
  /// <c>SeedAutoDiscoveryIAMDefaultDataAsync()</c>. Solo inserta si no existe.
  /// </summary>
  public static async Task SeedDataAsync(AutoDiscoveryIAMDbContext dbContext, string environmentName, CancellationToken cancellationToken = default)
  {
    if (environmentName is not ("Development" or "Testing"))
    {
      return;
    }

    var adminUserId = await dbContext.Users
      .Where(u => u.Username == "admin")
      .Select(u => u.Id)
      .FirstOrDefaultAsync(cancellationToken);

    var adminRoleId = await dbContext.Roles
      .Where(r => r.Name == RoleSeeder.AdminAutoDiscoveryIamRoleName)
      .Select(r => r.Id)
      .FirstOrDefaultAsync(cancellationToken);

    if (adminUserId == 0 || adminRoleId == 0)
    {
      return;
    }

    // IgnoreQueryFilters(): el índice único de (UserId, RoleId) no distingue filas borradas
    // suavemente, así que reinsertar una asignación ya existente (aunque esté borrada) chocaría
    // contra la restricción -- hay que detectarla y reactivarla en vez de reinsertarla.
    var existing = await dbContext.UserRoles
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(ur => ur.UserId == adminUserId && ur.RoleId == adminRoleId, cancellationToken);

    if (existing is not null)
    {
      if (existing.IsDeleted)
      {
        existing.IsDeleted = false;
        await dbContext.SaveChangesAsync(cancellationToken);
      }

      return;
    }

    dbContext.UserRoles.Add(new Features.Users.Models.Entities.UserRole
    {
      UserId = adminUserId,
      RoleId = adminRoleId,
      AssignedAt = DateTime.UtcNow,
    });

    await dbContext.SaveChangesAsync(cancellationToken);
  }
}
