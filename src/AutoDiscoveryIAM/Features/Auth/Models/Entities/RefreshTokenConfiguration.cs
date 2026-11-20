// -----------------------------------------------------------------------
// <copyright file="RefreshTokenConfiguration.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;

/// <summary>
/// Configuración de Entity Framework para la entidad RefreshToken.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
  public void Configure(EntityTypeBuilder<RefreshToken> builder)
  {
    builder.ToTable("RefreshTokens");

    builder.HasKey(rt => rt.Id);

    builder.Property(rt => rt.Token)
      .IsRequired()
      .HasMaxLength(500);

    builder.Property(rt => rt.UserId)
      .IsRequired();

    builder.Property(rt => rt.ExpiresAt)
      .IsRequired();

    builder.Property(rt => rt.IsRevoked)
      .IsRequired()
      .HasDefaultValue(false);

    builder.Property(rt => rt.RevokedAt)
      .IsRequired(false);

    builder.Property(rt => rt.IpAddress)
      .HasMaxLength(45); // IPv6 max length

    builder.Property(rt => rt.UserAgent)
      .HasMaxLength(500);

    builder.Property(rt => rt.ReplacedByToken)
      .HasMaxLength(500);

    builder.Property(rt => rt.RevocationReason)
      .HasMaxLength(200);

    // AuditableEntityConfiguration<T> (generico abierto) nunca se aplica via
    // ApplyConfigurationsFromAssembly, por lo que esta entidad no tenia ningun indice de PublicId.
    builder.HasIndex(rt => rt.PublicId)
      .IsUnique()
      .HasDatabaseName("IX_RefreshTokens_PublicId_Security");

    // Índices para performance
    builder.HasIndex(rt => rt.Token)
      .IsUnique()
      .HasDatabaseName("IX_RefreshTokens_Token");

    builder.HasIndex(rt => rt.UserId)
      .HasDatabaseName("IX_RefreshTokens_UserId");

    builder.HasIndex(rt => rt.ExpiresAt)
      .HasDatabaseName("IX_RefreshTokens_ExpiresAt");

    builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked })
      .HasDatabaseName("IX_RefreshTokens_UserId_IsRevoked");

    // concurrencia optimista — evita que dos rotaciones simultaneas del mismo token
    // (misma peticion /auth/refresh disparada dos veces en carrera real) tengan exito ambas.
    builder.Property(rt => rt.RowVersion).IsRowVersion();

    // Relación con User
    builder.HasOne(rt => rt.User)
      .WithMany()
      .HasForeignKey(rt => rt.UserId)
      .OnDelete(DeleteBehavior.Cascade);

    // RefreshToken hereda IsDeleted de AuditableEntity pero no tenía filtro global,
    // a diferencia de User/Role/Permission/Post — un token soft-deleted seguía siendo visible a
    // cualquier consulta normal.
    builder.HasQueryFilter(rt => !rt.IsDeleted);
  }
}
