// -----------------------------------------------------------------------
// <copyright file="ExternalLoginConfiguration.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;

/// <summary>
/// Configuración EF Core para la entidad ExternalLogin.
/// </summary>
public class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("ExternalLogins");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
          .IsRequired();

        builder.Property(x => x.Provider)
          .IsRequired()
          .HasMaxLength(50);

        builder.Property(x => x.ProviderId)
          .IsRequired()
          .HasMaxLength(255);

        // AuditableEntityConfiguration<T> (generico abierto) nunca se aplica via
        // ApplyConfigurationsFromAssembly, por lo que esta entidad no tenia ningun indice de PublicId.
        builder.HasIndex(x => x.PublicId)
          .IsUnique()
          .HasDatabaseName("IX_ExternalLogins_PublicId_Security");

        builder.HasIndex(x => new { x.Provider, x.ProviderId })
          .IsUnique()
          .HasDatabaseName("IX_ExternalLogins_Provider_ProviderId");

        builder.HasIndex(x => new { x.UserId, x.Provider })
          .IsUnique()
          .HasDatabaseName("IX_ExternalLogins_UserId_Provider");

        builder.HasOne(x => x.User)
          .WithMany(u => u.ExternalLogins)
          .HasForeignKey(x => x.UserId)
          .OnDelete(DeleteBehavior.Cascade);

        // ExternalLogin hereda IsDeleted de AuditableEntity pero no tenía filtro global,
        // a diferencia de User/Role/Permission/Post — un vínculo externo soft-deleted seguía siendo
        // visible a cualquier consulta normal.
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
