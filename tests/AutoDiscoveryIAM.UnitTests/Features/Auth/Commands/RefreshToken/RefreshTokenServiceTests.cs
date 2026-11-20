using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.RefreshToken;

public class RefreshTokenServiceTests
{
  private readonly RefreshTokenService _service;

  public RefreshTokenServiceTests()
  {
    var settings = new RefreshTokenSettings { ExpiryDays = 7, TokenLength = 32 };
    var options = Substitute.For<IOptions<RefreshTokenSettings>>();
    options.Value.Returns(settings);
    _service = new RefreshTokenService(options);
  }

  [Fact]
  public void GenerateRefreshToken_ShouldReturnTokenWithCorrectUserId()
  {
    var user = BuildUser();

    var token = _service.GenerateRefreshToken(user, "127.0.0.1", "test-agent");

    token.Should().NotBeNull();
    token.UserId.Should().Be(user.Id);
    token.IpAddress.Should().Be("127.0.0.1");
    token.UserAgent.Should().Be("test-agent");
    token.IsRevoked.Should().BeFalse();
    token.Token.Should().NotBeNullOrWhiteSpace();
    token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
  }

  [Fact]
  public void GenerateRefreshToken_ShouldStoreHashAndKeepPlaintextSeparate()
  {
    var user = BuildUser();

    var token = _service.GenerateRefreshToken(user);

    token.PlaintextToken.Should().NotBeNullOrWhiteSpace();
    token.Token.Should().NotBe(token.PlaintextToken!, "la columna Token debe almacenar el hash, no el valor en claro");
    token.Token.Should().Be(RefreshTokenHasher.Hash(token.PlaintextToken!));
  }

  [Fact]
  public void GenerateTokenString_ShouldReturnNonEmptyBase64String()
  {
    var tokenString = _service.GenerateTokenString();

    tokenString.Should().NotBeNullOrWhiteSpace();
    tokenString.Length.Should().BeGreaterThan(0);
  }

  [Fact]
  public void ValidateRefreshToken_WhenTokenIsActive_ShouldReturnTrue()
  {
    var user = BuildUser();
    var token = _service.GenerateRefreshToken(user);

    var result = _service.ValidateRefreshToken(token);

    result.Should().BeTrue();
  }

  [Fact]
  public void ValidateRefreshToken_WhenTokenIsRevoked_ShouldReturnFalse()
  {
    var user = BuildUser();
    var token = _service.GenerateRefreshToken(user);
    token.IsRevoked = true;

    var result = _service.ValidateRefreshToken(token);

    result.Should().BeFalse();
  }

  [Fact]
  public void ValidateRefreshToken_WhenTokenIsExpired_ShouldReturnFalse()
  {
    var user = BuildUser();
    var token = new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken
    {
      Token = "expired-token",
      UserId = user.Id,
      User = user,
      ExpiresAt = DateTime.UtcNow.AddDays(-1),
      IsRevoked = false,
    };

    var result = _service.ValidateRefreshToken(token);

    result.Should().BeFalse();
  }

  private static User BuildUser() => new()
  {
    Id = 5,
    PublicId = Guid.NewGuid(),
    Username = "svc-user",
    Email = "svc@test.local",
    PasswordHash = "hash",
    Status = "active",
    IsActive = true,
    IsDeleted = false,
  };
}
