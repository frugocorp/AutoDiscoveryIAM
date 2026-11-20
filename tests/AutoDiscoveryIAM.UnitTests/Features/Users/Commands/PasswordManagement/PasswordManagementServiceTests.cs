using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using Xunit;
using Microsoft.Extensions.Options;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.PasswordManagement;

public class PasswordManagementServiceTests
{
  private readonly IPasswordManagementRepository _repository = Substitute.For<IPasswordManagementRepository>();
  private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
  private readonly IPasswordResetTokenService _tokenService = Substitute.For<IPasswordResetTokenService>();
  private readonly IPasswordResetEmailService _emailService = Substitute.For<IPasswordResetEmailService>();
  private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
  private readonly IUserLogger _logger = Substitute.For<IUserLogger>();
  private readonly PasswordResetSettings _settings = new();

  private PasswordManagementService CreateSut()
  {
    var options = Substitute.For<IOptions<PasswordResetSettings>>();
    options.Value.Returns(_settings);

    return new PasswordManagementService(
      _repository,
      _passwordHasher,
      _tokenService,
      _emailService,
      _refreshTokenRepository,
      _unitOfWork,
      options,
      _logger);
  }

  private static User CreateUser(string? passwordHash = "existing-hash", bool isActive = true, bool isDeleted = false)
    => new()
    {
      Id = 1,
      PublicId = Guid.NewGuid(),
      Username = "testuser",
      Email = "test@example.com",
      PasswordHash = passwordHash ?? string.Empty,
      IsActive = isActive,
      IsDeleted = isDeleted,
    };

  [Fact(DisplayName = "ChangePasswordAsync retorna 404 cuando el usuario no existe")]
  public async Task ChangePasswordAsync_UserNotFound_ReturnsNotFound()
  {
    _repository.GetByPublicIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);
    var sut = CreateSut();

    var result = await sut.ChangePasswordAsync(Guid.NewGuid(), "current", "NewPass123*", CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().BeEquivalentTo(ErrorInfo.Status404NotFound);
  }

  [Fact(DisplayName = "ChangePasswordAsync retorna 401 cuando currentPassword es incorrecta")]
  public async Task ChangePasswordAsync_WrongCurrentPassword_ReturnsUnauthorized()
  {
    var user = CreateUser();
    _repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    _passwordHasher.VerifyPassword("wrong", user.PasswordHash).Returns(false);
    var sut = CreateSut();

    var result = await sut.ChangePasswordAsync(user.PublicId, "wrong", "NewPass123*", CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().BeEquivalentTo(ErrorInfo.Status401Unauthorized);
    result.Error.Should().Be(UserValidationTypes.CurrentPasswordInvalid.Message);
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact(DisplayName = "ChangePasswordAsync retorna 401 cuando currentPassword esta vacia y el usuario ya tiene hash")]
  public async Task ChangePasswordAsync_MissingCurrentPassword_WhenHashExists_ReturnsUnauthorized()
  {
    var user = CreateUser();
    _repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    var sut = CreateSut();

    var result = await sut.ChangePasswordAsync(user.PublicId, null, "NewPass123*", CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().BeEquivalentTo(ErrorInfo.Status401Unauthorized);
  }

  [Fact(DisplayName = "ChangePasswordAsync permite establecer contrasena sin currentPassword cuando el usuario social no tiene hash")]
  public async Task ChangePasswordAsync_SocialUserWithoutHash_AllowsDirectSet()
  {
    var user = CreateUser(passwordHash: string.Empty);
    _repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    _passwordHasher.HashPassword("NewPass123*").Returns("hashed-new-password");
    var sut = CreateSut();

    var result = await sut.ChangePasswordAsync(user.PublicId, null, "NewPass123*", CancellationToken.None);

    result.Success.Should().BeTrue();
    user.PasswordHash.Should().Be("hashed-new-password");
    await _refreshTokenRepository.Received(1).RevokeUserTokensAsync(user.Id, "password_changed", Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact(DisplayName = "ChangePasswordAsync actualiza el hash y revoca refresh tokens cuando la contrasena actual es correcta")]
  public async Task ChangePasswordAsync_ValidCurrentPassword_UpdatesHashAndRevokesTokens()
  {
    var user = CreateUser();
    _repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    _passwordHasher.VerifyPassword("current-pass", user.PasswordHash).Returns(true);
    _passwordHasher.HashPassword("NewPass123*").Returns("hashed-new-password");
    var sut = CreateSut();

    var result = await sut.ChangePasswordAsync(user.PublicId, "current-pass", "NewPass123*", CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    user.PasswordHash.Should().Be("hashed-new-password");
    await _refreshTokenRepository.Received(1).RevokeUserTokensAsync(user.Id, "password_changed", Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    _logger.Received(1).LogPasswordChanged(user.Id, true);
  }

  [Fact(DisplayName = "RequestResetAsync retorna respuesta neutra cuando el email esta vacio")]
  public async Task RequestResetAsync_EmptyEmail_ReturnsNeutralResponse()
  {
    var sut = CreateSut();

    var result = await sut.RequestResetAsync(string.Empty, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact(DisplayName = "RequestResetAsync retorna la misma respuesta neutra cuando el correo no existe")]
  public async Task RequestResetAsync_UnknownEmail_ReturnsSameNeutralResponse()
  {
    _repository.GetByEmailAsync("unknown@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);
    var sut = CreateSut();

    var result = await sut.RequestResetAsync("unknown@example.com", CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Message.Should().Be("Si el correo existe, se enviara un enlace para restablecer la contraseña.");
  }

  [Trait("Category", "Security")]
  [Fact(DisplayName = "RequestResetAsync aplica un retardo artificial cuando el correo no existe, para no delatar por tiempo de respuesta")]
  public async Task RequestResetAsync_UnknownEmail_AppliesArtificialDelay()
  {
    _repository.GetByEmailAsync("unknown@example.com", Arg.Any<CancellationToken>()).Returns((User?)null);
    var sut = CreateSut();
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    await sut.RequestResetAsync("unknown@example.com", CancellationToken.None);

    stopwatch.Stop();
    stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(200, "sin el retardo artificial, un email inexistente respondería casi instantáneo frente al envío SMTP real de un email existente, delatando por tiempo si la cuenta existe");
  }

  [Fact(DisplayName = "RequestResetAsync retorna respuesta neutra cuando el usuario esta inactivo")]
  public async Task RequestResetAsync_InactiveUser_ReturnsNeutralResponse()
  {
    var user = CreateUser(isActive: false);
    _repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
    var sut = CreateSut();

    var result = await sut.RequestResetAsync(user.Email, CancellationToken.None);

    result.Success.Should().BeTrue();
    await _emailService.DidNotReceive().SendPasswordResetEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
  }

  [Fact(DisplayName = "RequestResetAsync genera token, envia correo y persiste cuando el usuario es valido")]
  public async Task RequestResetAsync_ValidActiveUser_GeneratesTokenAndSendsEmail()
  {
    var user = CreateUser();
    _repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
    _tokenService.GenerateToken().Returns("generated-reset-token");
    _emailService.SendPasswordResetEmailAsync(Arg.Any<User>(), "generated-reset-token", Arg.Any<CancellationToken>(), Arg.Any<bool>())
      .Returns(Task.FromResult(OperationResult.Ok(true)));
    var sut = CreateSut();

    var result = await sut.RequestResetAsync(user.Email, CancellationToken.None);

    result.Success.Should().BeTrue();
    user.PasswordResetToken.Should().Be(OneTimeTokenHasher.Hash("generated-reset-token"), "en la base de datos solo queda el hash");
    user.PasswordResetTokenExpiration.Should().NotBeNull();
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact(DisplayName = "RequestResetAsync no persiste cambios cuando el envio de correo falla")]
  public async Task RequestResetAsync_EmailSendFails_DoesNotPersistChanges()
  {
    var user = CreateUser();
    _repository.GetByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
    _tokenService.GenerateToken().Returns("generated-reset-token");
    _emailService.SendPasswordResetEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
      .Returns(Task.FromResult(OperationResult.Fail<bool>(ErrorInfo.Status424Dependency, "smtp error")));
    var sut = CreateSut();

    var result = await sut.RequestResetAsync(user.Email, CancellationToken.None);

    result.Success.Should().BeTrue();
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact(DisplayName = "ConfirmResetAsync retorna error 400 cuando el token esta vacio")]
  public async Task ConfirmResetAsync_EmptyToken_ReturnsValidationError()
  {
    var sut = CreateSut();

    var result = await sut.ConfirmResetAsync(string.Empty, "NewPass123*", CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().BeEquivalentTo(ErrorInfo.Status400Validation);
  }

  [Fact(DisplayName = "ConfirmResetAsync retorna error 409 cuando el token no existe")]
  public async Task ConfirmResetAsync_TokenNotFound_ReturnsConflict()
  {
    _repository.GetByResetTokenHashAsync(OneTimeTokenHasher.Hash("missing-token"), Arg.Any<CancellationToken>()).Returns((User?)null);
    var sut = CreateSut();

    var result = await sut.ConfirmResetAsync("missing-token", "NewPass123*", CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().BeEquivalentTo(ErrorInfo.Status409Conflict);
  }

  [Fact(DisplayName = "ConfirmResetAsync retorna error 409 cuando el token esta expirado")]
  public async Task ConfirmResetAsync_ExpiredToken_ReturnsConflict()
  {
    var user = CreateUser();
    user.PasswordResetToken = OneTimeTokenHasher.Hash("valid-token");
    user.PasswordResetTokenExpiration = TimeHelpers.GetCurrentMexicoTime().AddMinutes(-5);
    _repository.GetByResetTokenHashAsync(OneTimeTokenHasher.Hash("valid-token"), Arg.Any<CancellationToken>()).Returns(user);
    var sut = CreateSut();

    var result = await sut.ConfirmResetAsync("valid-token", "NewPass123*", CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().BeEquivalentTo(ErrorInfo.Status409Conflict);
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact(DisplayName = "ConfirmResetAsync actualiza contrasena, limpia el token y revoca refresh tokens cuando el token es valido")]
  public async Task ConfirmResetAsync_ValidToken_UpdatesPasswordAndClearsToken()
  {
    var user = CreateUser();
    user.PasswordResetToken = OneTimeTokenHasher.Hash("valid-token");
    user.PasswordResetTokenExpiration = TimeHelpers.GetCurrentMexicoTime().AddMinutes(10);
    _repository.GetByResetTokenHashAsync(OneTimeTokenHasher.Hash("valid-token"), Arg.Any<CancellationToken>()).Returns(user);
    _passwordHasher.HashPassword("NewPass123*").Returns("hashed-new-password");
    var sut = CreateSut();

    var result = await sut.ConfirmResetAsync("valid-token", "NewPass123*", CancellationToken.None);

    result.Success.Should().BeTrue();
    user.PasswordHash.Should().Be("hashed-new-password");
    user.PasswordResetToken.Should().BeNull();
    user.PasswordResetTokenExpiration.Should().BeNull();
    await _refreshTokenRepository.Received(1).RevokeUserTokensAsync(user.Id, "password_reset", Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    _logger.Received(1).LogPasswordResetConfirmed(user.Id, true);
  }

  [Fact(DisplayName = "ConfirmResetAsync desbloquea la cuenta (FailedLoginAttempts/LockoutEnd) cuando el token es valido")]
  public async Task ConfirmResetAsync_ValidToken_ResetsLockoutState()
  {
    var user = CreateUser();
    user.PasswordResetToken = OneTimeTokenHasher.Hash("valid-token");
    user.PasswordResetTokenExpiration = TimeHelpers.GetCurrentMexicoTime().AddMinutes(10);
    user.FailedLoginAttempts = 4;
    user.LockoutEnd = TimeHelpers.GetCurrentMexicoTime().AddMinutes(30);
    _repository.GetByResetTokenHashAsync(OneTimeTokenHasher.Hash("valid-token"), Arg.Any<CancellationToken>()).Returns(user);
    _passwordHasher.HashPassword("NewPass123*").Returns("hashed-new-password");
    var sut = CreateSut();

    var result = await sut.ConfirmResetAsync("valid-token", "NewPass123*", CancellationToken.None);

    result.Success.Should().BeTrue();
    user.FailedLoginAttempts.Should().Be(0);
    user.LockoutEnd.Should().BeNull();
  }
}
