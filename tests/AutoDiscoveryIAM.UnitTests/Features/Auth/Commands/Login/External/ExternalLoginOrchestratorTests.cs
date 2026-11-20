using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Contracts;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using RefreshTokenEntity = FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login.External;

public class ExternalLoginOrchestratorTests
{
    private static AutoDiscoveryIAMDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AutoDiscoveryIAMDbContext(options);
    }

    private static (
        ExternalLoginOrchestrator Orchestrator,
        AutoDiscoveryIAMDbContext DbContext,
        IExternalLoginProvider Provider,
        IActivationService ActivationService,
        IActivationEmailService ActivationEmailService) CreateSut(string dbName)
    {
        var dbContext = CreateContext(dbName);
        var provider = Substitute.For<IExternalLoginProvider>();
        provider.ProviderName.Returns(ExternalLoginProviders.Google);
        var resolver = new ExternalLoginProviderResolver([provider]);

        var jwtClaimsFactory = Substitute.For<IJwtClaimsFactory>();
        jwtClaimsFactory.BuildAccessToken(Arg.Any<User>()).Returns(("access-token", DateTime.UtcNow.AddHours(1)));
        var refreshTokenService = Substitute.For<IRefreshTokenService>();
        refreshTokenService.GenerateRefreshToken(Arg.Any<User>(), Arg.Any<string?>(), Arg.Any<string?>())
          .Returns(_ => new RefreshTokenEntity { PlaintextToken = "refresh-token" });
        var refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        refreshTokenRepository.CreateAsync(Arg.Any<RefreshTokenEntity>(), Arg.Any<CancellationToken>())
          .Returns(callInfo => Task.FromResult(callInfo.Arg<RefreshTokenEntity>()));
        var sessionService = new ExternalLoginSessionService(jwtClaimsFactory, refreshTokenService, refreshTokenRepository);

        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.HashPassword(Arg.Any<string>()).Returns("hashed-random-password");
        var authLogger = Substitute.For<IAuthLogger>();
        var activationService = Substitute.For<IActivationService>();
        activationService.AssignActivationToken(Arg.Any<User>()).Returns("activation-token");
        var activationEmailService = Substitute.For<IActivationEmailService>();
        activationEmailService
          .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
          .Returns(OperationResult.Ok(true));

        var twoFactorCodeGenerator = Substitute.For<ITwoFactorCodeGenerator>();
        twoFactorCodeGenerator.GenerateChallengeToken().Returns("challenge-token");
        twoFactorCodeGenerator.GenerateNumericCode(Arg.Any<int>()).Returns("123456");

        var orchestrator = new ExternalLoginOrchestrator(
          resolver,
          sessionService,
          dbContext,
          passwordHasher,
          authLogger,
          activationService,
          activationEmailService,
          twoFactorCodeGenerator,
          Substitute.For<ITwoFactorEmailService>(),
          Options.Create(new TwoFactorSettings()));

        return (orchestrator, dbContext, provider, activationService, activationEmailService);
    }

    private static ExternalIdentityProfile Profile(string providerId = "google-sub-1", string email = "social@example.com")
      => new(ExternalLoginProviders.Google, providerId, email, IsEmailVerified: true);

    // ───────────────────────────────────────────────────────────────────────
    // HandleCallbackAsync
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleCallbackAsync_WhenProviderExchangeFails_PropagatesFailure()
    {
        var (orchestrator, _, provider, _, _) = CreateSut(nameof(HandleCallbackAsync_WhenProviderExchangeFails_PropagatesFailure));
        provider.ExchangeCodeAsync("code", Arg.Any<CancellationToken>())
          .Returns(OperationResult.Fail<ExternalIdentityProfile>(ErrorInfo.Status424Dependency, "Google no respondio."));

        var result = await orchestrator.HandleCallbackAsync(ExternalLoginProviders.Google, "code", null, null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status424Dependency);
    }

    [Fact]
    public async Task HandleCallbackAsync_WhenUserIsNew_CreatesInactiveUserAndReturnsPendingActivation()
    {
        var (orchestrator, dbContext, provider, activationService, activationEmailService) =
          CreateSut(nameof(HandleCallbackAsync_WhenUserIsNew_CreatesInactiveUserAndReturnsPendingActivation));
        var profile = Profile();
        provider.ExchangeCodeAsync("code", Arg.Any<CancellationToken>()).Returns(OperationResult.Ok(profile));

        var result = await orchestrator.HandleCallbackAsync(ExternalLoginProviders.Google, "code", "1.2.3.4", "agent", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.AccountPendingOrInactive);

        var createdUser = await dbContext.Users.SingleAsync(u => u.Email == profile.Email, CancellationToken.None);
        createdUser.IsActive.Should().BeFalse();
        activationService.Received(1).AssignActivationToken(createdUser);
        await activationEmailService.Received(1).SendActivationEmailAsync(createdUser, Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());

        var link = await dbContext.ExternalLogins.SingleAsync(x => x.UserId == createdUser.Id, CancellationToken.None);
        link.ProviderId.Should().Be(ExternalProviderIdHasher.Hash(profile.ProviderId));
    }

    [Fact]
    public async Task HandleCallbackAsync_WhenLinkedUserIsInactive_ReturnsAccountPendingOrInactive()
    {
        var (orchestrator, dbContext, provider, _, _) =
          CreateSut(nameof(HandleCallbackAsync_WhenLinkedUserIsInactive_ReturnsAccountPendingOrInactive));
        var profile = Profile();
        var user = new User { Username = "inactive-social", Email = profile.Email, PasswordHash = "hash", IsActive = false };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        dbContext.ExternalLogins.Add(new ExternalLogin { UserId = user.Id, Provider = profile.Provider, ProviderId = ExternalProviderIdHasher.Hash(profile.ProviderId) });
        await dbContext.SaveChangesAsync(CancellationToken.None);
        provider.ExchangeCodeAsync("code", Arg.Any<CancellationToken>()).Returns(OperationResult.Ok(profile));

        var result = await orchestrator.HandleCallbackAsync(ExternalLoginProviders.Google, "code", null, null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.AccountPendingOrInactive);
    }

    [Fact]
    public async Task HandleCallbackAsync_WhenLinkedUserIsActive_ReturnsSession()
    {
        var (orchestrator, dbContext, provider, _, _) = CreateSut(nameof(HandleCallbackAsync_WhenLinkedUserIsActive_ReturnsSession));
        var profile = Profile();
        var user = new User { Username = "active-social", Email = profile.Email, PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        dbContext.ExternalLogins.Add(new ExternalLogin { UserId = user.Id, Provider = profile.Provider, ProviderId = ExternalProviderIdHasher.Hash(profile.ProviderId) });
        await dbContext.SaveChangesAsync(CancellationToken.None);
        provider.ExchangeCodeAsync("code", Arg.Any<CancellationToken>()).Returns(OperationResult.Ok(profile));

        var result = await orchestrator.HandleCallbackAsync(ExternalLoginProviders.Google, "code", null, null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Token.Should().Be("access-token");
        result.Data.RefreshToken.Should().Be("refresh-token");
    }

    [Fact(DisplayName = "Login con Google de un usuario con 2FA: responde el reto, sin emitir tokens")]
    public async Task HandleCallbackAsync_WhenUserHasTwoFactor_ReturnsChallengeWithoutTokens()
    {
        var (orchestrator, dbContext, provider, _, _) = CreateSut(nameof(HandleCallbackAsync_WhenUserHasTwoFactor_ReturnsChallengeWithoutTokens));
        var profile = Profile();
        var user = new User
        {
            Username = "social-2fa",
            Email = profile.Email,
            PasswordHash = "hash",
            IsActive = true,
            TwoFactorEnabled = true,
            TwoFactorMethod = TwoFactorMethodCatalog.Authenticator,
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        dbContext.ExternalLogins.Add(new ExternalLogin { UserId = user.Id, Provider = profile.Provider, ProviderId = ExternalProviderIdHasher.Hash(profile.ProviderId) });
        await dbContext.SaveChangesAsync(CancellationToken.None);
        provider.ExchangeCodeAsync("code", Arg.Any<CancellationToken>()).Returns(OperationResult.Ok(profile));

        var result = await orchestrator.HandleCallbackAsync(ExternalLoginProviders.Google, "code", null, null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.RequiresTwoFactor.Should().BeTrue();
        result.Data.TwoFactorChallengeToken.Should().Be("challenge-token");
        result.Data.Token.Should().BeNull();
        result.Data.RefreshToken.Should().BeNull();

        var stored = await dbContext.Users.SingleAsync(u => u.Id == user.Id, CancellationToken.None);
        stored.TwoFactorChallengeToken.Should().Be(OneTimeTokenHasher.Hash("challenge-token"), "el reto debe persistirse (como hash) para que /auth/2fa/verify lo encuentre");
    }

    [Fact(DisplayName = "Login con Google de una cuenta bloqueada por intentos fallidos: rechazado")]
    public async Task HandleCallbackAsync_WhenUserIsLockedOut_ReturnsLockedOut()
    {
        var (orchestrator, dbContext, provider, _, _) = CreateSut(nameof(HandleCallbackAsync_WhenUserIsLockedOut_ReturnsLockedOut));
        var profile = Profile();
        var user = new User
        {
            Username = "social-locked",
            Email = profile.Email,
            PasswordHash = "hash",
            IsActive = true,
            LockoutEnd = TimeHelpers.GetCurrentMexicoTime().AddMinutes(30),
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        dbContext.ExternalLogins.Add(new ExternalLogin { UserId = user.Id, Provider = profile.Provider, ProviderId = ExternalProviderIdHasher.Hash(profile.ProviderId) });
        await dbContext.SaveChangesAsync(CancellationToken.None);
        provider.ExchangeCodeAsync("code", Arg.Any<CancellationToken>()).Returns(OperationResult.Ok(profile));

        var result = await orchestrator.HandleCallbackAsync(ExternalLoginProviders.Google, "code", null, null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.AccountLockedOut);
    }

    [Theory(DisplayName = "Login con Google no vincula por email una cuenta con flags especiales")]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task HandleCallbackAsync_WhenEmailMatchesFlaggedAccount_DoesNotAutoLink(bool isAdmin, bool isDevelopment, bool isSuAdmin)
    {
        var (orchestrator, dbContext, provider, _, _) = CreateSut($"{nameof(HandleCallbackAsync_WhenEmailMatchesFlaggedAccount_DoesNotAutoLink)}-{isAdmin}-{isDevelopment}-{isSuAdmin}");
        var profile = Profile();
        var user = new User
        {
            Username = "flagged",
            Email = profile.Email,
            PasswordHash = "hash",
            IsActive = true,
            IsAdmin = isAdmin,
            IsDevelopment = isDevelopment,
            IsSuAdmin = isSuAdmin,
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        provider.ExchangeCodeAsync("code", Arg.Any<CancellationToken>()).Returns(OperationResult.Ok(profile));

        var result = await orchestrator.HandleCallbackAsync(ExternalLoginProviders.Google, "code", null, null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
        (await dbContext.ExternalLogins.AnyAsync(x => x.UserId == user.Id, CancellationToken.None)).Should().BeFalse("no debe quedar ninguna vinculación");
    }

    [Fact(DisplayName = "Login con Google sí vincula por email una cuenta normal")]
    public async Task HandleCallbackAsync_WhenEmailMatchesRegularAccount_AutoLinksAndReturnsSession()
    {
        var (orchestrator, dbContext, provider, _, _) = CreateSut(nameof(HandleCallbackAsync_WhenEmailMatchesRegularAccount_AutoLinksAndReturnsSession));
        var profile = Profile();
        var user = new User { Username = "regular", Email = profile.Email, PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        provider.ExchangeCodeAsync("code", Arg.Any<CancellationToken>()).Returns(OperationResult.Ok(profile));

        var result = await orchestrator.HandleCallbackAsync(ExternalLoginProviders.Google, "code", null, null, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.Token.Should().Be("access-token");
        (await dbContext.ExternalLogins.AnyAsync(x => x.UserId == user.Id, CancellationToken.None)).Should().BeTrue();
    }

    [Theory(DisplayName = "Login con Google invalida la contraseña de una cuenta existente con email sin verificar (pre-registro)")]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task HandleCallbackAsync_WhenLinkingExistingAccount_InvalidatesPasswordOnlyIfEmailUnverified(bool isEmailVerified, bool expectPasswordInvalidated)
    {
        var (orchestrator, dbContext, provider, _, _) = CreateSut($"{nameof(HandleCallbackAsync_WhenLinkingExistingAccount_InvalidatesPasswordOnlyIfEmailUnverified)}-{isEmailVerified}");
        var profile = Profile();
        var user = new User { Username = "prehijack", Email = profile.Email, PasswordHash = "attacker-password-hash", IsActive = true, IsEmailVerified = isEmailVerified };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        provider.ExchangeCodeAsync("code", Arg.Any<CancellationToken>()).Returns(OperationResult.Ok(profile));

        var result = await orchestrator.HandleCallbackAsync(ExternalLoginProviders.Google, "code", null, null, CancellationToken.None);

        result.Success.Should().BeTrue();
        var reloaded = await dbContext.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id, CancellationToken.None);
        if (expectPasswordInvalidated)
        {
            reloaded.PasswordHash.Should().NotBe("attacker-password-hash", "quien registró la cuenta sin verificar el correo no debe conservar el acceso");
        }
        else
        {
            reloaded.PasswordHash.Should().Be("attacker-password-hash");
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    // LinkExternalIdentityAsync
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LinkExternalIdentityAsync_WhenCurrentUserNotFound_ReturnsUnauthorized()
    {
        var (orchestrator, _, _, _, _) = CreateSut(nameof(LinkExternalIdentityAsync_WhenCurrentUserNotFound_ReturnsUnauthorized));

        var result = await orchestrator.LinkExternalIdentityAsync(ExternalLoginProviders.Google, "code", Guid.NewGuid(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
    }

    [Fact]
    public async Task LinkExternalIdentityAsync_WhenIdentityAlreadyLinkedToAnotherUser_ReturnsConflict()
    {
        var (orchestrator, dbContext, provider, _, _) =
          CreateSut(nameof(LinkExternalIdentityAsync_WhenIdentityAlreadyLinkedToAnotherUser_ReturnsConflict));
        var profile = Profile();
        var currentUser = new User { Username = "current", Email = "current@example.com", PasswordHash = "hash", IsActive = true };
        var otherUser = new User { Username = "other", Email = "other@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.AddRange(currentUser, otherUser);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        dbContext.ExternalLogins.Add(new ExternalLogin { UserId = otherUser.Id, Provider = profile.Provider, ProviderId = ExternalProviderIdHasher.Hash(profile.ProviderId) });
        await dbContext.SaveChangesAsync(CancellationToken.None);
        provider.ExchangeCodeAsync("code", Arg.Any<CancellationToken>()).Returns(OperationResult.Ok(profile));

        var result = await orchestrator.LinkExternalIdentityAsync(ExternalLoginProviders.Google, "code", currentUser.PublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
    }

    [Fact]
    public async Task LinkExternalIdentityAsync_WhenIdentityAlreadyLinkedToCurrentUser_ReturnsSuccessWithoutDuplicateLink()
    {
        var (orchestrator, dbContext, provider, _, _) =
          CreateSut(nameof(LinkExternalIdentityAsync_WhenIdentityAlreadyLinkedToCurrentUser_ReturnsSuccessWithoutDuplicateLink));
        var profile = Profile();
        var currentUser = new User { Username = "current", Email = "current@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(currentUser);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        dbContext.ExternalLogins.Add(new ExternalLogin { UserId = currentUser.Id, Provider = profile.Provider, ProviderId = ExternalProviderIdHasher.Hash(profile.ProviderId) });
        await dbContext.SaveChangesAsync(CancellationToken.None);
        provider.ExchangeCodeAsync("code", Arg.Any<CancellationToken>()).Returns(OperationResult.Ok(profile));

        var result = await orchestrator.LinkExternalIdentityAsync(ExternalLoginProviders.Google, "code", currentUser.PublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        var links = await dbContext.ExternalLogins.Where(x => x.UserId == currentUser.Id).ToListAsync(CancellationToken.None);
        links.Should().HaveCount(1);
    }

    [Fact]
    public async Task LinkExternalIdentityAsync_WhenCurrentUserHasDifferentProviderLink_ReturnsConflict()
    {
        var (orchestrator, dbContext, provider, _, _) =
          CreateSut(nameof(LinkExternalIdentityAsync_WhenCurrentUserHasDifferentProviderLink_ReturnsConflict));
        var profile = Profile(providerId: "google-sub-new");
        var currentUser = new User { Username = "current", Email = "current@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(currentUser);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        dbContext.ExternalLogins.Add(new ExternalLogin { UserId = currentUser.Id, Provider = profile.Provider, ProviderId = ExternalProviderIdHasher.Hash("google-sub-old") });
        await dbContext.SaveChangesAsync(CancellationToken.None);
        provider.ExchangeCodeAsync("code", Arg.Any<CancellationToken>()).Returns(OperationResult.Ok(profile));

        var result = await orchestrator.LinkExternalIdentityAsync(ExternalLoginProviders.Google, "code", currentUser.PublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
    }

    [Fact]
    public async Task LinkExternalIdentityAsync_WhenNoConflicts_CreatesNewLink()
    {
        var (orchestrator, dbContext, provider, _, _) = CreateSut(nameof(LinkExternalIdentityAsync_WhenNoConflicts_CreatesNewLink));
        var profile = Profile();
        var currentUser = new User { Username = "current", Email = "current@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(currentUser);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        provider.ExchangeCodeAsync("code", Arg.Any<CancellationToken>()).Returns(OperationResult.Ok(profile));

        var result = await orchestrator.LinkExternalIdentityAsync(ExternalLoginProviders.Google, "code", currentUser.PublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        var link = await dbContext.ExternalLogins.SingleAsync(x => x.UserId == currentUser.Id, CancellationToken.None);
        link.ProviderId.Should().Be(ExternalProviderIdHasher.Hash(profile.ProviderId));
    }

    // ───────────────────────────────────────────────────────────────────────
    // UnlinkExternalIdentityAsync
    // ───────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UnlinkExternalIdentityAsync_WhenCurrentUserNotFound_ReturnsUnauthorized()
    {
        var (orchestrator, _, _, _, _) = CreateSut(nameof(UnlinkExternalIdentityAsync_WhenCurrentUserNotFound_ReturnsUnauthorized));

        var result = await orchestrator.UnlinkExternalIdentityAsync(ExternalLoginProviders.Google, Guid.NewGuid(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
    }

    [Fact]
    public async Task UnlinkExternalIdentityAsync_WhenNoLinkExists_ReturnsNotFound()
    {
        var (orchestrator, dbContext, _, _, _) = CreateSut(nameof(UnlinkExternalIdentityAsync_WhenNoLinkExists_ReturnsNotFound));
        var currentUser = new User { Username = "current", Email = "current@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(currentUser);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await orchestrator.UnlinkExternalIdentityAsync(ExternalLoginProviders.Google, currentUser.PublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    }

    [Fact]
    public async Task UnlinkExternalIdentityAsync_WhenLinkExists_RemovesIt()
    {
        var (orchestrator, dbContext, _, _, _) = CreateSut(nameof(UnlinkExternalIdentityAsync_WhenLinkExists_RemovesIt));
        var currentUser = new User { Username = "current", Email = "current@example.com", PasswordHash = "hash", IsActive = true };
        dbContext.Users.Add(currentUser);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        dbContext.ExternalLogins.Add(new ExternalLogin { UserId = currentUser.Id, Provider = ExternalLoginProviders.Google, ProviderId = ExternalProviderIdHasher.Hash("some-sub") });
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await orchestrator.UnlinkExternalIdentityAsync(ExternalLoginProviders.Google, currentUser.PublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        var remaining = await dbContext.ExternalLogins.IgnoreQueryFilters()
          .Where(x => x.UserId == currentUser.Id && !x.IsDeleted)
          .ToListAsync(CancellationToken.None);
        remaining.Should().BeEmpty();
    }
}
