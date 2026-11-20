// -----------------------------------------------------------------------
// <copyright file="CleanUpDbAfterTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests
{
  /// <summary>
  /// Limpieza best-effort de los datos de prueba dejados en una base de datos <b>real</b> por la
  /// suite de integración (ver <see cref="CustomWebApplicationFactory{T}"/>, activado solo cuando
  /// <c>USE_REAL_DB_FOR_TESTS=true</c>). Con la base InMemory por defecto no hace falta -- cada
  /// corrida usa una base descartable, sin estado que arrastrar entre ejecuciones.
  /// </summary>
  /// <remarks>
  /// Los patrones <c>LIKE</c> cubren los prefijos de usuario/rol/permiso que usan los distintos
  /// tests de integración. Si agregas un test nuevo con un prefijo de dato de prueba nuevo,
  /// agrégalo también aquí -- de lo contrario sus filas quedan huérfanas en la base real entre
  /// corridas, acumulándose indefinidamente.
  /// </remarks>
  internal static class CleanUpDbAfterTests
  {
    public static void CleanupTestData(AutoDiscoveryIAMDbContext db)
    {
      CleanupRefreshTokens(db);
      CleanupExternalLogins(db);
      CleanupEmailChangeRequests(db);
      CleanupUserRolesByUserPatterns(db);
      CleanupUserRolesByRolePatterns(db);
      CleanupRolePermissionsByRolePatterns(db);
      CleanupRolePermissionsByPermissionPatterns(db);
      CleanupRoles(db);
      CleanupPermissions(db);
      CleanupUsers(db);
    }

    private static void CleanupUserRolesByUserPatterns(AutoDiscoveryIAMDbContext db)
      => db.Database.ExecuteSqlRaw(CleanupSql.DeleteUserRolesByUsers);

    private static void CleanupUserRolesByRolePatterns(AutoDiscoveryIAMDbContext db)
      => db.Database.ExecuteSqlRaw(CleanupSql.DeleteUserRolesByRoles);

    private static void CleanupRolePermissionsByRolePatterns(AutoDiscoveryIAMDbContext db)
      => db.Database.ExecuteSqlRaw(CleanupSql.DeleteRolePermissionsByRoles);

    private static void CleanupRolePermissionsByPermissionPatterns(AutoDiscoveryIAMDbContext db)
      => db.Database.ExecuteSqlRaw(CleanupSql.DeleteRolePermissionsByPermissions);

    private static void CleanupRoles(AutoDiscoveryIAMDbContext db)
      => db.Database.ExecuteSqlRaw(CleanupSql.DeleteRoles);

    private static void CleanupPermissions(AutoDiscoveryIAMDbContext db)
      => db.Database.ExecuteSqlRaw(CleanupSql.DeletePermissions);

    private static void CleanupUsers(AutoDiscoveryIAMDbContext db)
      => db.Database.ExecuteSqlRaw(CleanupSql.DeleteUsers);

    private static void CleanupRefreshTokens(AutoDiscoveryIAMDbContext db)
      => db.Database.ExecuteSqlRaw(CleanupSql.DeleteRefreshTokens);

    private static void CleanupExternalLogins(AutoDiscoveryIAMDbContext db)
      => db.Database.ExecuteSqlRaw(CleanupSql.DeleteExternalLogins);

    private static void CleanupEmailChangeRequests(AutoDiscoveryIAMDbContext db)
      => db.Database.ExecuteSqlRaw(CleanupSql.DeleteEmailChangeRequests);

    private static class CleanupSql
    {
      public const string DeleteUserRolesByUsers = @"
        DELETE ur
        FROM UserRoles ur
        INNER JOIN Users u ON u.Id = ur.UserId
        WHERE
          u.Username LIKE 'integrationuser_%' OR
          u.Username LIKE 'users-query-%' OR
          u.Username LIKE 'assign-user-%' OR
          u.Username LIKE 'test-user-%' OR
          u.Username LIKE 'dryrun-user-%' OR
          u.Username LIKE 'delete-user-roles-user-%' OR
          u.Username LIKE 'missing-delete-user-role-user-%' OR
          u.Username LIKE 'create-user-roles-user-%' OR
          u.Username LIKE 'duplicate-user-role-user-%' OR
          u.Username LIKE 'restore-user-role-user-%' OR
          u.Username LIKE 'roles-query-user-%' OR
          u.Username LIKE 'clear-hidden-user-%' OR
          u.Username LIKE 'get-my-perms-%' OR
          u.Username LIKE 'get-my-perms-user-%' OR
          u.Username LIKE 'a04usr_%' OR
          u.Username LIKE 'a04adm_%' OR
          u.Username LIKE 'b02-role-user-%';";

      public const string DeleteUserRolesByRoles = @"
        DELETE ur
        FROM UserRoles ur
        INNER JOIN Roles r ON r.Id = ur.RoleId
        WHERE
          r.Name LIKE 'test-role-%' OR
          r.Name LIKE 'target-role-%' OR
          r.Name LIKE 'dryrun-role-%' OR
          r.Name LIKE 'delete-user-roles-role-%' OR
          r.Name LIKE 'missing-delete-user-role-role-%' OR
          r.Name LIKE 'create-user-roles-role-%' OR
          r.Name LIKE 'duplicate-user-role-role-%' OR
          r.Name LIKE 'missing-user-role-%' OR
          r.Name LIKE 'restore-user-role-role-%' OR
          r.Name LIKE 'bulk-role-permissions-role-%' OR
          r.Name LIKE 'bulk-role-permissions-dryrun-role-%' OR
          r.Name LIKE 'delete-role-permission-role-%' OR
          r.Name LIKE 'missing-delete-role-permission-role-%' OR
          r.Name LIKE 'create-role-permission-role-%' OR
          r.Name LIKE 'missing-permission-role-%' OR
          r.Name LIKE 'users-query-role-%' OR
          r.Name LIKE 'users-query-perm-role-%' OR
          r.Name LIKE 'permissions-query-role-%' OR
          r.Name LIKE 'permissions-query-users-role-%' OR
          r.Name LIKE 'roles-query-user-role-%' OR
          r.Name LIKE 'roles-query-perm-role-%' OR
          r.Name LIKE 'roles-query-inactive-%' OR
          r.Name LIKE 'clear-hidden-role-%' OR
          r.Name LIKE 'get-my-perms-role-%' OR
          r.Name LIKE 'b02-role-users-%' OR
          r.Name LIKE 'b02-role-perms-%' OR
          r.Name LIKE 'role-old-%' OR
          r.Name LIKE 'role-new-%' OR
          r.Name LIKE 'query-role-%' OR
          r.Name LIKE 'a03-updated-%' OR
          r.Name LIKE 'a03-res-%' OR
          r.Name LIKE 'a03-del-%' OR
          r.Name LIKE 'a03-role-%' OR
          r.Name LIKE 'a03-concurrency-%';";

      public const string DeleteRolePermissionsByRoles = @"
        DELETE rp
        FROM RolePermissions rp
        INNER JOIN Roles r ON r.Id = rp.RoleId
        WHERE
          r.Name LIKE 'test-role-%' OR
          r.Name LIKE 'target-role-%' OR
          r.Name LIKE 'dryrun-role-%' OR
          r.Name LIKE 'delete-user-roles-role-%' OR
          r.Name LIKE 'missing-delete-user-role-role-%' OR
          r.Name LIKE 'create-user-roles-role-%' OR
          r.Name LIKE 'duplicate-user-role-role-%' OR
          r.Name LIKE 'missing-user-role-%' OR
          r.Name LIKE 'restore-user-role-role-%' OR
          r.Name LIKE 'bulk-role-permissions-role-%' OR
          r.Name LIKE 'bulk-role-permissions-dryrun-role-%' OR
          r.Name LIKE 'delete-role-permission-role-%' OR
          r.Name LIKE 'missing-delete-role-permission-role-%' OR
          r.Name LIKE 'create-role-permission-role-%' OR
          r.Name LIKE 'missing-permission-role-%' OR
          r.Name LIKE 'users-query-role-%' OR
          r.Name LIKE 'users-query-perm-role-%' OR
          r.Name LIKE 'permissions-query-role-%' OR
          r.Name LIKE 'permissions-query-users-role-%' OR
          r.Name LIKE 'roles-query-user-role-%' OR
          r.Name LIKE 'roles-query-perm-role-%' OR
          r.Name LIKE 'roles-query-inactive-%' OR
          r.Name LIKE 'clear-hidden-role-%' OR
          r.Name LIKE 'get-my-perms-role-%' OR
          r.Name LIKE 'b02-role-users-%' OR
          r.Name LIKE 'b02-role-perms-%' OR
          r.Name LIKE 'role-old-%' OR
          r.Name LIKE 'role-new-%' OR
          r.Name LIKE 'query-role-%' OR
          r.Name LIKE 'a03-updated-%' OR
          r.Name LIKE 'a03-res-%' OR
          r.Name LIKE 'a03-del-%' OR
          r.Name LIKE 'a03-role-%' OR
          r.Name LIKE 'a03-concurrency-%';";

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
          p.[Key] LIKE 'permissions.query.roles.%' OR
          p.[Key] LIKE 'permissions.query.inactive.%' OR
          p.[Key] LIKE 'clear.hidden.permission.%' OR
          p.[Key] LIKE 'users.read_me_permissions_%' OR
          p.[Key] LIKE 'b02.role.perms.%' OR
          p.[Key] LIKE 'perm.a.%' OR
          p.[Key] LIKE 'perm.b.%';";

      public const string DeleteRoles = @"
        DELETE FROM Roles
        WHERE
          Name LIKE 'test-role-%' OR
          Name LIKE 'target-role-%' OR
          Name LIKE 'dryrun-role-%' OR
          Name LIKE 'delete-user-roles-role-%' OR
          Name LIKE 'missing-delete-user-role-role-%' OR
          Name LIKE 'create-user-roles-role-%' OR
          Name LIKE 'duplicate-user-role-role-%' OR
          Name LIKE 'missing-user-role-%' OR
          Name LIKE 'restore-user-role-role-%' OR
          Name LIKE 'bulk-role-permissions-role-%' OR
          Name LIKE 'bulk-role-permissions-dryrun-role-%' OR
          Name LIKE 'delete-role-permission-role-%' OR
          Name LIKE 'missing-delete-role-permission-role-%' OR
          Name LIKE 'create-role-permission-role-%' OR
          Name LIKE 'missing-permission-role-%' OR
          Name LIKE 'users-query-role-%' OR
          Name LIKE 'users-query-perm-role-%' OR
          Name LIKE 'permissions-query-role-%' OR
          Name LIKE 'permissions-query-users-role-%' OR
          Name LIKE 'roles-query-user-role-%' OR
          Name LIKE 'roles-query-perm-role-%' OR
          Name LIKE 'roles-query-inactive-%' OR
          Name LIKE 'clear-hidden-role-%' OR
          Name LIKE 'get-my-perms-role-%' OR
          Name LIKE 'b02-role-users-%' OR
          Name LIKE 'b02-role-perms-%' OR
          Name LIKE 'role-old-%' OR
          Name LIKE 'role-new-%' OR
          Name LIKE 'query-role-%' OR
          Name LIKE 'a03-updated-%' OR
          Name LIKE 'a03-res-%' OR
          Name LIKE 'a03-del-%' OR
          Name LIKE 'a03-role-%' OR
          Name LIKE 'a03-concurrency-%';";

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

      public const string DeleteUsers = @"
        DELETE FROM Users
        WHERE
          Username LIKE 'integrationuser_%' OR
          Username LIKE 'users-query-%' OR
          Username LIKE 'assign-user-%' OR
          Username LIKE 'test-user-%' OR
          Username LIKE 'dryrun-user-%' OR
          Username LIKE 'delete-user-roles-user-%' OR
          Username LIKE 'missing-delete-user-role-user-%' OR
          Username LIKE 'create-user-roles-user-%' OR
          Username LIKE 'duplicate-user-role-user-%' OR
          Username LIKE 'restore-user-role-user-%' OR
          Username LIKE 'roles-query-user-%' OR
          Username LIKE 'permissions-query-user-%' OR
          Username LIKE 'query-user-%' OR
          Username LIKE 'clear-hidden-user-%' OR
          Username LIKE 'get-my-perms-%' OR
          Username LIKE 'get-my-perms-user-%' OR
          Username LIKE 'a04usr_%' OR
          Username LIKE 'a04adm_%' OR
          Username LIKE 'b02-role-user-%' OR
          Username LIKE 'activation_%' OR
          Username LIKE 'google_int_%' OR
          Username LIKE 'pwd-mgmt-%' OR
          Username LIKE 'twofa-%' OR
          Username LIKE 'verify2fa-%' OR
          Username LIKE 'flow-%' OR
          Username LIKE 'concurrency-%' OR
          Username LIKE 'myprofile-%' OR
          Username LIKE 'canary_%' OR
          Username LIKE 'canary_admin_%' OR
          Username LIKE 'enum-timing-%' OR
          Username LIKE '2fa-lockout-%' OR
          Email LIKE 'activation-%@example.com' OR
          Email LIKE '%-google-test@example.com' OR
          Email LIKE 'change-pwd-%@example.com' OR
          Email LIKE 'reset-%@example.com' OR
          Email LIKE 'twofa-%@example.com' OR
          Email LIKE 'verify2fa-%@example.com' OR
          Email LIKE 'canary_%@example.com' OR
          Email LIKE 'canary_admin_%@example.com' OR
          Email LIKE 'enum-timing-%@example.com' OR
          Email LIKE 'does-not-exist-%@example.com' OR
          Email LIKE '2fa-lockout-%@example.com';";

      public const string DeleteExternalLogins = @"
        DELETE el
        FROM ExternalLogins el
        LEFT JOIN Users u ON u.Id = el.UserId
        WHERE
          u.Username LIKE 'google_int_%' OR
          u.Email LIKE '%-google-test@example.com';";

      public const string DeleteEmailChangeRequests = @"
        DELETE ecr
        FROM EmailChangeRequests ecr
        INNER JOIN Users u ON u.Id = ecr.UserId
        WHERE
          u.Username LIKE 'flow-%' OR
          u.Username LIKE 'concurrency-%';";

      public const string DeleteRefreshTokens = @"
        DELETE FROM RefreshTokens;";
    }
  }
}
