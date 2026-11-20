using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;

/// <summary>
/// Configuración EF Core para la entidad Role del sistema RBAC.
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
  public void Configure(EntityTypeBuilder<Role> builder)
  {
    builder.HasKey(r => r.Id);
    builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
    builder.Property(r => r.Description).HasMaxLength(500);
    builder.Property(r => r.IsActive).IsRequired().HasDefaultValue(true);

    // Índices únicos
    builder.HasIndex(r => r.Name).IsUnique();

    // Índice único para PublicId (seguridad API)
    // Similar al User, este índice permite búsquedas seguras por PublicId
    // evitando exposición de IDs internos en endpoints como GetRoleByPublicId
    builder.HasIndex(r => r.PublicId)
        .IsUnique()
        .HasDatabaseName("IX_Roles_PublicId_Security");

    // Índice único filtrado: como mucho un rol puede tener IsDefaultForNewUser=true a la vez.
    // Sin esto, dos roles marcados como default dejarían la asignación en auto-registro ambigua.
    builder.HasIndex(r => r.IsDefaultForNewUser)
        .IsUnique()
        .HasFilter("[IsDefaultForNewUser] = 1")
        .HasDatabaseName("IX_Roles_IsDefaultForNewUser_Unique");

    // concurrencia optimista — SQL Server actualiza esta columna en cada UPDATE.
    builder.Property(r => r.RowVersion).IsRowVersion();

    // Los campos de auditoría se configuran automáticamente desde AuditableEntity
    builder.HasQueryFilter(r => !r.IsDeleted);
  }
}
