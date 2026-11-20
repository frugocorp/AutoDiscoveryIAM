using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using RefreshTokenEntity = FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.RefreshToken;

public class RefreshTokenHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAuthLogger _authLogger;
    private readonly ISecurityEventLogger _securityEventLogger;
    private readonly IOptions<JwtSettings> _jwtSettings;
    private readonly IJwtClaimsFactory _jwtClaimsFactory;
    private readonly RefreshTokenHandler _handler;

    public RefreshTokenHandlerTests()
    {
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _refreshTokenService = Substitute.For<IRefreshTokenService>();
        _authLogger = Substitute.For<IAuthLogger>();
        _securityEventLogger = Substitute.For<ISecurityEventLogger>();

        var settings = new JwtSettings
        {
            Key = "ThisIsAVeryLongSecretKeyForTestingPurposesWithAtLeast256BitsAbc",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60,
        };
        _jwtSettings = Substitute.For<IOptions<JwtSettings>>();
        _jwtSettings.Value.Returns(settings);
        _jwtClaimsFactory = new JwtClaimsFactory(_jwtSettings);

        _handler = new RefreshTokenHandler(_refreshTokenRepository, _refreshTokenService, _authLogger, _jwtClaimsFactory, _jwtSettings, _securityEventLogger);
    }

    [Fact]
    public async Task Handle_WhenTokenIsValid_ShouldReturnNewTokensAndRotate()
    {
        const string tokenValue = "valid-refresh-token";
        var user = BuildActiveUser();
        var storedToken = BuildActiveToken(tokenValue, user);
        const string newTokenValue = "new-refresh-token";
        var newRefreshToken = BuildActiveToken(newTokenValue, user);

        _refreshTokenRepository.GetByTokenAsync(tokenValue, Arg.Any<CancellationToken>())
            .Returns(storedToken);
        _refreshTokenService.ValidateRefreshToken(storedToken).Returns(true);
        _refreshTokenService.GenerateRefreshToken(user, Arg.Any<string>(), Arg.Any<string>())
            .Returns(newRefreshToken);
        _refreshTokenRepository.UpdateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _refreshTokenRepository.CreateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>())
            .Returns(newRefreshToken);

        var command = new RefreshTokenCommand(tokenValue);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Data.RefreshToken.Should().Be(newTokenValue);
        result.Data.TokenType.Should().Be("Bearer");
        storedToken.IsRevoked.Should().BeTrue();
        storedToken.RevocationReason.Should().Be("Token rotated");
        _authLogger.Received(1).LogLoginSuccess(user.Email);
    }

    [Fact(DisplayName = "Handle retorna 401 sin revocar toda la sesion cuando otra peticion concurrente ya roto el mismo token (issue #075)")]
    public async Task Handle_WhenConcurrentRotationConflictOccurs_ShouldReturnUnauthorizedWithoutRevokingSession()
    {
        const string tokenValue = "raced-refresh-token";
        var user = BuildActiveUser();
        var storedToken = BuildActiveToken(tokenValue, user);
        var newRefreshToken = BuildActiveToken("new-token-that-never-persists", user);

        _refreshTokenRepository.GetByTokenAsync(tokenValue, Arg.Any<CancellationToken>())
            .Returns(storedToken);
        _refreshTokenService.ValidateRefreshToken(storedToken).Returns(true);
        _refreshTokenService.GenerateRefreshToken(user, Arg.Any<string>(), Arg.Any<string>())
            .Returns(newRefreshToken);
        _refreshTokenRepository.UpdateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("Row was modified concurrently."));

        var command = new RefreshTokenCommand(tokenValue);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
        await _refreshTokenRepository.DidNotReceive().RevokeUserTokensAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ShouldReturnUnauthorized()
    {
        _refreshTokenRepository.GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshTokenEntity?)null);

        var command = new RefreshTokenCommand("nonexistent-token");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
        _authLogger.Received(1).LogLoginFailed("unknown");
    }

    [Fact]
    public async Task Handle_WhenTokenIsRevoked_ShouldDetectReuseAndRevokeAllUserTokens()
    {
        const string tokenValue = "revoked-token";
        var user = BuildActiveUser();
        var revokedToken = BuildActiveToken(tokenValue, user);
        revokedToken.IsRevoked = true;
        revokedToken.RevocationReason = "Token rotated";

        _refreshTokenRepository.GetByTokenAsync(tokenValue, Arg.Any<CancellationToken>())
            .Returns(revokedToken);

        var command = new RefreshTokenCommand(tokenValue, "203.0.113.5");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
        _authLogger.Received(1).LogLoginFailed(user.Email);
        await _refreshTokenRepository.Received(1).RevokeUserTokensAsync(user.Id, "refresh_token_reuse_detected", Arg.Any<CancellationToken>());
        _securityEventLogger.Received(1).LogSecurityEvent(
            SecurityEventType.RefreshTokenReuseDetected,
            user.Email,
            "203.0.113.5",
            "/auth/refresh",
            Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenTokenIsExpiredButNotRevoked_ShouldReturnUnauthorizedWithoutRevokingSession()
    {
        const string tokenValue = "expired-token";
        var user = BuildActiveUser();
        var expiredToken = BuildActiveToken(tokenValue, user);
        expiredToken.IsRevoked = false;

        _refreshTokenRepository.GetByTokenAsync(tokenValue, Arg.Any<CancellationToken>())
            .Returns(expiredToken);
        _refreshTokenService.ValidateRefreshToken(expiredToken).Returns(false);

        var command = new RefreshTokenCommand(tokenValue);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
        _authLogger.Received(1).LogLoginFailed(user.Email);
        await _refreshTokenRepository.DidNotReceive().RevokeUserTokensAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ShouldReturnUnauthorized()
    {
        const string tokenValue = "token-inactive-user";
        var user = BuildActiveUser();
        user.IsActive = false;
        var storedToken = BuildActiveToken(tokenValue, user);

        _refreshTokenRepository.GetByTokenAsync(tokenValue, Arg.Any<CancellationToken>())
            .Returns(storedToken);
        _refreshTokenService.ValidateRefreshToken(storedToken).Returns(true);

        var command = new RefreshTokenCommand(tokenValue);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
        _authLogger.Received(1).LogLoginFailed(user.Email);
    }

    [Fact]
    public async Task Handle_WhenPersistenceFails_ShouldBubbleExceptionToMiddleware()
    {
        const string tokenValue = "valid-refresh-token";
        var user = BuildActiveUser();
        var storedToken = BuildActiveToken(tokenValue, user);
        const string newTokenValue = "new-refresh-token";
        var newRefreshToken = BuildActiveToken(newTokenValue, user);

        _refreshTokenRepository.GetByTokenAsync(tokenValue, Arg.Any<CancellationToken>())
            .Returns(storedToken);
        _refreshTokenService.ValidateRefreshToken(storedToken).Returns(true);
        _refreshTokenService.GenerateRefreshToken(user, Arg.Any<string>(), Arg.Any<string>())
            .Returns(newRefreshToken);
        _refreshTokenRepository.UpdateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("db failure"));

        var command = new RefreshTokenCommand(tokenValue);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static User BuildActiveUser() => new()
    {
        Id = 1,
        PublicId = Guid.NewGuid(),
        Username = "testuser",
        Email = "test@test.local",
        PasswordHash = "hash",
        Status = "active",
        IsActive = true,
        IsDeleted = false,
    };

    private static RefreshTokenEntity BuildActiveToken(string tokenValue, User user) => new()
    {
        Id = 1,
        Token = tokenValue,
        PlaintextToken = tokenValue,
        UserId = user.Id,
        User = user,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        IsRevoked = false,
    };
}
