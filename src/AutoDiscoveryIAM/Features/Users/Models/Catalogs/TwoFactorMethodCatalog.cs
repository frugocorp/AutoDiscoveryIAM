namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;

/// <summary>
/// Métodos de segundo factor de autenticación soportados por <c>User.TwoFactorMethod</c>.
/// </summary>
public static class TwoFactorMethodCatalog
{
  public const string None = "none";
  public const string Email = "email";
  public const string Authenticator = "authenticator";

  /// <summary>
  /// Devuelve todos los métodos de 2FA válidos hoy (WhatsApp se agrega aquí cuando se implemente).
  /// </summary>
  public static IReadOnlyCollection<string> GetAll() => new[] { None, Email, Authenticator };
}
