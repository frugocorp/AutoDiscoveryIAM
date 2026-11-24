// -----------------------------------------------------------------------
// <copyright file="RefreshTokenGraceStore.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Guarda por unos segundos el par de tokens que entregó una rotación, para devolver el mismo par si
/// el cliente reintenta con el refresh token recién rotado (respuesta perdida, dos refresh a la vez).
/// </summary>
public interface IRefreshTokenGraceStore
{
  /// <summary>
  /// Guarda el par emitido, identificado por el hash del refresh token nuevo.
  /// </summary>
  Task StoreAsync(string newTokenHash, RefreshTokenResponseDto response, TimeSpan lifetime, CancellationToken cancellationToken);

  /// <summary>
  /// Devuelve el par emitido con ese refresh token nuevo, o <c>null</c> si ya venció o no existe.
  /// </summary>
  Task<RefreshTokenResponseDto?> GetAsync(string newTokenHash, CancellationToken cancellationToken);
}

/// <summary>
/// Implementación sobre <see cref="IDistributedCache"/>. El par se guarda cifrado con Data
/// Protection: con un caché compartido (Redis, SQL Server) los tokens en claro no quedan legibles
/// en el servidor del caché.
/// </summary>
public sealed class RefreshTokenGraceStore(IDistributedCache cache, IDataProtectionProvider dataProtectionProvider) : IRefreshTokenGraceStore
{
  private const string KeyPrefix = "adiam:refresh-grace:";
  private readonly IDistributedCache _cache = cache;
  private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector("FrugoCorp.AspNetCore.AutoDiscoveryIAM.RefreshTokenGrace");

  /// <inheritdoc />
  public Task StoreAsync(string newTokenHash, RefreshTokenResponseDto response, TimeSpan lifetime, CancellationToken cancellationToken)
  {
    var payload = _protector.Protect(JsonSerializer.Serialize(response));
    return _cache.SetStringAsync(
      KeyPrefix + newTokenHash,
      payload,
      new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = lifetime },
      cancellationToken);
  }

  /// <inheritdoc />
  public async Task<RefreshTokenResponseDto?> GetAsync(string newTokenHash, CancellationToken cancellationToken)
  {
    var payload = await _cache.GetStringAsync(KeyPrefix + newTokenHash, cancellationToken);
    if (payload == null)
    {
      return null;
    }

    try
    {
      var response = JsonSerializer.Deserialize<RefreshTokenResponseDto>(_protector.Unprotect(payload));

      // El par debe corresponder al token nuevo que registró la base, no a otro.
      return response?.RefreshToken != null && string.Equals(RefreshTokenHasher.Hash(response.RefreshToken), newTokenHash, StringComparison.Ordinal)
        ? response
        : null;
    }
    catch (CryptographicException)
    {
      // Claves de Data Protection distintas (otra instancia sin anillo compartido): se trata como
      // si no hubiera par guardado.
      return null;
    }
  }
}
