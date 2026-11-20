using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;

/// <summary>
/// Configuración EF Core para la entidad RolePermission del sistema RBAC.
/// </summary>
public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
  public void Configure(EntityTypeBuilder<RolePermission> builder)
  {
    builder.HasKey(rp => rp.Id);
    builder.Property(rp => rp.RoleId).IsRequired();
    builder.Property(rp => rp.PermissionId).IsRequired();
    builder.Property(rp => rp.AssignedAt).IsRequired();

    // Índice único para evitar duplicados
    builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();

    // AuditableEntityConfiguration<T> (generico abierto) nunca se aplica via
    // ApplyConfigurationsFromAssembly, por lo que esta entidad no tenia ningun indice de PublicId.
    builder.HasIndex(rp => rp.PublicId)
        .IsUnique()
        .HasDatabaseName("IX_RolePermissions_PublicId_Security");

    // Índice compuesto para performance en JOINs
    // Este índice acelera las consultas que buscan permisos por rol
    // especialmente útil para queries como: ur.Role.RolePermissions
    // Performance: Permite JOINs instantáneos en lugar de recorrer toda la tabla
    builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
        .HasDatabaseName("IX_RolePermissions_RoleId_PermissionId_Performance");

    // Relaciones Foreign Key
    builder.HasOne(rp => rp.Role)
        .WithMany(r => r.RolePermissions)
        .HasForeignKey(rp => rp.RoleId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(rp => rp.Permission)
        .WithMany(p => p.RolePermissions)
        .HasForeignKey(rp => rp.PermissionId)
        .OnDelete(DeleteBehavior.Cascade);

    // Los campos de auditoría se configuran automáticamente desde AuditableEntity
    builder.HasQueryFilter(rp => !rp.IsDeleted);
  }
}
