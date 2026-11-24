using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.TwoFactorStepUp;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using OtpNet;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Services.TwoFactorStepUp;

/// <summary>
/// Pruebas de la verificación step-up con el Authenticator: código válido, reuso, intentos fallidos
/// y bloqueo.
/// </summary>
public sealed class TwoFactorStepUpServiceTests : IDisposable
{
  private readonly AutoDiscoveryIAMDbContext _dbContext;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
  private readonly IAuthContextService _authContext = Substitute.For<IAuthContextService>();
  private readonly ISecurityEventLogger _securityEventLogger = Substitute.For<ISecurityEventLogger>();
  private readonly TotpService _totpService;
  private readonly TwoFactorStepUpService _service;

  public TwoFactorStepUpServiceTests()
  {
    _dbContext = new AutoDiscoveryIAMDbContext(new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);
    _totpService = new TotpService(Options.Create(new TwoFactorSettings()), new EphemeralDataProtectionProvider(), NullLogger<TotpService>.Instance);
    _service = new TwoFactorStepUpService(
      _dbContext,
      _unitOfWork,
      _totpService,
      _authContext,
      Substitute.For<IAuthLogger>(),
      _securityEventLogger,
      Options.Create(new LockoutSettings { MaxFailedAttempts = 3, LockoutMinutes = 30 }));
  }

  public void Dispose() => _dbContext.Dispose();

  private async Task<(User User, string Secret)> SeedAuthenticatorUserAsync()
  {
    var secret = _totpService.GenerateSecret();
    var user = new User
    {
      PublicId = Guid.NewGuid(),
      Username = $"stepup-{Guid.NewGuid():N}",
      Email = "stepup@example.com",
      PasswordHash = "hash",
      IsActive = true,
      TwoFactorEnabled = true,
      TwoFactorMethod = TwoFactorMethodCatalog.Authenticator,
      TwoFactorSecret = _totpService.ProtectSecret(secret),
    };
    _dbContext.Users.Add(user);
    await _dbContext.SaveChangesAsync(CancellationToken.None);
    return (user, secret);
  }

  private static string CurrentCode(string secret) => new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();

  [Fact(DisplayName = "Un código válido se acepta y queda guardado el paso usado")]
  public async Task Verify_ValidCode_IsVerifiedAndStoresTimeStep()
  {
    var (user, secret) = await SeedAuthenticatorUserAsync();

    var result = await _service.VerifyAsync(user.PublicId, CurrentCode(secret), "203.0.113.5", CancellationToken.None);

    result.IsVerified.Should().BeTrue();
    user.TwoFactorLastUsedTimeStep.Should().NotBeNull();
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact(DisplayName = "El mismo código no sirve dos veces")]
  public async Task Verify_SameCodeTwice_SecondIsRejected()
  {
    var (user, secret) = await SeedAuthenticatorUserAsync();
    var code = CurrentCode(secret);

    (await _service.VerifyAsync(user.PublicId, code, null, CancellationToken.None)).IsVerified.Should().BeTrue();
    var second = await _service.VerifyAsync(user.PublicId, code, null, CancellationToken.None);

    second.Status.Should().Be(TwoFactorStepUpStatus.InvalidCode);
    user.FailedLoginAttempts.Should().Be(1);
  }

  [Fact(DisplayName = "Al llegar al máximo de intentos fallidos la cuenta se bloquea, y un código válido ya no pasa")]
  public async Task Verify_TooManyInvalidCodes_LocksAccount()
  {
    var (user, secret) = await SeedAuthenticatorUserAsync();

    (await _service.VerifyAsync(user.PublicId, "000000", null, CancellationToken.None)).Status.Should().Be(TwoFactorStepUpStatus.InvalidCode);
    (await _service.VerifyAsync(user.PublicId, "000000", null, CancellationToken.None)).Status.Should().Be(TwoFactorStepUpStatus.InvalidCode);
    var third = await _service.VerifyAsync(user.PublicId, "000000", null, CancellationToken.None);

    third.Status.Should().Be(TwoFactorStepUpStatus.LockedOut);
    third.LockoutEnd.Should().BeAfter(TimeHelpers.GetCurrentMexicoTime());
    (await _service.VerifyAsync(user.PublicId, CurrentCode(secret), null, CancellationToken.None)).Status.Should().Be(TwoFactorStepUpStatus.LockedOut);
    _securityEventLogger.Received().LogSecurityEvent(SecurityEventType.AccountLockedOut, user.Email, Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>());
  }

  [Fact(DisplayName = "Sin Authenticator activo responde NotEnrolled y no cuenta intentos")]
  public async Task Verify_WithoutAuthenticator_ReturnsNotEnrolled()
  {
    var (user, _) = await SeedAuthenticatorUserAsync();
    user.TwoFactorMethod = TwoFactorMethodCatalog.Email;
    await _dbContext.SaveChangesAsync(CancellationToken.None);

    var result = await _service.VerifyAsync(user.PublicId, "123456", null, CancellationToken.None);

    result.Status.Should().Be(TwoFactorStepUpStatus.NotEnrolled);
    user.FailedLoginAttempts.Should().Be(0);
  }

  [Fact(DisplayName = "Usuario inexistente o sin sesión responde UserNotFound")]
  public async Task Verify_UnknownOrAnonymousUser_ReturnsUserNotFound()
  {
    (await _service.VerifyAsync(Guid.NewGuid(), "123456", null, CancellationToken.None)).Status.Should().Be(TwoFactorStepUpStatus.UserNotFound);

    _authContext.GetCurrentUserPublicId().Returns((Guid?)null);
    (await _service.VerifyCurrentUserAsync("123456", null, CancellationToken.None)).Status.Should().Be(TwoFactorStepUpStatus.UserNotFound);
  }

  [Fact(DisplayName = "VerifyCurrentUserAsync usa el usuario autenticado de la petición")]
  public async Task VerifyCurrentUser_UsesAuthenticatedUser()
  {
    var (user, secret) = await SeedAuthenticatorUserAsync();
    _authContext.GetCurrentUserPublicId().Returns(user.PublicId);

    (await _service.VerifyCurrentUserAsync(CurrentCode(secret), null, CancellationToken.None)).IsVerified.Should().BeTrue();
  }
}
