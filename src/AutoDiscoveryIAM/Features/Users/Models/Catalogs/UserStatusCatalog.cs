namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;

public static class UserStatusCatalog
{
  public const string Active = "active";
  public const string Inactive = "inactive";
  public const string Blocked = "blocked";
  public const string Suspended = "suspended";
  public const string Pending = "pending";

  /// <summary>
  /// Devuelve los estados considerados como "activos" para el usuario.
  /// </summary>
  public static IReadOnlyCollection<string> GetActiveStatuses() => new[] { Active };

  /// <summary>
  /// Devuelve los estados considerados como "no activos" para el usuario.
  /// </summary>
  public static IReadOnlyCollection<string> GetInactiveStatuses() => new[] { Inactive, Blocked, Suspended, Pending };

  /// <summary>
  /// Devuelve todos los estados posibles.
  /// </summary>
  public static IReadOnlyCollection<string> GetAll() => new[] { Active, Inactive, Blocked, Suspended, Pending };
}
