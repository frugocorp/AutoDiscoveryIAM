// -----------------------------------------------------------------------
// <copyright file="PrivilegeEscalationSaveChangesInterceptor.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Authorization;

/// <summary>
/// Regla de no escalada: nadie otorga ni administra permisos que no tiene.
/// </summary>
/// <remarks>
/// <para>
/// El permiso RBAC de una operación no basta. Quien solo puede asignar roles podría asignarse el rol
/// administrador; quien solo puede editar usuarios podría cambiarle el email a un administrador y
/// quedarse con su cuenta con el reseteo público de contraseña. Antes de guardar se revisa, para el
/// usuario autenticado:
/// </para>
/// <list type="bullet">
/// <item>Asignar o restaurar un rol a un usuario, o reactivar un rol: debe tener todos los permisos del rol.</item>
/// <item>Asignar o restaurar un permiso a un rol: debe tener ese permiso.</item>
/// <item>Cambiar el email de otro usuario: debe tener todos los permisos de ese usuario, y el usuario no puede tener flags.</item>
/// </list>
/// <para>
/// Se aplica al guardar (no en cada endpoint) para cubrir cualquier camino: asignación individual,
/// restauración, operaciones masivas y endpoints futuros. No aplica sin usuario autenticado (seed,
/// autorregistro), a cuentas con <c>IsAdmin</c>, <c>IsSuAdmin</c> o <c>IsDevelopment</c>, que ya
/// pasan por encima del RBAC, ni a quien tiene todos los permisos administrativos de la librería
/// (el rol <c>AdminAutoDiscoveryIAM</c> completo): ese ya administra todo, y es quien otorga los
/// permisos de los endpoints propios después de cada sincronización, que al inicio no tiene nadie.
/// La regla restringe a los roles administrativos parciales (por ejemplo, un rol de soporte).
/// </para>
/// </remarks>
internal sealed class PrivilegeEscalationSaveChangesInterceptor(Func<Guid?> resolveCallerPublicId) : SaveChangesInterceptor
{
  /// <summary>Mensaje del 403 cuando la operación otorgaría o administraría permisos que el llamador no tiene.</summary>
  internal const string EscalationMessage = "No puedes otorgar ni administrar permisos que tú mismo no tienes.";

  private readonly Func<Guid?> _resolveCallerPublicId = resolveCallerPublicId ?? throw new ArgumentNullException(nameof(resolveCallerPublicId));

  public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
    DbContextEventData eventData,
    InterceptionResult<int> result,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(eventData);

    if (eventData.Context is AutoDiscoveryIAMDbContext dbContext)
    {
      await EnsureNoEscalationAsync(dbContext, cancellationToken);
    }

    return await base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
  {
    ArgumentNullException.ThrowIfNull(eventData);

    if (eventData.Context is AutoDiscoveryIAMDbContext dbContext)
    {
      EnsureNoEscalation(dbContext);
    }

    return base.SavingChanges(eventData, result);
  }

  private async Task EnsureNoEscalationAsync(AutoDiscoveryIAMDbContext dbContext, CancellationToken cancellationToken)
  {
    var changes = CollectChanges(dbContext);
    if (changes.IsEmpty || _resolveCallerPublicId() is not Guid callerPublicId)
    {
      return;
    }

    // Un llamador que no existe en la base se trata como uno sin permisos: no otorga nada.
    var caller = await CallerQuery(dbContext, callerPublicId).FirstOrDefaultAsync(cancellationToken) ?? UnknownCaller;
    if (caller.IsPrivileged)
    {
      return;
    }

    var managedUserIds = changes.ManagedUserIds.Where(id => id != caller.Id).ToList();
    if (managedUserIds.Count > 0 && await dbContext.Users.AsNoTracking().AnyAsync(u => managedUserIds.Contains(u.Id) && (u.IsAdmin || u.IsSuAdmin || u.IsDevelopment), cancellationToken))
    {
      throw new InsufficientPermissionsException(EscalationMessage);
    }

    var callerKeys = (await PermissionKeysOfUsers(dbContext, [caller.Id]).ToListAsync(cancellationToken)).ToHashSet(StringComparer.Ordinal);
    if (IsFullAdministrator(callerKeys))
    {
      return;
    }

    var requiredKeys = new List<string>();
    requiredKeys.AddRange(await PermissionKeysOfRoles(dbContext, changes.RoleIds).ToListAsync(cancellationToken));
    requiredKeys.AddRange(await PermissionKeys(dbContext, changes.PermissionIds).ToListAsync(cancellationToken));
    requiredKeys.AddRange(await PermissionKeysOfUsers(dbContext, managedUserIds).ToListAsync(cancellationToken));

    ThrowIfMissing(callerKeys, requiredKeys);
  }

  private void EnsureNoEscalation(AutoDiscoveryIAMDbContext dbContext)
  {
    var changes = CollectChanges(dbContext);
    if (changes.IsEmpty || _resolveCallerPublicId() is not Guid callerPublicId)
    {
      return;
    }

    var caller = CallerQuery(dbContext, callerPublicId).FirstOrDefault() ?? UnknownCaller;
    if (caller.IsPrivileged)
    {
      return;
    }

    var managedUserIds = changes.ManagedUserIds.Where(id => id != caller.Id).ToList();
    if (managedUserIds.Count > 0 && dbContext.Users.AsNoTracking().Any(u => managedUserIds.Contains(u.Id) && (u.IsAdmin || u.IsSuAdmin || u.IsDevelopment)))
    {
      throw new InsufficientPermissionsException(EscalationMessage);
    }

    var callerKeys = PermissionKeysOfUsers(dbContext, [caller.Id]).ToHashSet(StringComparer.Ordinal);
    if (IsFullAdministrator(callerKeys))
    {
      return;
    }

    var requiredKeys = new List<string>();
    requiredKeys.AddRange(PermissionKeysOfRoles(dbContext, changes.RoleIds));
    requiredKeys.AddRange(PermissionKeys(dbContext, changes.PermissionIds));
    requiredKeys.AddRange(PermissionKeysOfUsers(dbContext, managedUserIds));

    ThrowIfMissing(callerKeys, requiredKeys);
  }

  /// <summary>Tiene todos los permisos administrativos de la librería (<see cref="PermissionSeeder.DefaultPermissions"/>).</summary>
  private static bool IsFullAdministrator(HashSet<string> callerKeys)
    => Array.TrueForAll(PermissionSeeder.DefaultPermissions, permission => Holds(callerKeys, permission.Key));

  private static void ThrowIfMissing(HashSet<string> callerKeys, List<string> requiredKeys)
  {
    if (requiredKeys.Exists(key => !Holds(callerKeys, key)))
    {
      throw new InsufficientPermissionsException(EscalationMessage);
    }
  }

  /// <summary>Misma regla que el middleware de autorización: la clave exacta o el comodín <c>feature.*</c>.</summary>
  private static bool Holds(HashSet<string> callerKeys, string key)
  {
    if (callerKeys.Contains(key))
    {
      return true;
    }

    var separator = key.IndexOf('.', StringComparison.Ordinal);
    return separator > 0 && callerKeys.Contains(string.Concat(key.AsSpan(0, separator), ".*"));
  }

  private static PendingChanges CollectChanges(AutoDiscoveryIAMDbContext dbContext)
  {
    var changes = new PendingChanges();

    foreach (var entry in dbContext.ChangeTracker.Entries<UserRole>())
    {
      if (IsGranted(entry, entry.Entity.IsDeleted))
      {
        changes.RoleIds.Add(entry.Entity.RoleId);
      }
    }

    foreach (var entry in dbContext.ChangeTracker.Entries<RolePermission>())
    {
      if (IsGranted(entry, entry.Entity.IsDeleted))
      {
        changes.PermissionIds.Add(entry.Entity.PermissionId);
      }
    }

    foreach (var entry in dbContext.ChangeTracker.Entries<Role>())
    {
      if (entry.State == EntityState.Modified && (BecameFalse(entry, nameof(Role.IsDeleted)) || BecameTrue(entry, nameof(Role.IsActive))))
      {
        changes.RoleIds.Add(entry.Entity.Id);
      }
    }

    foreach (var entry in dbContext.ChangeTracker.Entries<User>())
    {
      if (entry.State == EntityState.Modified && entry.Property(nameof(User.Email)).IsModified)
      {
        changes.ManagedUserIds.Add(entry.Entity.Id);
      }
    }

    return changes;
  }

  /// <summary>Relación nueva y vigente, o una borrada (lógicamente) que se restaura.</summary>
  private static bool IsGranted(EntityEntry entry, bool isDeleted)
    => (entry.State == EntityState.Added && !isDeleted)
      || (entry.State == EntityState.Modified && BecameFalse(entry, "IsDeleted"));

  private static bool BecameFalse(EntityEntry entry, string propertyName)
  {
    var property = entry.Property(propertyName);
    return property.IsModified && property.OriginalValue is true && property.CurrentValue is false;
  }

  private static bool BecameTrue(EntityEntry entry, string propertyName)
  {
    var property = entry.Property(propertyName);
    return property.IsModified && property.OriginalValue is false && property.CurrentValue is true;
  }

  private static IQueryable<CallerInfo> CallerQuery(AutoDiscoveryIAMDbContext dbContext, Guid callerPublicId)
    => dbContext.Users
      .AsNoTracking()
      .Where(u => u.PublicId == callerPublicId)
      .Select(u => new CallerInfo(u.Id, u.IsAdmin || u.IsSuAdmin || u.IsDevelopment));

  /// <summary>Permisos efectivos, con los mismos filtros que la autorización por petición.</summary>
  private static IQueryable<string> PermissionKeysOfUsers(AutoDiscoveryIAMDbContext dbContext, IReadOnlyCollection<long> userIds)
    => dbContext.UserRoles
      .AsNoTracking()
      .Where(ur => userIds.Contains(ur.UserId) && !ur.IsDeleted && ur.User.IsActive && ur.Role.IsActive && !ur.Role.IsDeleted)
      .SelectMany(ur => ur.Role.RolePermissions)
      .Where(rp => !rp.IsDeleted && rp.Permission.IsActive && !rp.Permission.IsDeleted)
      .Select(rp => rp.Permission.Key)
      .Distinct();

  private static IQueryable<string> PermissionKeysOfRoles(AutoDiscoveryIAMDbContext dbContext, IReadOnlyCollection<long> roleIds)
    => dbContext.RolePermissions
      .AsNoTracking()
      .Where(rp => roleIds.Contains(rp.RoleId) && !rp.IsDeleted && rp.Permission.IsActive && !rp.Permission.IsDeleted)
      .Select(rp => rp.Permission.Key)
      .Distinct();

  private static IQueryable<string> PermissionKeys(AutoDiscoveryIAMDbContext dbContext, IReadOnlyCollection<long> permissionIds)
    => dbContext.Permissions
      .AsNoTracking()
      .Where(p => permissionIds.Contains(p.Id) && p.IsActive)
      .Select(p => p.Key);

  private static readonly CallerInfo UnknownCaller = new(0, false);

  private sealed record CallerInfo(long Id, bool IsPrivileged);

  private sealed class PendingChanges
  {
    public List<long> RoleIds { get; } = [];

    public List<long> PermissionIds { get; } = [];

    public List<long> ManagedUserIds { get; } = [];

    public bool IsEmpty => RoleIds.Count == 0 && PermissionIds.Count == 0 && ManagedUserIds.Count == 0;
  }
}
