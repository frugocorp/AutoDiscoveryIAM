using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using NSubstitute;
using Xunit;
using RefreshTokenEntity = FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Logout;

public class LogoutHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuthLogger _authLogger;
    private readonly IJwtBlacklistService _jwtBlacklistService;
    private readonly LogoutHandler _handler;

    public LogoutHandlerTests()
    {
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        _authLogger = Substitute.For<IAuthLogger>();
        _jwtBlacklistService = Substitute.For<IJwtBlacklistService>();
        _handler = new LogoutHandler(_refreshTokenRepository, _authLogger, _jwtBlacklistService);
    }

    [Fact]
    public async Task Handle_WhenValidRefreshTokenProvided_ShouldRevokeTokenAndReturnSuccess()
    {
        const string userEmail = "user@test.local";
        const string tokenValue = "valid-refresh-token";
        var user = BuildUser(userEmail);
        var token = BuildActiveToken(tokenValue, user);

        _refreshTokenRepository.GetByTokenAsync(tokenValue, Arg.Any<CancellationToken>())
            .Returns(token);

        var command = new LogoutCommand(RefreshToken: tokenValue, UserId: 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TokensRevoked.Should().Be(1);
        token.IsRevoked.Should().BeTrue();
        token.RevocationReason.Should().Be("User logout");
        await _refreshTokenRepository.Received(1).UpdateAsync(token, Arg.Any<CancellationToken>());
        _authLogger.Received(1).LogLogout(userEmail);
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ShouldReturnSuccessWithZeroRevoked()
    {
        const string tokenValue = "nonexistent-token";

        _refreshTokenRepository.GetByTokenAsync(tokenValue, Arg.Any<CancellationToken>())
            .Returns((RefreshTokenEntity?)null);

        var command = new LogoutCommand(RefreshToken: tokenValue, UserId: 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TokensRevoked.Should().Be(0);
        await _refreshTokenRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenTokenAlreadyRevoked_ShouldReturnSuccessWithZeroRevoked()
    {
        const string tokenValue = "already-revoked-token";
        var user = BuildUser("user@test.local");
        var token = BuildActiveToken(tokenValue, user);
        token.IsRevoked = true;

        _refreshTokenRepository.GetByTokenAsync(tokenValue, Arg.Any<CancellationToken>())
            .Returns(token);

        var command = new LogoutCommand(RefreshToken: tokenValue, UserId: 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.TokensRevoked.Should().Be(0);
        await _refreshTokenRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenRevokeAllTokensWithUserId_ShouldRevokeAllAndReturnSuccess()
    {
        const int userId = 42;
        var command = new LogoutCommand(UserId: userId, RevokeAllTokens: true);

        _refreshTokenRepository.RevokeUserTokensAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        await _refreshTokenRepository.Received(1).RevokeUserTokensAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAccessTokenJtiAndExpiryProvided_ShouldBlacklistAccessToken()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(10);
        var command = new LogoutCommand(AccessTokenJti: "jti-123", AccessTokenExpiresAtUtc: expiresAt);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        await _jwtBlacklistService.Received(1).BlacklistAsync("jti-123", expiresAt, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoTokenAndNoUserId_ShouldReturnSuccessWithMessage()
    {
        var command = new LogoutCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TokensRevoked.Should().Be(0);
        result.Data.Message.Should().Be("Logout completado.");
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_ShouldBubbleExceptionToMiddleware()
    {
        const string tokenValue = "failing-refresh-token";
        var user = BuildUser("user@test.local");
        var token = BuildActiveToken(tokenValue, user);

        _refreshTokenRepository.GetByTokenAsync(tokenValue, Arg.Any<CancellationToken>())
            .Returns(token);
        _refreshTokenRepository.UpdateAsync(token, Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("db failure"));

        var command = new LogoutCommand(RefreshToken: tokenValue, UserId: 1);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact(DisplayName = "Un refresh token de otro usuario no se revoca, ni con RevokeAllTokens")]
    public async Task Handle_WhenRefreshTokenBelongsToAnotherUser_DoesNotRevokeAnything()
    {
        const string tokenValue = "someone-elses-token";
        var owner = BuildUser("owner@test.local");
        var token = BuildActiveToken(tokenValue, owner);

        _refreshTokenRepository.GetByTokenAsync(tokenValue, Arg.Any<CancellationToken>())
            .Returns(token);

        var command = new LogoutCommand(RefreshToken: tokenValue, UserId: owner.Id + 1, RevokeAllTokens: true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.TokensRevoked.Should().Be(0);
        token.IsRevoked.Should().BeFalse();
        await _refreshTokenRepository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
        await _refreshTokenRepository.DidNotReceiveWithAnyArgs().RevokeUserTokensAsync(default, default!, default);
    }

    private static User BuildUser(string email) => new()
    {
        Id = 1,
        PublicId = Guid.NewGuid(),
        Username = "testuser",
        Email = email,
        PasswordHash = "hash",
        Status = "active",
        IsActive = true,
        IsDeleted = false,
    };

    private static RefreshTokenEntity BuildActiveToken(string tokenValue, User user) => new()
    {
        Id = 1,
        Token = tokenValue,
        UserId = user.Id,
        User = user,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        IsRevoked = false,
    };
}
