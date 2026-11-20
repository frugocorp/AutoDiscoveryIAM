namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;

/// <summary>
/// Permite invalidar un access token JWT concreto (por su claim <c>jti</c>) antes de que expire
/// por sí solo, para casos como logout explícito o incidentes de seguridad.
/// </summary>
/// <remarks>
/// Implementado sobre <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>. La
/// librería registra la versión en memoria (<c>AddDistributedMemoryCache</c>), suficiente con una sola
/// instancia de la API. Con varias instancias, registra un caché distribuido compartido (Redis, SQL
/// Server) en tu <c>Program.cs</c>: si no, un logout solo invalida el token en la instancia que lo
/// atendió.
/// </remarks>
public interface IJwtBlacklistService
{
  /// <summary>
  /// Marca un <c>jti</c> como inválido hasta su expiración natural.
  /// </summary>
  /// <param name="jti">Identificador único del token (claim <c>jti</c>).</param>
  /// <param name="expiresAtUtc">Expiración natural del token, en UTC; la entrada no necesita vivir más que eso.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  Task BlacklistAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default);

  /// <summary>
  /// Indica si un <c>jti</c> fue invalidado antes de su expiración natural.
  /// </summary>
  Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default);
}
