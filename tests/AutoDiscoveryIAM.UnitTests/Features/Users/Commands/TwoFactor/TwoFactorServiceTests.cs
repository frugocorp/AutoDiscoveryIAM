using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.TwoFactor;

public class TwoFactorServiceTests
{
  private readonly ITwoFactorRepository _repository = Substitute.For<ITwoFactorRepository>();
  private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
  private readonly ITotpService _totpService = Substitute.For<ITotpService>();
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
  private readonly TwoFactorService _sut;

  public TwoFactorServiceTests()
  {
    _sut = new TwoFactorService(_repository, _passwordHasher, _totpService, _unitOfWork);
  }

  private static User CreateUser(string? passwordHash = "existing-hash")
    => new()
    {
      Id = 1,
      PublicId = Guid.NewGuid(),
      Username = "testuser",
      Email = "test@example.com",
      PasswordHash = passwordHash ?? string.Empty,
      IsActive = true,
    };

  [Fact(DisplayName = "EnableEmailAsync retorna 404 cuando el usuario no existe")]
  public async Task EnableEmailAsync_UserNotFound_ReturnsNotFound()
  {
    _repository.GetByPublicIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

    var result = await _sut.EnableEmailAsync(Guid.NewGuid(), "pass", null, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
  }

  [Fact(DisplayName = "EnableEmailAsync retorna 401 cuando la contrasena actual es incorrecta")]
  public async Task EnableEmailAsync_WrongPassword_ReturnsUnauthorized()
  {
    var user = CreateUser();
    _repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    _passwordHasher.VerifyPassword("wrong", user.PasswordHash).Returns(false);

    var result = await _sut.EnableEmailAsync(user.PublicId, "wrong", null, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
    result.Error.Should().Be(UserValidationTypes.CurrentPasswordInvalid.Message);
  }

  [Fact(DisplayName = "EnableEmailAsync activa 2FA por correo cuando la contrasena es correcta")]
  public async Task EnableEmailAsync_CorrectPassword_EnablesEmailMethod()
  {
    var user = CreateUser();
    _repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    _passwordHasher.VerifyPassword("correct", user.PasswordHash).Returns(true);

    var result = await _sut.EnableEmailAsync(user.PublicId, "correct", null, CancellationToken.None);

    result.Success.Should().BeTrue();
    user.TwoFactorEnabled.Should().BeTrue();
    user.TwoFactorMethod.Should().Be(TwoFactorMethodCatalog.Email);
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact(DisplayName = "EnrollAuthenticatorAsync genera un secreto pendiente sin activar 2FA todavia")]
  public async Task EnrollAuthenticatorAsync_GeneratesPendingSecret()
  {
    var user = CreateUser();
    _repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    _passwordHasher.VerifyPassword("correct", user.PasswordHash).Returns(true);
    _totpService.GenerateSecret().Returns("SECRETBASE32");
    _totpService.BuildProvisioningUri(user.Email, "SECRETBASE32").Returns("otpauth://totp/example");
    _totpService.ProtectSecret("SECRETBASE32").Returns("PROTECTED-SECRET");

    var result = await _sut.EnrollAuthenticatorAsync(user.PublicId, "correct", null, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Secret.Should().Be("SECRETBASE32");
    result.Data.ProvisioningUri.Should().Be("otpauth://totp/example");
    user.TwoFactorPendingSecret.Should().Be("PROTECTED-SECRET", "en la base de datos el secreto queda cifrado");
    user.TwoFactorEnabled.Should().BeFalse();
  }

  [Fact(DisplayName = "ConfirmAuthenticatorAsync retorna 409 cuando no hay enrolamiento pendiente")]
  public async Task ConfirmAuthenticatorAsync_NoPendingSecret_ReturnsConflict()
  {
    var user = CreateUser();
    _repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);

    var result = await _sut.ConfirmAuthenticatorAsync(user.PublicId, "123456", CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
  }

  [Fact(DisplayName = "ConfirmAuthenticatorAsync retorna 401 cuando el codigo es invalido")]
  public async Task ConfirmAuthenticatorAsync_InvalidCode_ReturnsUnauthorized()
  {
    var user = CreateUser();
    user.TwoFactorPendingSecret = "SECRETBASE32";
    _repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    _totpService.ValidateCode("SECRETBASE32", "000000", null).Returns((long?)null);

    var result = await _sut.ConfirmAuthenticatorAsync(user.PublicId, "000000", CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
  }

  [Fact(DisplayName = "ConfirmAuthenticatorAsync activa Authenticator cuando el codigo es valido")]
  public async Task ConfirmAuthenticatorAsync_ValidCode_ActivatesAuthenticator()
  {
    var user = CreateUser();
    user.TwoFactorPendingSecret = "SECRETBASE32";
    _repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    _totpService.ValidateCode("SECRETBASE32", "123456", null).Returns(58_000_000L);

    var result = await _sut.ConfirmAuthenticatorAsync(user.PublicId, "123456", CancellationToken.None);

    result.Success.Should().BeTrue();
    user.TwoFactorSecret.Should().Be("SECRETBASE32");
    user.TwoFactorPendingSecret.Should().BeNull();
    user.TwoFactorMethod.Should().Be(TwoFactorMethodCatalog.Authenticator);
    user.TwoFactorEnabled.Should().BeTrue();
    user.TwoFactorLastUsedTimeStep.Should().Be(58_000_000L, "el código de confirmación tampoco debe servir para iniciar sesión");
  }

  [Fact(DisplayName = "DisableAsync limpia todo el estado de 2FA sin importar el metodo activo")]
  public async Task DisableAsync_ClearsAllTwoFactorState()
  {
    var user = CreateUser();
    user.TwoFactorEnabled = true;
    user.TwoFactorMethod = TwoFactorMethodCatalog.Authenticator;
    user.TwoFactorSecret = "SECRET";
    user.TwoFactorChallengeToken = "token";
    _repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    _passwordHasher.VerifyPassword("correct", user.PasswordHash).Returns(true);

    var result = await _sut.DisableAsync(user.PublicId, "correct", null, CancellationToken.None);

    result.Success.Should().BeTrue();
    user.TwoFactorEnabled.Should().BeFalse();
    user.TwoFactorMethod.Should().Be(TwoFactorMethodCatalog.None);
    user.TwoFactorSecret.Should().BeNull();
    user.TwoFactorChallengeToken.Should().BeNull();
  }

  [Fact(DisplayName = "Cuenta sin contrasena con Authenticator activo: sin codigo valido no se puede desactivar el 2FA")]
  public async Task DisableAsync_PasswordlessAuthenticatorWithoutValidCode_IsRejected()
  {
    var user = CreateUser(passwordHash: null);
    user.TwoFactorEnabled = true;
    user.TwoFactorMethod = TwoFactorMethodCatalog.Authenticator;
    user.TwoFactorSecret = "SECRET";
    _repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    _totpService.ValidateCode("SECRET", Arg.Any<string>(), Arg.Any<long?>()).Returns((long?)null);

    var result = await _sut.DisableAsync(user.PublicId, "cualquier-cosa", "000000", CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
    user.TwoFactorEnabled.Should().BeTrue();
  }

  [Fact(DisplayName = "Cuenta sin contrasena con Authenticator activo: con un codigo valido si se desactiva")]
  public async Task DisableAsync_PasswordlessAuthenticatorWithValidCode_Succeeds()
  {
    var user = CreateUser(passwordHash: null);
    user.TwoFactorEnabled = true;
    user.TwoFactorMethod = TwoFactorMethodCatalog.Authenticator;
    user.TwoFactorSecret = "SECRET";
    _repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    _totpService.ValidateCode("SECRET", "123456", Arg.Any<long?>()).Returns(42L);

    var result = await _sut.DisableAsync(user.PublicId, "cualquier-cosa", "123456", CancellationToken.None);

    result.Success.Should().BeTrue();
    user.TwoFactorEnabled.Should().BeFalse();
  }
}
