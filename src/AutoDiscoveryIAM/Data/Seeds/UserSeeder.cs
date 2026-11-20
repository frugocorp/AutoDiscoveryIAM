// -----------------------------------------------------------------------
// <copyright file="UserSeeder.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;

/// <summary>
/// Seeder para datos iniciales de la tabla Users.
/// </summary>
internal static class UserSeeder
{
  /// <summary>
  /// Inserta el usuario de auditoría del sistema (siempre) y el administrador de bootstrap
  /// admin@example.com: AdminPass123! con <c>IsAdmin=true</c> (solo en Development/Testing), en
  /// tiempo de ejecución vía el
  /// <see cref="AutoDiscoveryIAMDbContext"/> -- agnóstico de proveedor, invocable desde
  /// <c>SeedAutoDiscoveryIAMDefaultDataAsync()</c>. Solo inserta si no existe, preservando cambios del
  /// usuario. Ver README.md para credenciales de desarrollo.
  /// </summary>
  /// <remarks>
  /// SECURITY: el usuario "admin" con contraseña conocida solo se crea en Development/Testing,
  /// siguiendo el mismo patrón que <see cref="DevUserSeeder"/>. Un ambiente de producción debe
  /// crear su propio SuAdmin por un medio seguro fuera de este seeder.
  /// El usuario "system" (Id=1) debe existir siempre (muchos procesos lo usan como CreatedBy/UpdatedBy
  /// para auditoría), pero se crea con IsActive=0 y un hash Argon2id de una contraseña aleatoria
  /// descartada (nunca conocida por nadie) para que sea imposible usarlo como acceso real —
  /// LoginHandler rechaza cuentas inactivas incluso si alguien adivinara la contraseña. El motor
  /// asigna el Id que le corresponda, y <see cref="AutoDiscoveryIAMDbContext.ResolveCurrentAuditUserId"/>
  /// ya lo resuelve por nombre de usuario, no por Id fijo.
  /// </remarks>
  /// <summary>
  /// Datos del usuario admin de bootstrap (username, email, hash Argon2id).
  /// Única fuente de verdad de estas credenciales -- reusada también por
  /// <c>CustomWebApplicationFactory</c> en el proyecto de integration tests (vía
  /// <c>InternalsVisibleTo</c>) para no duplicar el hash en dos lugares.
  /// </summary>
  internal static readonly (string Username, string Email, string PasswordHash) AdminBootstrapUser =
    ("admin", "admin@example.com", "$argon2id$v=19$m=65536,t=3,p=1$imBP+dxp6AfePkAmjVfl9A$Ch3Yrq6VGEnwaO2rxwAM/FjzX5bBXrrKPGseqx9O6Ig"); // AdminPass123!

  public static async Task SeedDataAsync(AutoDiscoveryIAMDbContext dbContext, string environmentName, CancellationToken cancellationToken = default)
  {
    // IgnoreQueryFilters(): los índices únicos de Username/Email no distinguen filas borradas
    // suavemente, así que reinsertar un usuario ya existente (aunque esté borrado) chocaría contra
    // la restricción -- hay que detectarlo y reactivarlo en vez de reinsertarlo.
    var systemUser = await dbContext.Users
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(u => u.Username == AutoDiscoveryIAMDbContext.SystemUsername, cancellationToken);

    if (systemUser is null)
    {
      dbContext.Users.Add(new User
      {
        Username = AutoDiscoveryIAMDbContext.SystemUsername,
        Email = "system@example.com",
        // Hash Argon2id de una contraseña aleatoria descartada, nunca conocida por nadie.
        PasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$hVs0q3FqGeBemZJNbKcpeQ$eYUIluiJkODM6Tke3IyDzwPkWX5RP2rwu762Y+Q0yp8",
        IsEmailVerified = true,
        Status = UserStatusCatalog.Active,
        IsActive = false, // Nunca puede usarse para iniciar sesión real, ver comentario de clase.
      });

      await dbContext.SaveChangesAsync(cancellationToken);
    }
    else if (systemUser.IsDeleted)
    {
      systemUser.IsDeleted = false;
      await dbContext.SaveChangesAsync(cancellationToken);
    }

    if (!environmentName.Equals("Development", StringComparison.OrdinalIgnoreCase)
      && !environmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
    {
      return;
    }

    var adminUser = await dbContext.Users
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(u => u.Username == AdminBootstrapUser.Username, cancellationToken);

    if (adminUser is not null)
    {
      if (adminUser.IsDeleted)
      {
        adminUser.IsDeleted = false;
        await dbContext.SaveChangesAsync(cancellationToken);
      }

      return;
    }

    dbContext.Users.Add(new User
    {
      Username = AdminBootstrapUser.Username,
      Email = AdminBootstrapUser.Email,
      PasswordHash = AdminBootstrapUser.PasswordHash,
      IsEmailVerified = true,
      Status = UserStatusCatalog.Active,
      IsActive = true,
      IsAdmin = true,
    });

    await dbContext.SaveChangesAsync(cancellationToken);
  }
}
