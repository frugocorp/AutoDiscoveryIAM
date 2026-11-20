using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.TwoFactor.VerifyTwoFactor;

public class VerifyTwoFactorHandlerTests : IDisposable
{
  private readonly AutoDiscoveryIAMDbContext _dbContext;
  private readonly ITotpService _totpService = Substitute.For<ITotpService>();
  private readonly IRefreshTokenService _refreshTokenService = Substitute.For<IRefreshTokenService>();
  private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
  private readonly IAuthLogger _authLogger = Substitute.For<IAuthLogger>();
  private readonly IJwtClaimsFactory _jwtClaimsFactory;
  private readonly LockoutSettings _lockoutSettings = new() { MaxFailedAttempts = 3, LockoutMinutes = 30 };
  private readonly VerifyTwoFactorHandler _handler;

  public VerifyTwoFactorHandlerTests()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    _dbContext = new AutoDiscoveryIAMDbContext(options);

    var jwtSettings = Substitute.For<IOptions<JwtSettings>>();
    jwtSettings.Value.Returns(new JwtSettings
    {
      Key = "ThisIsAVeryLongSecretKeyForTestingPurposesWithAtLeast256BitsAbc",
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpiryMinutes = 60,
    });
    _jwtClaimsFactory = new JwtClaimsFactory(jwtSettings);

    _refreshTokenService.GenerateRefreshToken(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<string>())
      .Returns(callInfo => new FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken
      {
        Token = "hash",
        PlaintextToken = "plain-refresh-token",
        UserId = callInfo.Arg<User>().Id,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
      });

    var lockoutOptions = Substitute.For<IOptions<LockoutSettings>>();
    lockoutOptions.Value.Returns(_lockoutSettings);

    _handler = new VerifyTwoFactorHandler(_dbContext, _totpService, _jwtClaimsFactory, _refreshTokenService, _refreshTokenRepository, _unitOfWork, _authLogger, Substitute.For<ISecurityEventLogger>(), lockoutOptions);
  }

  private async Task<User> SeedUserAsync(string method, string? code = null, DateTime? codeExpiration = null, string? secret = null)
  {
    var user = new User
    {
      PublicId = Guid.NewGuid(),
      Username = $"user-{Guid.NewGuid():N}",
      Email = "2fa-user@example.com",
      PasswordHash = "hash",
      IsActive = true,
      TwoFactorEnabled = true,
      TwoFactorMethod = method,
      TwoFactorCode = code,
      TwoFactorCodeExpiration = codeExpiration,
      TwoFactorSecret = secret,
      TwoFactorChallengeToken = OneTimeTokenHasher.Hash("valid-challenge-token"),
      TwoFactorChallengeTokenExpiration = TimeHelpers.GetCurrentMexicoTime().AddMinutes(10),
      UserRoles = new List<UserRole>(),
    };

    _dbContext.Users.Add(user);
    await _dbContext.SaveChangesAsync(CancellationToken.None);
    return user;
  }

  [Fact(DisplayName = "Handle retorna 409 cuando el token de desafio no existe o expiro")]
  public async Task Handle_UnknownChallengeToken_ReturnsConflict()
  {
    var command = new VerifyTwoFactorCommand("unknown-token", "123456");

    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
  }

  [Fact(DisplayName = "Handle retorna 409 cuando el token de desafio ya expiro")]
  public async Task Handle_ExpiredChallengeToken_ReturnsConflict()
  {
    var user = await SeedUserAsync(TwoFactorMethodCatalog.Email, code: "123456", codeExpiration: TimeHelpers.GetCurrentMexicoTime().AddMinutes(5));
    user.TwoFactorChallengeTokenExpiration = TimeHelpers.GetCurrentMexicoTime().AddMinutes(-1);
    await _dbContext.SaveChangesAsync(CancellationToken.None);

    var command = new VerifyTwoFactorCommand("valid-challenge-token", "123456");

    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
  }

  [Fact(DisplayName = "Handle (email) retorna 401 cuando el codigo no coincide")]
  public async Task Handle_EmailMethod_WrongCode_ReturnsUnauthorized()
  {
    await SeedUserAsync(TwoFactorMethodCatalog.Email, code: "123456", codeExpiration: TimeHelpers.GetCurrentMexicoTime().AddMinutes(5));

    var command = new VerifyTwoFactorCommand("valid-challenge-token", "999999");

    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
  }

  [Fact(DisplayName = "Handle (email) emite tokens y limpia el desafio cuando el codigo es correcto")]
  public async Task Handle_EmailMethod_CorrectCode_ReturnsTokensAndClearsChallenge()
  {
    var user = await SeedUserAsync(TwoFactorMethodCatalog.Email, code: "123456", codeExpiration: TimeHelpers.GetCurrentMexicoTime().AddMinutes(5));

    var command = new VerifyTwoFactorCommand("valid-challenge-token", "123456");

    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Token.Should().NotBeNullOrWhiteSpace();
    result.Data.RefreshToken.Should().Be("plain-refresh-token");
    result.Data.RequiresTwoFactor.Should().BeFalse();

    var reloaded = await _dbContext.Users.FirstAsync(u => u.PublicId == user.PublicId, CancellationToken.None);
    reloaded.TwoFactorChallengeToken.Should().BeNull();
    reloaded.TwoFactorCode.Should().BeNull();
  }

  [Fact(DisplayName = "Handle (Authenticator) usa ITotpService para validar el codigo")]
  public async Task Handle_AuthenticatorMethod_UsesTotpService()
  {
    await SeedUserAsync(TwoFactorMethodCatalog.Authenticator, secret: "JBSWY3DPEHPK3PXP");
    _totpService.ValidateCode("JBSWY3DPEHPK3PXP", "654321", null).Returns(58_000_000L);

    var command = new VerifyTwoFactorCommand("valid-challenge-token", "654321");

    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeTrue();
    _totpService.Received(1).ValidateCode("JBSWY3DPEHPK3PXP", "654321", null);
  }

  [Fact(DisplayName = "Handle (Authenticator) guarda el paso de tiempo del codigo aceptado y lo pasa en la siguiente verificacion")]
  public async Task Handle_AuthenticatorMethod_PersistsLastUsedTimeStep()
  {
    var user = await SeedUserAsync(TwoFactorMethodCatalog.Authenticator, secret: "JBSWY3DPEHPK3PXP");
    user.TwoFactorLastUsedTimeStep = 57_999_999L;
    await _dbContext.SaveChangesAsync(CancellationToken.None);
    _totpService.ValidateCode("JBSWY3DPEHPK3PXP", "654321", 57_999_999L).Returns(58_000_000L);

    var result = await _handler.Handle(new VerifyTwoFactorCommand("valid-challenge-token", "654321"), CancellationToken.None);

    result.Success.Should().BeTrue();
    var reloaded = await _dbContext.Users.FirstAsync(u => u.PublicId == user.PublicId, CancellationToken.None);
    reloaded.TwoFactorLastUsedTimeStep.Should().Be(58_000_000L);
  }

  [Fact(DisplayName = "Handle (Authenticator) rechaza el codigo cuando ITotpService no lo acepta (ej. ya usado)")]
  public async Task Handle_AuthenticatorCodeRejected_ReturnsUnauthorized()
  {
    await SeedUserAsync(TwoFactorMethodCatalog.Authenticator, secret: "JBSWY3DPEHPK3PXP");
    _totpService.ValidateCode("JBSWY3DPEHPK3PXP", "654321", Arg.Any<long?>()).Returns((long?)null);

    var result = await _handler.Handle(new VerifyTwoFactorCommand("valid-challenge-token", "654321"), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
  }

  [Fact(DisplayName = "Handle bloquea la cuenta tras alcanzar el maximo de intentos fallidos")]
  public async Task Handle_WrongCodeReachingMaxAttempts_LocksAccount()
  {
    var user = await SeedUserAsync(TwoFactorMethodCatalog.Email, code: "123456", codeExpiration: TimeHelpers.GetCurrentMexicoTime().AddMinutes(5));
    var command = new VerifyTwoFactorCommand("valid-challenge-token", "000000");

    for (var i = 0; i < _lockoutSettings.MaxFailedAttempts; i++)
    {
      await _handler.Handle(command, CancellationToken.None);
    }

    var reloaded = await _dbContext.Users.FirstAsync(u => u.PublicId == user.PublicId, CancellationToken.None);
    reloaded.LockoutEnd.Should().NotBeNull();
    reloaded.LockoutEnd!.Value.Should().BeAfter(TimeHelpers.GetCurrentMexicoTime());
    reloaded.FailedLoginAttempts.Should().Be(0);
  }

  [Fact(DisplayName = "Handle rechaza incluso el codigo correcto mientras la cuenta esta bloqueada")]
  public async Task Handle_CorrectCodeWhileLockedOut_ReturnsAccountLockedOut()
  {
    var user = await SeedUserAsync(TwoFactorMethodCatalog.Email, code: "123456", codeExpiration: TimeHelpers.GetCurrentMexicoTime().AddMinutes(5));
    user.LockoutEnd = TimeHelpers.GetCurrentMexicoTime().AddMinutes(30);
    await _dbContext.SaveChangesAsync(CancellationToken.None);

    var command = new VerifyTwoFactorCommand("valid-challenge-token", "123456");

    var result = await _handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.AccountLockedOut);
  }

  [Fact(DisplayName = "Handle resetea los intentos fallidos cuando el codigo correcto llega antes del bloqueo")]
  public async Task Handle_CorrectCodeAfterSomeFailures_ResetsFailedAttempts()
  {
    var user = await SeedUserAsync(TwoFactorMethodCatalog.Email, code: "123456", codeExpiration: TimeHelpers.GetCurrentMexicoTime().AddMinutes(5));
    var wrongCommand = new VerifyTwoFactorCommand("valid-challenge-token", "000000");
    await _handler.Handle(wrongCommand, CancellationToken.None);

    var correctCommand = new VerifyTwoFactorCommand("valid-challenge-token", "123456");
    var result = await _handler.Handle(correctCommand, CancellationToken.None);

    result.Success.Should().BeTrue();
    var reloaded = await _dbContext.Users.FirstAsync(u => u.PublicId == user.PublicId, CancellationToken.None);
    reloaded.FailedLoginAttempts.Should().Be(0);
    reloaded.LockoutEnd.Should().BeNull();
  }

  public void Dispose()
  {
    _dbContext.Dispose();
    GC.SuppressFinalize(this);
  }
}
