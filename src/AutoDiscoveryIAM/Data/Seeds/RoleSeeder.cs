// -----------------------------------------------------------------------
// <copyright file="RoleSeeder.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;

/// <summary>
/// Seeder para datos iniciales de la tabla Roles.
/// </summary>
internal static class RoleSeeder
{
  /// <summary>
  /// Nombre del rol de administración del propio package, compartido con
  /// <see cref="RolePermissionSeeder"/>/<see cref="UserRoleSeeder"/>. Tiene todos los permisos del
  /// package (roles, permisos, usuarios) -- un fork puede libremente crear su propio rol "Admin" de
  /// la app final, separado de este.
  /// </summary>
  public const string AdminAutoDiscoveryIamRoleName = "AdminAutoDiscoveryIAM";

  /// <summary>
  /// Nombre del rol que se asigna automáticamente a todo usuario auto-registrado (local o por login
  /// externo) cuando ningún admin interviene. Nace sin ningún permiso -- el equipo dev le agrega los
  /// que su app necesite. Puede eliminarse o renombrarse libremente; si se reemplaza por un rol
  /// propio, basta con marcar ese rol con <see cref="Role.IsDefaultForNewUser"/> en vez de este.
  /// </summary>
  public const string UserDefaultRoleName = "UserDefault";

  /// <summary>
  /// Inserta los roles base del sistema en tiempo de ejecución, vía el
  /// <see cref="AutoDiscoveryIAMDbContext"/> -- agnóstico de proveedor, invocable desde
  /// <c>SeedAutoDiscoveryIAMDefaultDataAsync()</c>. Solo inserta lo que no exista todavía,
  /// preservando cambios del usuario. Si un rol ya existe pero fue borrado suave o desactivado, lo
  /// reactiva en vez de reinsertarlo -- el índice único de <c>Name</c> no distingue filas borradas
  /// suavemente.
  /// </summary>
  public static async Task SeedDataAsync(AutoDiscoveryIAMDbContext dbContext, CancellationToken cancellationToken = default)
  {
    await UpsertRoleAsync(dbContext, AdminAutoDiscoveryIamRoleName, "Administración del propio package AutoDiscoveryIAM (roles, permisos, usuarios).", isDefaultForNewUser: false, cancellationToken);
    await UpsertRoleAsync(dbContext, UserDefaultRoleName, "Rol asignado automáticamente a todo usuario auto-registrado. Nace sin permisos.", isDefaultForNewUser: true, cancellationToken);
  }

  private static async Task UpsertRoleAsync(AutoDiscoveryIAMDbContext dbContext, string name, string description, bool isDefaultForNewUser, CancellationToken cancellationToken)
  {
    var existing = await dbContext.Roles
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    if (existing is not null)
    {
      if (existing.IsDeleted || !existing.IsActive)
      {
        existing.IsDeleted = false;
        existing.IsActive = true;
        await dbContext.SaveChangesAsync(cancellationToken);
      }

      return;
    }

    dbContext.Roles.Add(new Role
    {
      Name = name,
      Description = description,
      IsActive = true,
      IsDefaultForNewUser = isDefaultForNewUser,
    });

    await dbContext.SaveChangesAsync(cancellationToken);
  }
}
