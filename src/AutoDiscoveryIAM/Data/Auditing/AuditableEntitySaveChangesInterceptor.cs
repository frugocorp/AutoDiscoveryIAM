// -----------------------------------------------------------------------
// <copyright file="AuditableEntitySaveChangesInterceptor.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Auditing;

/// <summary>
/// Rellena automaticamente <see cref="AuditableEntity.CreatedAt"/>/<c>By</c>,
/// <see cref="AuditableEntity.UpdatedAt"/>/<c>By</c>, <see cref="AuditableEntity.DeletedAt"/>/<c>By</c>,
/// <see cref="AuditableEntity.RestoredAt"/>/<c>By</c> e <see cref="AuditableEntity.IsDeleted"/> (soft
/// delete) para cualquier entidad que herede <see cref="AuditableEntity"/>, sin importar a que
/// <see cref="DbContext"/> pertenezca -- <see cref="AutoDiscoveryIAMDbContext"/> lo usa para sus propias
/// 8 entidades, y un proyecto consumidor puede registrar la misma instancia en su propio
/// <see cref="DbContext"/> (via <c>optionsBuilder.AddInterceptors(...)</c>) para que sus propias
/// entidades (<c>MiEntidad : AuditableEntity</c>) queden auditadas igual, sin duplicar esta logica.
/// </summary>
/// <remarks>
/// Solo resuelve timestamps y el usuario auditor (via el <c>resolveCurrentUserId</c> provisto) --
/// no asume nada sobre como un consumidor identifica a su usuario actual, a proposito: el
/// mecanismo de <see cref="AutoDiscoveryIAMDbContext"/> para resolver el Id interno a partir del
/// <c>PublicId</c> del JWT (con fallback al usuario "system") es especifico de su propia tabla
/// <c>Users</c> y no se generaliza aqui. Ver README.md, seccion "Reutilizar auditoria/soft-delete".
/// </remarks>
public sealed class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
  private readonly Func<long?> _resolveCurrentUserId;

  public AuditableEntitySaveChangesInterceptor(Func<long?> resolveCurrentUserId)
  {
    _resolveCurrentUserId = resolveCurrentUserId ?? throw new ArgumentNullException(nameof(resolveCurrentUserId));
  }

  public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
  {
    ApplyAuditInfo(eventData.Context);
    return base.SavingChanges(eventData, result);
  }

  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
    DbContextEventData eventData,
    InterceptionResult<int> result,
    CancellationToken cancellationToken = default)
  {
    ApplyAuditInfo(eventData.Context);
    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  private void ApplyAuditInfo(DbContext? context)
  {
    if (context == null)
    {
      return;
    }

    var entries = context.ChangeTracker.Entries()
      .Where(e => e.Entity is AuditableEntity &&
                  (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted));

    foreach (var entry in entries)
    {
      var auditable = (AuditableEntity)entry.Entity;
      var now = TimeHelpers.GetCurrentMexicoTime();
      var currentUserId = _resolveCurrentUserId();

      ApplyAuditInfoByState(entry, auditable, now, currentUserId);
    }
  }

  private static void ApplyAuditInfoByState(EntityEntry entry, AuditableEntity auditable, DateTime now, long? currentUserId)
  {
    switch (entry.State)
    {
      case EntityState.Added:
        auditable.CreatedAt = now;
        auditable.CreatedBy ??= currentUserId;
        auditable.IsDeleted = false;
        break;
      case EntityState.Modified:
        HandleModifiedEntity(entry, auditable, now, currentUserId);
        break;
      case EntityState.Deleted:
        auditable.DeletedAt = now;
        auditable.DeletedBy = currentUserId;
        auditable.IsDeleted = true;
        entry.State = EntityState.Modified; // Soft delete
        break;
    }
  }

  private static void HandleModifiedEntity(EntityEntry entry, AuditableEntity auditable, DateTime now, long? currentUserId)
  {
    // Detectar si la entidad esta siendo restaurada (IsDeleted cambio de true a false)
    var isDeletedProperty = entry.Property(nameof(auditable.IsDeleted));
    var wasDeleted = isDeletedProperty.OriginalValue is true;
    var isNowActive = isDeletedProperty.CurrentValue is false;

    if (wasDeleted && isNowActive)
    {
      // Entidad siendo restaurada. Los campos DeletedAt/DeletedBy se mantienen para preservar
      // el historial de auditoria completo.
      auditable.RestoredAt = now;
      auditable.RestoredBy ??= currentUserId;
    }
    else
    {
      auditable.UpdatedAt = now;
      auditable.UpdatedBy = currentUserId;
    }
  }
}
