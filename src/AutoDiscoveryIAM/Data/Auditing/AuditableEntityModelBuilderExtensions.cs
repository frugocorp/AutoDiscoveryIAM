// -----------------------------------------------------------------------
// <copyright file="AuditableEntityModelBuilderExtensions.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq.Expressions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Auditing;

/// <summary>
/// Aplica por convencion el filtro global de soft-delete (<c>WHERE !IsDeleted</c>) a cualquier
/// tipo de entidad del modelo que herede <see cref="AuditableEntity"/>, sin tener que escribir
/// <c>HasQueryFilter(x => !x.IsDeleted)</c> a mano en cada <c>IEntityTypeConfiguration&lt;T&gt;</c>.
/// Pensado para el <c>OnModelCreating</c> de un <see cref="Microsoft.EntityFrameworkCore.DbContext"/>
/// propio de un proyecto consumidor -- ver README.md, seccion "Reutilizar auditoria/soft-delete".
/// </summary>
public static class AuditableEntityModelBuilderExtensions
{
  public static void ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
  {
    ArgumentNullException.ThrowIfNull(modelBuilder);

    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      if (!typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
      {
        continue;
      }

      var parameter = Expression.Parameter(entityType.ClrType, "entity");
      var isDeletedProperty = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
      var notDeleted = Expression.Lambda(Expression.Not(isDeletedProperty), parameter);

      entityType.SetQueryFilter(notDeleted);
    }
  }
}
