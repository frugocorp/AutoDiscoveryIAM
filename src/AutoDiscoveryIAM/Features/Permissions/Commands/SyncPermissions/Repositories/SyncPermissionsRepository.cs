// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Repositories;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Repositories;

/// <summary>
/// Implementación del repositorio específico para operaciones de sincronización de permisos.
/// </summary>
public class SyncPermissionsRepository(AutoDiscoveryIAMDbContext context)
    : PermissionsRepository(context), ISyncPermissionsRepository
{
  /// <inheritdoc />
  public async Task MarkAsObsoleteAsync(string key, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    Stats.QueriesExecuted++;
    var permission = await Context.Permissions
        .FirstAsync(p => p.Key == key, cancellationToken);

    var now = DateTime.UtcNow;
    permission.IsActive = false;
    permission.IsDeleted = true;
    permission.DeletedAt = now;
    permission.DeletedBy = 1;
    permission.UpdatedAt = now;
    permission.UpdatedBy = 1;

    Stats.Updated++;
    Stats.Blocked++; // Consideramos marcar como obsoleto como "bloquear"
  }

  /// <inheritdoc />
  public async Task RecoverPermissionAsync(Permission permission, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(permission);
    cancellationToken.ThrowIfCancellationRequested();
    Stats.QueriesExecuted++;

    var now = DateTime.UtcNow;
    permission.IsActive = true;
    permission.IsDeleted = false;
    permission.RestoredAt = now;
    permission.RestoredBy = 1;
    permission.UpdatedAt = now;
    permission.UpdatedBy = 1;

    Stats.Updated++;
    Stats.Restored++;

    await Task.Yield();
  }

  /// <inheritdoc />
  public async Task UpdateLastSyncAsync(string key, DateTime syncDate, CancellationToken cancellationToken = default)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    Stats.QueriesExecuted++;

    var permission = await Context.Permissions
        .FirstOrDefaultAsync(p => p.Key == key, cancellationToken);

    if (permission != null)
    {
      permission.LastSyncAt = syncDate;
      permission.UpdatedAt = DateTime.UtcNow;
      permission.UpdatedBy = 1;
      Stats.Updated++;
    }
  }

  /// <inheritdoc />
  public async Task<IList<Permission>> GetOutOfSyncPermissionsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
  {
    Stats.QueriesExecuted++;

    return await Context.Permissions
        .Where(p => p.IsActive && (p.LastSyncAt == null || p.LastSyncAt < cutoffDate))
        .OrderBy(p => p.Key)
        .ToListAsync(cancellationToken);
  }
}
