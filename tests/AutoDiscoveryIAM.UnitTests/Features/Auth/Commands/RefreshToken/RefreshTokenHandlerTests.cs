using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
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
    private readonly IRefreshTokenGraceStore _graceStore = Substitute.For<IRefreshTokenGraceStore>();
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

        _handler = CreateHandler(gracePeriodSeconds: 0);
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

    private RefreshTokenHandler CreateHandler(int gracePeriodSeconds) => new(
        _refreshTokenRepository,
        _refreshTokenService,
        _authLogger,
        _jwtClaimsFactory,
        _jwtSettings,
        _securityEventLogger,
        _graceStore,
        Options.Create(new RefreshTokenSettings { ReuseGracePeriodSeconds = gracePeriodSeconds }));

    private static RefreshTokenEntity BuildRecentlyRotatedToken(string tokenValue, User user, string successorHash, TimeSpan rotatedAgo)
    {
        var token = BuildActiveToken(tokenValue, user);
        token.IsRevoked = true;
        token.RevocationReason = "Token rotated";
        token.RevokedAt = TimeHelpers.GetCurrentMexicoTime() - rotatedAgo;
        token.ReplacedByToken = successorHash;
        return token;
    }

    [Fact(DisplayName = "Ventana de gracia: reusar un token recién rotado devuelve el mismo par sin revocar la sesión")]
    public async Task Handle_WithinGracePeriod_ReturnsSamePairWithoutRevoking()
    {
        var user = BuildActiveUser();
        var rotated = BuildRecentlyRotatedToken("rotated-token", user, "successor-hash", TimeSpan.FromSeconds(5));
        var successor = BuildActiveToken("successor-hash", user);
        var cachedPair = new RefreshTokenResponseDto("access", "successor-plain", "Bearer", 3600, DateTime.UtcNow.AddHours(1));
        _refreshTokenRepository.GetByTokenAsync("rotated-token", Arg.Any<CancellationToken>()).Returns(rotated);
        _refreshTokenRepository.GetByHashAsync("successor-hash", Arg.Any<CancellationToken>()).Returns(successor);
        _graceStore.GetAsync("successor-hash", Arg.Any<CancellationToken>()).Returns(cachedPair);

        var result = await CreateHandler(gracePeriodSeconds: 30).Handle(new RefreshTokenCommand("rotated-token"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(cachedPair);
        await _refreshTokenRepository.DidNotReceive().RevokeUserTokensAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _refreshTokenService.DidNotReceive().GenerateRefreshToken(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory(DisplayName = "Ventana de gracia desde otro cliente (otra IP u otro User-Agent): se trata como reuso y revoca toda la sesión")]
    [InlineData("10.0.0.9", "Navegador/1.0")]
    [InlineData("10.0.0.1", "OtroBot/2.0")]
    [InlineData(null, "Navegador/1.0")]
    public async Task Handle_WithinGracePeriodFromAnotherClient_RevokesSession(string? ipAddress, string userAgent)
    {
        var user = BuildActiveUser();
        var rotated = BuildRecentlyRotatedToken("rotated-token", user, "successor-hash", TimeSpan.FromSeconds(5));
        var successor = BuildActiveToken("successor-hash", user);
        successor.IpAddress = "10.0.0.1";
        successor.UserAgent = "Navegador/1.0";
        _refreshTokenRepository.GetByTokenAsync("rotated-token", Arg.Any<CancellationToken>()).Returns(rotated);
        _refreshTokenRepository.GetByHashAsync("successor-hash", Arg.Any<CancellationToken>()).Returns(successor);
        _graceStore.GetAsync("successor-hash", Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenResponseDto("access", "successor-plain", "Bearer", 3600, DateTime.UtcNow.AddHours(1)));

        var result = await CreateHandler(gracePeriodSeconds: 30).Handle(new RefreshTokenCommand("rotated-token", ipAddress, userAgent), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
        await _refreshTokenRepository.Received(1).RevokeUserTokensAsync(user.Id, "refresh_token_reuse_detected", Arg.Any<CancellationToken>());
        await _graceStore.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _securityEventLogger.Received(1).LogSecurityEvent(
            SecurityEventType.RefreshTokenReuseDetected, Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Fact(DisplayName = "Ventana de gracia desde el mismo cliente de la rotación: devuelve el mismo par")]
    public async Task Handle_WithinGracePeriodFromSameClient_ReturnsSamePair()
    {
        var user = BuildActiveUser();
        var rotated = BuildRecentlyRotatedToken("rotated-token", user, "successor-hash", TimeSpan.FromSeconds(5));
        var successor = BuildActiveToken("successor-hash", user);
        successor.IpAddress = "10.0.0.1";
        successor.UserAgent = "Navegador/1.0";
        var cachedPair = new RefreshTokenResponseDto("access", "successor-plain", "Bearer", 3600, DateTime.UtcNow.AddHours(1));
        _refreshTokenRepository.GetByTokenAsync("rotated-token", Arg.Any<CancellationToken>()).Returns(rotated);
        _refreshTokenRepository.GetByHashAsync("successor-hash", Arg.Any<CancellationToken>()).Returns(successor);
        _graceStore.GetAsync("successor-hash", Arg.Any<CancellationToken>()).Returns(cachedPair);

        var result = await CreateHandler(gracePeriodSeconds: 30).Handle(new RefreshTokenCommand("rotated-token", "10.0.0.1", "Navegador/1.0"), CancellationToken.None);

        result.Data.Should().Be(cachedPair);
        await _refreshTokenRepository.DidNotReceive().RevokeUserTokensAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Ventana de gracia sin par guardado: 401 sin revocar la sesión")]
    public async Task Handle_WithinGracePeriodWithoutCachedPair_ReturnsUnauthorizedWithoutRevoking()
    {
        var user = BuildActiveUser();
        var rotated = BuildRecentlyRotatedToken("rotated-token", user, "successor-hash", TimeSpan.FromSeconds(5));
        _refreshTokenRepository.GetByTokenAsync("rotated-token", Arg.Any<CancellationToken>()).Returns(rotated);
        _refreshTokenRepository.GetByHashAsync("successor-hash", Arg.Any<CancellationToken>()).Returns(BuildActiveToken("successor-hash", user));
        _graceStore.GetAsync("successor-hash", Arg.Any<CancellationToken>()).Returns((RefreshTokenResponseDto?)null);

        var result = await CreateHandler(gracePeriodSeconds: 30).Handle(new RefreshTokenCommand("rotated-token"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
        await _refreshTokenRepository.DidNotReceive().RevokeUserTokensAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Ventana de gracia: si el token sustituto ya se revocó (logout), no se devuelve el par")]
    public async Task Handle_WithinGracePeriodButSuccessorRevoked_ReturnsUnauthorized()
    {
        var user = BuildActiveUser();
        var rotated = BuildRecentlyRotatedToken("rotated-token", user, "successor-hash", TimeSpan.FromSeconds(5));
        var successor = BuildActiveToken("successor-hash", user);
        successor.IsRevoked = true;
        _refreshTokenRepository.GetByTokenAsync("rotated-token", Arg.Any<CancellationToken>()).Returns(rotated);
        _refreshTokenRepository.GetByHashAsync("successor-hash", Arg.Any<CancellationToken>()).Returns(successor);

        var result = await CreateHandler(gracePeriodSeconds: 30).Handle(new RefreshTokenCommand("rotated-token"), CancellationToken.None);

        result.Success.Should().BeFalse();
        await _graceStore.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Fuera de la ventana de gracia el reuso revoca toda la sesión")]
    public async Task Handle_OutsideGracePeriod_DetectsReuse()
    {
        var user = BuildActiveUser();
        var rotated = BuildRecentlyRotatedToken("rotated-token", user, "successor-hash", TimeSpan.FromMinutes(2));
        _refreshTokenRepository.GetByTokenAsync("rotated-token", Arg.Any<CancellationToken>()).Returns(rotated);

        var result = await CreateHandler(gracePeriodSeconds: 30).Handle(new RefreshTokenCommand("rotated-token"), CancellationToken.None);

        result.Success.Should().BeFalse();
        await _refreshTokenRepository.Received(1).RevokeUserTokensAsync(user.Id, "refresh_token_reuse_detected", Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Con ventana de gracia, la rotación guarda el par identificado por el token nuevo")]
    public async Task Handle_RotationWithGracePeriod_StoresPairByNewTokenHash()
    {
        var user = BuildActiveUser();
        var storedToken = BuildActiveToken("valid-token", user);
        var newRefreshToken = BuildActiveToken("new-hash", user);
        newRefreshToken.PlaintextToken = "new-plain";
        _refreshTokenRepository.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(storedToken);
        _refreshTokenService.ValidateRefreshToken(storedToken).Returns(true);
        _refreshTokenService.GenerateRefreshToken(user, Arg.Any<string>(), Arg.Any<string>()).Returns(newRefreshToken);

        var result = await CreateHandler(gracePeriodSeconds: 30).Handle(new RefreshTokenCommand("valid-token"), CancellationToken.None);

        result.Success.Should().BeTrue();
        await _graceStore.Received(1).StoreAsync(
            "new-hash",
            Arg.Is<RefreshTokenResponseDto>(r => r.RefreshToken == "new-plain"),
            TimeSpan.FromSeconds(30),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Con ventana de gracia, la petición que pierde la carrera recibe el par de la que ganó")]
    public async Task Handle_ConcurrentRotationWithGracePeriod_ReturnsWinnerPair()
    {
        var user = BuildActiveUser();
        var storedToken = BuildActiveToken("valid-token", user);
        var winnerPair = new RefreshTokenResponseDto("access", "winner-plain", "Bearer", 3600, DateTime.UtcNow.AddHours(1));
        _refreshTokenRepository.GetByTokenAsync("valid-token", Arg.Any<CancellationToken>()).Returns(storedToken);
        _refreshTokenService.ValidateRefreshToken(storedToken).Returns(true);
        _refreshTokenService.GenerateRefreshToken(user, Arg.Any<string>(), Arg.Any<string>()).Returns(BuildActiveToken("loser-hash", user));
        _refreshTokenRepository.UpdateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new DbUpdateConcurrencyException("conflict"));
        // La recarga trae lo que guardó la petición ganadora.
        _refreshTokenRepository.ReloadAsync(storedToken, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            storedToken.IsRevoked = true;
            storedToken.RevocationReason = "Token rotated";
            storedToken.RevokedAt = TimeHelpers.GetCurrentMexicoTime();
            storedToken.ReplacedByToken = "winner-hash";
            return Task.CompletedTask;
        });
        _refreshTokenRepository.GetByHashAsync("winner-hash", Arg.Any<CancellationToken>()).Returns(BuildActiveToken("winner-hash", user));
        _graceStore.GetAsync("winner-hash", Arg.Any<CancellationToken>()).Returns(winnerPair);

        var result = await CreateHandler(gracePeriodSeconds: 30).Handle(new RefreshTokenCommand("valid-token"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(winnerPair);
        await _refreshTokenRepository.DidNotReceive().RevokeUserTokensAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
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
