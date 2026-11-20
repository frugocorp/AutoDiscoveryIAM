using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TuProyecto.Features.UserProfiles.Models.Entities;

/// <summary>
/// Configuración EF Core para la entidad UserProfile -- vive junto a la entidad, no centralizada en
/// el DbContext (mismo patrón que usa la librería, ver src/AutoDiscoveryIAM/Features/Roles/Models/Entities/RoleConfiguration.cs).
/// </summary>
public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
  public void Configure(EntityTypeBuilder<UserProfile> builder)
  {
    builder.HasKey(p => p.Id);
    builder.Property(p => p.Occupation).IsRequired().HasMaxLength(200);
    builder.Property(p => p.Biography).IsRequired();
    builder.Property(p => p.MaritalStatus).IsRequired().HasMaxLength(50);

    // Índice único para PublicId (mismo motivo que en la librería: nunca exponer el Id interno en rutas).
    builder.HasIndex(p => p.PublicId).IsUnique();

    // Un perfil por usuario -- también evita más de un UserProfile activo para el mismo UserId.
    builder.HasIndex(p => p.UserId).IsUnique();

    // FK real hacia el User de la librería: solo funciona porque ApplicationDbContext hereda
    // AutoDiscoveryIAMDbContext (mismo modelo/base de datos que Users). WithMany() sin argumento
    // configura la relación sin exigir un ICollection<UserProfile> de vuelta en User -- User no se
    // toca (ver UserProfile.cs).
    builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);

    // El filtro de soft-delete (WHERE !IsDeleted) NO se declara aquí -- lo aplica
    // ApplySoftDeleteQueryFilters() en el OnModelCreating de ApplicationDbContext, por convención,
    // a cualquier entidad que herede AuditableEntity.
  }
}
