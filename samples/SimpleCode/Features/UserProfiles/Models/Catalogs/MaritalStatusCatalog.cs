namespace TuProyecto.Features.UserProfiles.Models.Catalogs;

/// <summary>
/// Catálogo de estados civiles válidos para un UserProfile -- una lista cerrada de valores
/// permitidos (en vez de un string libre), validable en el Validator del Command (ver
/// UserProfileValidationRules) sin necesitar una tabla propia en base de datos para algo que no
/// cambia en runtime. Mismo patrón que usa la librería para sus propios catálogos cerrados (ver
/// Features/Users/Models/Catalogs/UserStatusCatalog.cs).
/// </summary>
public static class MaritalStatusCatalog
{
  public const string Soltero = "soltero";
  public const string Casado = "casado";
  public const string Divorciado = "divorciado";
  public const string Viudo = "viudo";
  public const string UnionLibre = "union_libre";

  /// <summary>
  /// Devuelve todos los estados civiles válidos.
  /// </summary>
  public static IReadOnlyCollection<string> GetAll() => new[] { Soltero, Casado, Divorciado, Viudo, UnionLibre };
}
