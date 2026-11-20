// -----------------------------------------------------------------------
// <copyright file="ExternalLoginExchangeCodeConfiguration.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;

/// <summary>
/// Configuración de Entity Framework para la entidad ExternalLoginExchangeCode.
/// </summary>
public class ExternalLoginExchangeCodeConfiguration : IEntityTypeConfiguration<ExternalLoginExchangeCode>
{
  public void Configure(EntityTypeBuilder<ExternalLoginExchangeCode> builder)
  {
    builder.ToTable("ExternalLoginExchangeCodes");

    builder.HasKey(x => x.Id);

    builder.Property(x => x.CodeHash)
      .IsRequired()
      .HasMaxLength(128);

    builder.Property(x => x.SessionPayload)
      .IsRequired();

    builder.Property(x => x.ExpiresAt)
      .IsRequired();

    // Token de concurrencia: el UPDATE que marca el código como consumido incluye
    // "WHERE IsConsumed = 0", así que de dos canjes simultáneos solo uno lo logra.
    builder.Property(x => x.IsConsumed)
      .IsRequired()
      .HasDefaultValue(false)
      .IsConcurrencyToken();

    builder.HasIndex(x => x.PublicId)
      .IsUnique()
      .HasDatabaseName("IX_ExternalLoginExchangeCodes_PublicId");

    // Unico por hash: cada codigo se genera con suficiente entropia para que una colision real
    // sea, en la practica, imposible -- el indice unico es solo defensa adicional, no la unica
    // garantia de unicidad.
    builder.HasIndex(x => x.CodeHash)
      .IsUnique()
      .HasDatabaseName("IX_ExternalLoginExchangeCodes_CodeHash");

    // Para el borrado periodico de codigos vencidos/consumidos (no automatico todavia, ver
    // ExternalLoginExchangeCodeRepository).
    builder.HasIndex(x => x.ExpiresAt)
      .HasDatabaseName("IX_ExternalLoginExchangeCodes_ExpiresAt");

    builder.HasQueryFilter(x => !x.IsDeleted);
  }
}
