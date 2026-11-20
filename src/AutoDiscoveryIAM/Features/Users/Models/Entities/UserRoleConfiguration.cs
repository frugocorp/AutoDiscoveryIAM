using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

/// <summary>
/// Configuración EF Core para la entidad UserRole del sistema RBAC.
/// </summary>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
  public void Configure(EntityTypeBuilder<UserRole> builder)
  {
    builder.HasKey(ur => ur.Id);
    builder.Property(ur => ur.UserId).IsRequired();
    builder.Property(ur => ur.RoleId).IsRequired();
    builder.Property(ur => ur.AssignedAt).IsRequired();
    builder.Property(ur => ur.ExpiresAt);

    // Índice único para evitar duplicados
    builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();

    // AuditableEntityConfiguration<T> (generico abierto) nunca se aplica via
    // ApplyConfigurationsFromAssembly, por lo que esta entidad no tenia ningun indice de PublicId.
    builder.HasIndex(ur => ur.PublicId)
        .IsUnique()
        .HasDatabaseName("IX_UserRoles_PublicId_Security");

    // Índice compuesto para performance en JOINs
    // Este índice acelera las consultas que buscan roles por usuario
    // especialmente útil para queries como: u.UserRoles donde se hace JOIN con Users
    // Performance: Convierte Table Scan (lento) en Index Seek (súper rápido)
    builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
        .HasDatabaseName("IX_UserRoles_UserId_RoleId_Performance");

    // Relaciones Foreign Key
    builder.HasOne(ur => ur.User)
        .WithMany(u => u.UserRoles)
        .HasForeignKey(ur => ur.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne(ur => ur.Role)
        .WithMany(r => r.UserRoles)
        .HasForeignKey(ur => ur.RoleId)
        .OnDelete(DeleteBehavior.Cascade);

    // Los campos de auditoría se configuran automáticamente desde AuditableEntity
    builder.HasQueryFilter(ur => !ur.IsDeleted);
  }
}
