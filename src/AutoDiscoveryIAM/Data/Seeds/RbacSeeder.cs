// -----------------------------------------------------------------------
// <copyright file="RbacSeeder.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;

/// <summary>
/// Seeder principal para el sistema RBAC. Coordina la siembra de todos los datos iniciales.
/// </summary>
internal static class RbacSeeder
{
  /// <summary>
  /// Ejecuta la siembra completa del sistema RBAC en tiempo de ejecución, vía el
  /// <see cref="AutoDiscoveryIAMDbContext"/> -- agnóstico de proveedor de base de datos. Invocable
  /// directamente desde la extensión <c>app.SeedAutoDiscoveryIAMDefaultDataAsync()</c>
  /// (<c>Data/Seeds/AutoDiscoveryIAMSeedingExtensions.cs</c>). Idempotente: cada seeder individual solo
  /// inserta lo que no exista todavía.
  /// </summary>
  /// <param name="dbContext">DbContext ya resuelto (típicamente desde un scope de DI al arrancar la app).</param>
  /// <param name="environmentName">Nombre del ambiente actual (<c>IHostEnvironment.EnvironmentName</c>) -- gatea los usuarios de desarrollo/bootstrap.</param>
  /// <param name="includeDevelopmentUsers">
  /// Opt-in explícito del consumidor para sembrar las cuentas con contraseña conocida. Sin él no se
  /// siembran aunque el ambiente sea Development/Testing: un despliegue real con
  /// <c>ASPNETCORE_ENVIRONMENT=Development</c> por descuido no debe nacer con esas cuentas.
  /// </param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  public static async Task SeedAllAsync(AutoDiscoveryIAMDbContext dbContext, string environmentName, bool includeDevelopmentUsers, CancellationToken cancellationToken = default)
  {
    // Cada seeder de usuarios conserva su propio filtro por ambiente; sin opt-in se le pasa
    // Production, que siembra solo lo que existe en todo ambiente (el usuario de sistema).
    var developmentUsersEnvironment = includeDevelopmentUsers ? environmentName : Environments.Production;

    // Orden importante: primero entidades principales, luego relaciones -- cada paso hace su propio
    // SaveChangesAsync, así que los Id generados ya están disponibles para el siguiente paso.
    await RoleSeeder.SeedDataAsync(dbContext, cancellationToken);
    await PermissionSeeder.SeedDataAsync(dbContext, cancellationToken);
    await UserSeeder.SeedDataAsync(dbContext, developmentUsersEnvironment, cancellationToken);
    await DevUserSeeder.SeedDataAsync(dbContext, developmentUsersEnvironment, cancellationToken);
    await RolePermissionSeeder.SeedDataAsync(dbContext, cancellationToken);
    await UserRoleSeeder.SeedDataAsync(dbContext, developmentUsersEnvironment, cancellationToken);
  }

  /// <summary>
  /// Emails de las cuentas con contraseña conocida (admin de bootstrap y usuarios de desarrollo)
  /// que siguen activas en la base de datos.
  /// </summary>
  internal static async Task<IReadOnlyList<string>> FindActiveKnownPasswordAccountsAsync(AutoDiscoveryIAMDbContext dbContext, CancellationToken cancellationToken = default)
  {
    var emails = DevUserSeeder.DevUsers.Select(u => u.Email).Append(UserSeeder.AdminBootstrapUser.Email).ToArray();

    return await dbContext.Users
      .Where(u => u.IsActive && emails.Contains(u.Email))
      .Select(u => u.Email)
      .ToListAsync(cancellationToken);
  }
}
