using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Auditing;
using Microsoft.EntityFrameworkCore;
using TuProyecto.Features.UserProfiles.Models.Entities;

namespace TuProyecto.Data;

/// <summary>
/// Un solo DbContext: hereda AutoDiscoveryIAMDbContext en vez de declarar el tuyo aparte -- une el
/// modelo de la librería (User, Role, Permission, ...) y el tuyo propio (UserProfile) en una sola
/// base de datos/migración, habilitando FKs y joins nativos entre ambos (ver UserProfile.User y
/// GetUserProfilesRepository). Es una opción, no una obligación: ver "Elige tu propio DbContext: uno
/// o dos" en el README de AutoDiscoveryIAM para la alternativa de dos DbContexts separados (más
/// desacoplada, pero sin joins nativos) y cuándo conviene cada una.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentAuditUserService? currentAuditUserService = null)
  : AutoDiscoveryIAMDbContext(options, currentAuditUserService)
{
  public DbSet<UserProfile> UserProfiles { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder); // aplica los configs de la librería (User, Role, Permission, ...)

    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

    // Reaplica el mismo filtro WHERE !IsDeleted a las entidades de la librería (idéntico al que
    // ya traían) y lo agrega, sin repetirlo a mano, a tus propias entidades que hereden
    // AuditableEntity (aquí, UserProfile) -- ver AuditableEntityModelBuilderExtensions.
    modelBuilder.ApplySoftDeleteQueryFilters();
  }
}
