// -----------------------------------------------------------------------
// <copyright file="EmailChangeRequestConfiguration.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

/// <summary>
/// Configuración EF Core para <see cref="EmailChangeRequest"/>.
/// </summary>
public class EmailChangeRequestConfiguration : IEntityTypeConfiguration<EmailChangeRequest>
{
  public void Configure(EntityTypeBuilder<EmailChangeRequest> builder)
  {
    builder.ToTable("EmailChangeRequests");
    builder.HasKey(x => x.Id);

    builder.Property(x => x.RequestedEmail).IsRequired().HasMaxLength(150);
    builder.Property(x => x.Status).IsRequired().HasMaxLength(30);
    builder.Property(x => x.RejectionReason).HasMaxLength(500);

    // AuditableEntityConfiguration<T> (generico abierto) nunca se aplica via
    // ApplyConfigurationsFromAssembly, por lo que esta entidad no tenia ningun indice de PublicId.
    builder.HasIndex(x => x.PublicId)
      .IsUnique()
      .HasDatabaseName("IX_EmailChangeRequests_PublicId_Security");

    builder.HasIndex(x => x.UserId);
    builder.HasIndex(x => x.Status);

    // concurrencia optimista — SQL Server actualiza esta columna en cada UPDATE.
    builder.Property(x => x.RowVersion).IsRowVersion();

    builder.HasOne(x => x.User)
      .WithMany()
      .HasForeignKey(x => x.UserId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.HasQueryFilter(x => !x.IsDeleted);
  }
}
