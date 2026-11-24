using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;

/// <summary>
/// Configuración EF Core para la entidad Permission del sistema RBAC.
/// </summary>
public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
  /// <summary>
  /// Largo máximo de <see cref="Permission.Description"/> en la base de datos.
  /// </summary>
  internal const int DescriptionMaxLength = 500;

  public void Configure(EntityTypeBuilder<Permission> builder)
  {
    builder.HasKey(p => p.Id);
    builder.Property(p => p.Key).IsRequired().HasMaxLength(200);
    builder.Property(p => p.Description).HasMaxLength(DescriptionMaxLength);
    builder.Property(p => p.Group).HasMaxLength(100);
    builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);
    builder.Property(p => p.IsSystemGenerated).IsRequired().HasDefaultValue(false);

    // Metadatos técnicos
    builder.Property(p => p.Route).HasMaxLength(500);
    builder.Property(p => p.HttpMethod).HasMaxLength(10);
    builder.Property(p => p.FullNamespace).HasMaxLength(500);
    builder.Property(p => p.SourceFilePath).HasMaxLength(1000);
    builder.Property(p => p.EndpointMethodName).HasMaxLength(200);
    builder.Property(p => p.CqrsType).HasMaxLength(50);

    // Campos de auditoría de cambios
    builder.Property(p => p.PreviousKey).HasMaxLength(200);
    builder.Property(p => p.PreviousFilePath).HasMaxLength(1000);
    builder.Property(p => p.PreviousMethodName).HasMaxLength(200);
    builder.Property(p => p.LastSyncAt);

    // Índices únicos para garantizar integridad
    builder.HasIndex(p => p.Key).IsUnique();

    // AuditableEntityConfiguration<T> (generico abierto) nunca se aplica via
    // ApplyConfigurationsFromAssembly, por lo que esta entidad no tenia ningun indice de PublicId.
    builder.HasIndex(p => p.PublicId)
        .IsUnique()
        .HasDatabaseName("IX_Permissions_PublicId_Security");

    // Índices compuestos para consultas eficientes
    builder.HasIndex(p => new { p.IsActive, p.IsSystemGenerated });
    builder.HasIndex(p => new { p.Group, p.IsActive });

    // Los campos de auditoría se configuran automáticamente desde AuditableEntity
    builder.HasQueryFilter(p => !p.IsDeleted);
  }
}
