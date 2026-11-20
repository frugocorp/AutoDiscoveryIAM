using Microsoft.Extensions.Caching.Distributed;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;

/// <inheritdoc cref="IJwtBlacklistService"/>
public sealed class JwtBlacklistService(IDistributedCache cache) : IJwtBlacklistService
{
  private const string CacheKeyPrefix = "adiam:jwt-blacklist:";

  private static readonly byte[] Marker = [1];

  private readonly IDistributedCache _cache = cache;

  public async Task BlacklistAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(jti))
    {
      return;
    }

    var expiresAt = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc));

    // Un token ya expirado no necesita entrada: la validación de lifetime ya lo rechaza.
    if (expiresAt <= DateTimeOffset.UtcNow)
    {
      return;
    }

    await _cache.SetAsync(
      CacheKeyPrefix + jti,
      Marker,
      new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt },
      cancellationToken);
  }

  public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
  {
    return !string.IsNullOrWhiteSpace(jti) && await _cache.GetAsync(CacheKeyPrefix + jti, cancellationToken) is not null;
  }
}
