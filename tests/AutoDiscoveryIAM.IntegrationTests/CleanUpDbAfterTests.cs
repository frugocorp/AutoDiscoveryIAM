// -----------------------------------------------------------------------
// <copyright file="CleanUpDbAfterTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests
{
  /// <summary>
  /// Limpieza de los datos que deja la suite de integración en una base de datos <b>real</b> (ver
  /// <see cref="CustomWebApplicationFactory{T}"/>, activado solo cuando <c>USE_REAL_DB_FOR_TESTS=true</c>).
  /// Con la base InMemory por defecto no hace falta: cada corrida usa una base descartable.
  /// </summary>
  /// <remarks>
  /// Usuarios y roles se limpian por <b>lista de conservación</b>: se borra todo lo que no sea del seed
  /// (usuario de sistema, admin de bootstrap y usuarios de desarrollo de la librería; roles
  /// <c>AdminAutoDiscoveryIAM</c> y <c>UserDefault</c>), sin importar el prefijo que use cada prueba.
  /// Así una prueba nueva no puede dejar basura por olvidar registrar su prefijo. Sus relaciones
  /// (roles asignados, refresh tokens, logins externos, solicitudes de cambio de email) se borran en
  /// cascada. Los permisos sí van por prefijo: los que crea el sync son legítimos y no se distinguen
  /// de otra forma.
  /// </remarks>
  internal static class CleanUpDbAfterTests
  {
    /// <summary>
    /// Usuarios del seed que se conservan (por <c>Username</c>, que las pruebas no cambian).
    /// </summary>
    internal static IReadOnlyList<string> SeedUsernames { get; } =
    [
      AutoDiscoveryIAMDbContext.SystemUsername,
      UserSeeder.AdminBootstrapUser.Username,
      .. DevUserSeeder.DevUsers.Select(u => u.Username),
    ];

    /// <summary>
    /// Roles del seed que se conservan.
    /// </summary>
    internal static IReadOnlyList<string> SeedRoleNames { get; } =
    [
      RoleSeeder.AdminAutoDiscoveryIamRoleName,
      RoleSeeder.UserDefaultRoleName,
    ];

    public static void CleanupTestData(AutoDiscoveryIAMDbContext db)
    {
      db.Database.ExecuteSqlRaw(CleanupSql.DeleteRefreshTokens);
      db.Database.ExecuteSqlRaw(CleanupSql.DeleteExternalLoginExchangeCodes);
      db.Database.ExecuteSqlRaw(CleanupSql.DeleteRolePermissionsByPermissions);
      db.Database.ExecuteSqlRaw(CleanupSql.DeletePermissions);
      DeleteAllExcept(db, "Roles", "Name", SeedRoleNames);
      DeleteAllExcept(db, "Users", "Username", SeedUsernames);
    }

    /// <summary>
    /// Falla si, después de la limpieza, queda algún usuario o rol fuera del seed: la limpieza no hizo
    /// su trabajo y las pruebas correrían sobre datos de corridas anteriores.
    /// </summary>
    public static void EnsureOnlySeedDataRemains(AutoDiscoveryIAMDbContext db)
    {
      var leftoverUsers = db.Users.IgnoreQueryFilters()
        .Where(u => !SeedUsernames.Contains(u.Username))
        .Select(u => u.Username)
        .Take(10)
        .ToList();
      var leftoverRoles = db.Roles.IgnoreQueryFilters()
        .Where(r => !SeedRoleNames.Contains(r.Name))
        .Select(r => r.Name)
        .Take(10)
        .ToList();

      if (leftoverUsers.Count > 0 || leftoverRoles.Count > 0)
      {
        throw new InvalidOperationException(
          $"La limpieza de la base de pruebas dejó datos fuera del seed. Usuarios: [{string.Join(", ", leftoverUsers)}]. Roles: [{string.Join(", ", leftoverRoles)}].");
      }
    }

    private static void DeleteAllExcept(AutoDiscoveryIAMDbContext db, string table, string column, IReadOnlyList<string> keep)
    {
      // Los nombres de tabla y columna son constantes de este archivo; los valores van como parámetros.
      var placeholders = string.Join(", ", keep.Select((_, i) => $"{{{i}}}"));
      var sql = $"DELETE FROM [{table}] WHERE [{column}] NOT IN ({placeholders});";
      db.Database.ExecuteSqlRaw(sql, keep.Cast<object>().ToArray());
    }

    private static class CleanupSql
    {
      public const string DeleteRolePermissionsByPermissions = @"
        DELETE rp
        FROM RolePermissions rp
        INNER JOIN Permissions p ON p.Id = rp.PermissionId
        WHERE
          p.[Key] LIKE 'roles.bulk_permission_%' OR
          p.[Key] LIKE 'roles.bulk_dryrun_permission_%' OR
          p.[Key] LIKE 'roles.create_permission_%' OR
          p.[Key] LIKE 'roles.delete_permission_%' OR
          p.[Key] LIKE 'roles.missing_delete_permission_%' OR
          p.[Key] LIKE 'roles.query.permissions.%' OR
          p.[Key] LIKE 'users.query.permissions.%' OR
          p.[Key] LIKE 'permissions.query.roles.%' OR
          p.[Key] LIKE 'permissions.query.users.%' OR
          p.[Key] LIKE 'permissions.query.inactive.%' OR
          p.[Key] LIKE 'clear.hidden.permission.%' OR
          p.[Key] LIKE 'users.read_me_permissions_%' OR
          p.[Key] LIKE 'b02.role.perms.%' OR
          p.[Key] LIKE 'perm.a.%' OR
          p.[Key] LIKE 'perm.b.%';";

      public const string DeletePermissions = @"
        DELETE FROM Permissions
        WHERE
          [Key] LIKE 'roles.bulk_permission_%' OR
          [Key] LIKE 'roles.bulk_dryrun_permission_%' OR
          [Key] LIKE 'roles.create_permission_%' OR
          [Key] LIKE 'roles.delete_permission_%' OR
          [Key] LIKE 'roles.missing_delete_permission_%' OR
          [Key] LIKE 'roles.query.permissions.%' OR
          [Key] LIKE 'users.query.permissions.%' OR
          [Key] LIKE 'permissions.query.roles.%' OR
          [Key] LIKE 'permissions.query.users.%' OR
          [Key] LIKE 'permissions.query.inactive.%' OR
          [Key] LIKE 'clear.hidden.permission.%' OR
          [Key] LIKE 'users.read_me_permissions_%' OR
          [Key] LIKE 'b02.role.perms.%' OR
          [Key] LIKE 'perm.a.%' OR
          [Key] LIKE 'perm.b.%';";

      public const string DeleteRefreshTokens = @"
        DELETE FROM RefreshTokens;";

      public const string DeleteExternalLoginExchangeCodes = @"
        DELETE FROM ExternalLoginExchangeCodes;";
    }
  }
}
