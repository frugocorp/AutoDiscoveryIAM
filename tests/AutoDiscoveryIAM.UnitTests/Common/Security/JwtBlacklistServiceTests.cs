using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Common.Security;

public class JwtBlacklistServiceTests
{
    private static JwtBlacklistService CreateSut() => new(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));

    [Fact]
    public async Task IsBlacklisted_WhenJtiNeverBlacklisted_ReturnsFalse()
    {
        var sut = CreateSut();

        (await sut.IsBlacklistedAsync(Guid.NewGuid().ToString(), CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task IsBlacklisted_WhenJtiWasBlacklisted_ReturnsTrue()
    {
        var sut = CreateSut();
        var jti = Guid.NewGuid().ToString();

        await sut.BlacklistAsync(jti, DateTime.UtcNow.AddMinutes(5), CancellationToken.None);

        (await sut.IsBlacklistedAsync(jti, CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task IsBlacklisted_AfterExpiration_ReturnsFalse()
    {
        var sut = CreateSut();
        var jti = Guid.NewGuid().ToString();

        await sut.BlacklistAsync(jti, DateTime.UtcNow.AddMilliseconds(-1), CancellationToken.None);

        (await sut.IsBlacklistedAsync(jti, CancellationToken.None)).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blacklist_WhenJtiIsNullOrWhitespace_DoesNotBlacklistAnything(string? jti)
    {
        var sut = CreateSut();

        await sut.BlacklistAsync(jti!, DateTime.UtcNow.AddMinutes(5), CancellationToken.None);

        (await sut.IsBlacklistedAsync(jti!, CancellationToken.None)).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsBlacklisted_WhenJtiIsNullOrWhitespace_ReturnsFalse(string? jti)
    {
        var sut = CreateSut();

        (await sut.IsBlacklistedAsync(jti!, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task IsBlacklisted_IsIndependentPerJti()
    {
        var sut = CreateSut();
        var blacklistedJti = Guid.NewGuid().ToString();
        var otherJti = Guid.NewGuid().ToString();

        await sut.BlacklistAsync(blacklistedJti, DateTime.UtcNow.AddMinutes(5), CancellationToken.None);

        (await sut.IsBlacklistedAsync(blacklistedJti, CancellationToken.None)).Should().BeTrue();
        (await sut.IsBlacklistedAsync(otherJti, CancellationToken.None)).Should().BeFalse();
    }
}
