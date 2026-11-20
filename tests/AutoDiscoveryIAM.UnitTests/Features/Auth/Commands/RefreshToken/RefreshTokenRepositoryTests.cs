using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.RefreshToken;

public class RefreshTokenRepositoryTests
{
  [Fact]
  public async Task GetByTokenAsync_ShouldReturnTokenIncludingUser()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var user = new User { Username = "rt-user", Email = "rt-user@test.local", PasswordHash = "hash", IsDeleted = false };
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    var token = new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken
    {
      UserId = user.Id,
      User = user,
      Token = RefreshTokenHasher.Hash("token-1"),
      ExpiresAt = DateTime.UtcNow.AddMinutes(10),
    };
    context.RefreshTokens.Add(token);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RefreshTokenRepository(context);

    var result = await repository.GetByTokenAsync("token-1", CancellationToken.None);

    result.Should().NotBeNull();
    result!.User.Email.Should().Be(user.Email);
  }

  [Fact]
  public async Task GetByTokenAsync_WhenColumnStoresPlaintext_ShouldNotMatch()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var user = new User { Username = "rt-plain", Email = "rt-plain@test.local", PasswordHash = "hash", IsDeleted = false };
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    // Simula una fila legada/incorrecta con el token en claro (no el hash) en la columna Token.
    var token = new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken
    {
      UserId = user.Id,
      User = user,
      Token = "token-in-plaintext",
      ExpiresAt = DateTime.UtcNow.AddMinutes(10),
    };
    context.RefreshTokens.Add(token);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RefreshTokenRepository(context);

    var result = await repository.GetByTokenAsync("token-in-plaintext", CancellationToken.None);

    result.Should().BeNull("la busqueda hashea el valor recibido y ya no compara contra texto plano");
  }

  [Fact]
  public async Task CreateAsync_ShouldPersistToken()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var user = new User { Username = "rt-create", Email = "rt-create@test.local", PasswordHash = "hash", IsDeleted = false };
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RefreshTokenRepository(context);
    var refreshToken = new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken
    {
      UserId = user.Id,
      User = user,
      Token = "create-token",
      ExpiresAt = DateTime.UtcNow.AddHours(1),
    };

    var created = await repository.CreateAsync(refreshToken, CancellationToken.None);

    created.Id.Should().BeGreaterThan(0);
    context.RefreshTokens.Count(rt => rt.Token == "create-token").Should().Be(1);
  }

  [Fact]
  public async Task RevokeUserTokensAsync_ShouldRevokeOnlyActiveTokensForUser()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var user = new User { Username = "rt-revoke", Email = "rt-revoke@test.local", PasswordHash = "hash", IsDeleted = false };
    var otherUser = new User { Username = "rt-other", Email = "rt-other@test.local", PasswordHash = "hash", IsDeleted = false };
    context.Users.AddRange(user, otherUser);
    await context.SaveChangesAsync(CancellationToken.None);

    context.RefreshTokens.AddRange(
      new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken { UserId = user.Id, User = user, Token = "u-active", ExpiresAt = DateTime.UtcNow.AddHours(1), IsRevoked = false },
      new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken { UserId = user.Id, User = user, Token = "u-revoked", ExpiresAt = DateTime.UtcNow.AddHours(1), IsRevoked = true },
      new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken { UserId = otherUser.Id, User = otherUser, Token = "other-active", ExpiresAt = DateTime.UtcNow.AddHours(1), IsRevoked = false });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RefreshTokenRepository(context);

    await repository.RevokeUserTokensAsync(user.Id, "logout-all", CancellationToken.None);

    var activeUserToken = await context.RefreshTokens.FirstAsync(x => x.Token == "u-active", CancellationToken.None);
    var alreadyRevoked = await context.RefreshTokens.FirstAsync(x => x.Token == "u-revoked", CancellationToken.None);
    var otherToken = await context.RefreshTokens.FirstAsync(x => x.Token == "other-active", CancellationToken.None);

    activeUserToken.IsRevoked.Should().BeTrue();
    activeUserToken.RevocationReason.Should().Be("logout-all");
    alreadyRevoked.RevocationReason.Should().BeNull();
    otherToken.IsRevoked.Should().BeFalse();
  }

  [Fact]
  public async Task CleanExpiredTokensAsync_ShouldRemoveOnlyExpiredTokens()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var user = new User { Username = "rt-clean", Email = "rt-clean@test.local", PasswordHash = "hash", IsDeleted = false };
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    context.RefreshTokens.AddRange(
      new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken { UserId = user.Id, User = user, Token = "expired", ExpiresAt = DateTime.UtcNow.AddHours(-2) },
      new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken { UserId = user.Id, User = user, Token = "alive", ExpiresAt = DateTime.UtcNow.AddHours(2) });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RefreshTokenRepository(context);

    await repository.CleanExpiredTokensAsync(CancellationToken.None);

    context.RefreshTokens.Count(x => x.Token == "alive").Should().Be(1);
  }
}
