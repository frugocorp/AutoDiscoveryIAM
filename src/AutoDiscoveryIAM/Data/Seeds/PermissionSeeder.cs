// -----------------------------------------------------------------------
// <copyright file="PermissionSeeder.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;

/// <summary>
/// Seeder para datos iniciales de la tabla Permissions.
/// </summary>
internal static class PermissionSeeder
{
  /// <summary>
  /// Permisos con los que el rol <see cref="RoleSeeder.AdminAutoDiscoveryIamRoleName"/> administra la
  /// app desde el primer día: consultar usuarios, roles y permisos, crear y editar roles, asignarles
  /// permisos, asignar roles a usuarios y aprobar o rechazar registros.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Estas keys deben coincidir exactamente con las que deriva el descubrimiento automático
  /// (formato <c>{feature}.{accion_snake_case}</c>, ver <c>PermissionNamingService</c>) -- si
  /// renombras un slice de Users/Roles/Permissions, actualiza también esta lista.
  /// </para>
  /// <para>
  /// Es aditiva a propósito: al actualizar el paquete, una key nueva aquí se crea y se asigna al rol en
  /// el siguiente arranque (<c>SeedAutoDiscoveryIAMDefaultDataAsync()</c>), sin tocar las demás
  /// asignaciones. No incluye lo que sigue reservado a los flags que solo se marcan en la base de
  /// datos: cambiar <c>IsAdmin</c>, limpiar relaciones ocultas, ejecutar cambios de correo y la prueba
  /// de SMTP (<c>[RequiresSuAdminAccess]</c>), ni el sync (<c>[RequiresDevelopmentAccess]</c>).
  /// </para>
  /// </remarks>
  internal static readonly (string Key, string Description, string Group)[] DefaultPermissions =
  [
    ("users.get_users", "Consultar usuarios", "Users"),
    ("users.get_user_by_public_id", "Consultar un usuario", "Users"),
    ("users.create_user_by_admin", "Dar de alta usuarios", "Users"),
    ("users.update_user", "Actualizar usuario", "Users"),
    ("users.activation", "Aprobar, rechazar y reenviar la activación de usuarios", "Users"),
    ("users.create_user_roles", "Asignar rol a usuario", "Users"),
    ("users.delete_user_roles_by_public_id", "Quitar rol de usuario", "Users"),
    ("users.update_bulk_user_roles", "Reemplazar los roles de un usuario", "Users"),
    ("users.update_bulk_user_roles_by_role_public_id", "Reemplazar los usuarios de un rol", "Users"),
    ("users.get_roles_by_public_id", "Consultar los roles de un usuario", "Users"),
    ("users.get_permissions_by_public_id", "Consultar los permisos de un usuario", "Users"),
    ("users.review_email_change_request", "Revisar solicitudes de cambio de correo", "Users"),
    ("roles.get_roles", "Consultar roles", "Roles"),
    ("roles.get_role_by_public_id", "Consultar un rol", "Roles"),
    ("roles.create_role", "Crear nuevos roles", "Roles"),
    ("roles.update_role_by_public_id", "Actualizar rol por ID público", "Roles"),
    ("roles.delete_role_by_public_id", "Eliminar rol por ID público", "Roles"),
    ("roles.restore_role_by_public_id", "Restaurar un rol eliminado", "Roles"),
    ("roles.create_role_permissions", "Asignar permiso a rol", "Roles"),
    ("roles.delete_role_permissions_by_permission_public_id", "Quitar permiso de rol", "Roles"),
    ("roles.update_bulk_role_permissions", "Reemplazar los permisos de un rol", "Roles"),
    ("roles.get_permissions_by_public_id", "Consultar los permisos de un rol", "Roles"),
    ("roles.get_users_by_public_id", "Consultar los usuarios de un rol", "Roles"),
    ("permissions.get_permissions", "Consultar permisos", "Permissions"),
    ("permissions.get_roles_by_public_id", "Consultar los roles que tienen un permiso", "Permissions"),
    ("permissions.get_users_by_public_id", "Consultar los usuarios que tienen un permiso", "Permissions"),
  ];

  private static readonly HashSet<string> AdministrativeKeys = DefaultPermissions.Select(p => p.Key).ToHashSet(StringComparer.Ordinal);

  /// <summary>
  /// Indica si <paramref name="key"/> es uno de los permisos de administración del paquete
  /// (<see cref="DefaultPermissions"/>). El rol por defecto de los usuarios auto-registrados nunca
  /// puede tenerlos: con uno solo, todo el que se registre nacería administrador.
  /// </summary>
  /// <param name="key">Clave de permiso.</param>
  /// <returns><c>true</c> si es un permiso de administración del paquete.</returns>
  internal static bool IsAdministrativeKey(string key) => AdministrativeKeys.Contains(key);

  /// <summary>
  /// Inserta los permisos esenciales (<see cref="DefaultPermissions"/>) en tiempo de ejecución, vía
  /// el <see cref="AutoDiscoveryIAMDbContext"/> -- agnóstico de proveedor, invocable desde
  /// <c>SeedAutoDiscoveryIAMDefaultDataAsync()</c>. Solo inserta los que no existen; si uno ya
  /// existe pero fue borrado suave o desactivado (ej. un admin lo eliminó y luego la app reinició),
  /// lo reactiva en vez de intentar insertarlo de nuevo -- el índice único de <c>Key</c> no distingue
  /// filas borradas suavemente, así que reinsertar chocaría contra la restricción.
  /// </summary>
  public static async Task SeedDataAsync(AutoDiscoveryIAMDbContext dbContext, CancellationToken cancellationToken = default)
  {
    var keys = DefaultPermissions.Select(p => p.Key).ToArray();

    var existingByKey = (await dbContext.Permissions
      .IgnoreQueryFilters()
      .Where(p => keys.Contains(p.Key))
      .ToListAsync(cancellationToken))
      .ToDictionary(p => p.Key, StringComparer.Ordinal);

    var hasChanges = false;
    foreach (var (key, description, group) in DefaultPermissions)
    {
      if (existingByKey.TryGetValue(key, out var existing))
      {
        if (existing.IsDeleted || !existing.IsActive)
        {
          existing.IsDeleted = false;
          existing.IsActive = true;
          hasChanges = true;
        }

        continue;
      }

      dbContext.Permissions.Add(new Permission
      {
        Key = key,
        Description = description,
        Group = group,
        IsActive = true,
        IsSystemGenerated = true,
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
