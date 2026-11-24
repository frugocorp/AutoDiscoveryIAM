using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Pruebas del almacén del par de tokens de la ventana de gracia.
/// </summary>
public class RefreshTokenGraceStoreTests
{
  private readonly IDistributedCache _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
  private readonly RefreshTokenGraceStore _store;

  public RefreshTokenGraceStoreTests()
  {
    _store = new RefreshTokenGraceStore(_cache, new EphemeralDataProtectionProvider());
  }

  [Fact(DisplayName = "Guarda y devuelve el mismo par, cifrado en el caché")]
  public async Task StoreAndGet_RoundTripsEncryptedPair()
  {
    var pair = new RefreshTokenResponseDto("access-token", "plain-refresh", "Bearer", 3600, DateTime.UtcNow.AddHours(1));
    var hash = RefreshTokenHasher.Hash("plain-refresh");

    await _store.StoreAsync(hash, pair, TimeSpan.FromSeconds(30), CancellationToken.None);

    (await _store.GetAsync(hash, CancellationToken.None)).Should().Be(pair);
    var raw = await _cache.GetStringAsync("adiam:refresh-grace:" + hash, CancellationToken.None);
    raw!.Contains("plain-refresh", StringComparison.Ordinal).Should().BeFalse("el caché nunca guarda el token en claro");
  }

  [Fact(DisplayName = "Un par que no corresponde al hash pedido no se entrega")]
  public async Task Get_WhenPairDoesNotMatchHash_ReturnsNull()
  {
    var pair = new RefreshTokenResponseDto("access-token", "plain-refresh", "Bearer", 3600, DateTime.UtcNow.AddHours(1));

    await _store.StoreAsync("otro-hash", pair, TimeSpan.FromSeconds(30), CancellationToken.None);

    (await _store.GetAsync("otro-hash", CancellationToken.None)).Should().BeNull();
  }

  [Fact(DisplayName = "Sin par guardado devuelve null")]
  public async Task Get_WhenMissing_ReturnsNull()
  {
    (await _store.GetAsync("inexistente", CancellationToken.None)).Should().BeNull();
  }
}
