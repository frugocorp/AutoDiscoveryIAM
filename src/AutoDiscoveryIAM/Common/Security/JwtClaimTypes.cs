namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;

/// <summary>
/// Nombres de claims personalizados usados en los JWT del proyecto, para tener una única fuente
/// de verdad entre quien emite el token (<see cref="FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.JwtClaimsFactory"/>)
/// y quien lo valida (<c>OnTokenValidated</c> en <c>Program.cs</c>).
/// </summary>
public static class JwtClaimTypes
{
  /// <summary>
  /// Marca de tiempo (segundos Unix, UTC) de <see cref="Features.Users.Models.Entities.User.TokenValidFrom"/>
  /// al momento de emitir el token. Permite invalidar todos los tokens de un usuario emitidos
  /// antes de un cambio de contraseña, sin necesidad de rastrear cada token individualmente.
  /// </summary>
  public const string TokenValidFrom = "tvf";
}
