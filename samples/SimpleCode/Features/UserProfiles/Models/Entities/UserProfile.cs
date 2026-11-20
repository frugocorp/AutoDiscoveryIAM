using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Common.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace TuProyecto.Features.UserProfiles.Models.Entities;

/// <summary>
/// Entidad propia del consumidor con un FK real hacia el User de la librería -- solo es posible
/// porque ApplicationDbContext hereda AutoDiscoveryIAMDbContext (mismo modelo, misma base de datos).
/// User, Role, Permission, etc. son cerrados a modificación (Open/Closed Principle, ver README de
/// AutoDiscoveryIAM): nunca se heredan ni se les agregan propiedades. Si necesitas más datos por
/// usuario ("estado civil" u otro campo), este es el patrón -- una entidad satélite propia, 1:1 con
/// User.Id, no un cambio a User.
/// </summary>
public class UserProfile : AuditableEntity
{
  public long Id { get; set; }
  public long UserId { get; set; }
  public string Occupation { get; set; } = string.Empty;
  public string Biography { get; set; } = string.Empty;

  /// <summary>
  /// Uno de los valores de MaritalStatusCatalog.GetAll() -- validado en
  /// CreateUserProfileCommandValidator, no aquí (la entidad no valida, solo almacena).
  /// </summary>
  public string MaritalStatus { get; set; } = string.Empty;

  /// <summary>
  /// Navegación unidireccional -- User no tiene (ni necesita) un ICollection&lt;UserProfile&gt; de
  /// vuelta, precisamente porque no se toca la clase de la librería.
  /// </summary>
  public virtual User User { get; set; } = null!;
}
