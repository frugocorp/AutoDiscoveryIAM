namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;

public static class UserStatusCatalog
{
  public const string Active = "active";
  public const string Inactive = "inactive";
  public const string Blocked = "blocked";
  public const string Suspended = "suspended";
  public const string Pending = "pending";

  /// <summary>
  /// Registro propio que espera la aprobación de un administrador
  /// (<c>Registration:RequireAdminApproval</c>). Confirmar el correo no lo activa: solo
  /// <c>POST /users/{publicId}/approve</c>.
  /// </summary>
  public const string PendingApproval = "pending_approval";

  /// <summary>
  /// Devuelve los estados considerados como "activos" para el usuario.
  /// </summary>
  public static IReadOnlyCollection<string> GetActiveStatuses() => new[] { Active };

  /// <summary>
  /// Devuelve los estados considerados como "no activos" para el usuario.
  /// </summary>
  public static IReadOnlyCollection<string> GetInactiveStatuses() => new[] { Inactive, Blocked, Suspended, Pending, PendingApproval };

  /// <summary>
  /// Devuelve todos los estados posibles.
  /// </summary>
  public static IReadOnlyCollection<string> GetAll() => new[] { Active, Inactive, Blocked, Suspended, Pending, PendingApproval };
}
