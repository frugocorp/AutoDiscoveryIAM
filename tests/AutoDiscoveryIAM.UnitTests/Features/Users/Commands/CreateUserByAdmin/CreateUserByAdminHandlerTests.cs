using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.DefaultRole;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.CreateUserByAdmin;

/// <summary>
/// Pruebas del alta de usuarios por un administrador.
/// </summary>
public class CreateUserByAdminHandlerTests
{
  private readonly ICreateUserByAdminRepository _repository = Substitute.For<ICreateUserByAdminRepository>();
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
  private readonly IActivationService _activationService = Substitute.For<IActivationService>();
  private readonly IActivationEmailService _activationEmailService = Substitute.For<IActivationEmailService>();
  private readonly IDefaultRoleAssignmentService _defaultRole = Substitute.For<IDefaultRoleAssignmentService>();
  private readonly IPasswordResetTokenService _resetTokenService = Substitute.For<IPasswordResetTokenService>();
  private readonly IPasswordResetEmailService _resetEmailService = Substitute.For<IPasswordResetEmailService>();

  public CreateUserByAdminHandlerTests()
  {
    _activationService.AssignActivationToken(Arg.Any<User>()).Returns("activation-token");
    _activationEmailService
      .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
      .Returns(Task.FromResult(OperationResult.Ok(true)));
    _resetTokenService.GenerateToken().Returns("setup-token");
    _resetEmailService
      .SendPasswordSetupEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
      .Returns(Task.FromResult(OperationResult.Ok(true)));
  }

  private CreateUserByAdminHandler CreateHandler()
  {
    var passwordHasher = Substitute.For<IPasswordHasher>();
    passwordHasher.HashPassword(Arg.Any<string>()).Returns("hashed");
    return new CreateUserByAdminHandler(_repository, Substitute.For<IUserLogger>(), _unitOfWork, passwordHasher, _activationService, _activationEmailService, _defaultRole, _resetTokenService, _resetEmailService, Options.Create(new RegistrationSettings()));
  }

  [Fact(DisplayName = "Con activate=true la cuenta nace activa, con el rol por defecto y sin correo")]
  public async Task Handle_Activate_CreatesActiveUserWithoutEmail()
  {
    User? added = null;
    _repository.When(r => r.Add(Arg.Any<User>())).Do(c => added = c.Arg<User>());

    var result = await CreateHandler().Handle(new CreateUserByAdminCommand("activo", "activo@example.com", "SecPass123*", true), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Status.Should().Be(UserStatusCatalog.Active);
    result.Data.ActivationEmailSent.Should().BeFalse();
    added!.IsActive.Should().BeTrue();
    added.IsEmailVerified.Should().BeFalse("el administrador no prueba de quién es el correo");
    await _defaultRole.Received(1).AssignDefaultRoleAsync(added, Arg.Any<CancellationToken>());
    await _activationEmailService.DidNotReceive().SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact(DisplayName = "Con activate=false la cuenta nace pendiente y recibe el correo de activación")]
  public async Task Handle_NotActivate_CreatesPendingUserAndSendsActivation()
  {
    var result = await CreateHandler().Handle(new CreateUserByAdminCommand("pendiente", "pendiente@example.com", "SecPass123*", false), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Status.Should().Be(UserStatusCatalog.Pending);
    result.Data.ActivationEmailSent.Should().BeTrue();
    await _activationEmailService.Received(1).SendActivationEmailAsync(
      Arg.Is<User>(u => u.Email == "pendiente@example.com" && !u.IsActive), "activation-token", Arg.Any<CancellationToken>(), false);
  }

  [Fact(DisplayName = "Sin contraseña: cuenta activa sin correo confirmado y un solo correo para definir la contraseña")]
  public async Task Handle_WithoutPassword_SendsSingleSetupEmail()
  {
    User? added = null;
    _repository.When(r => r.Add(Arg.Any<User>())).Do(c => added = c.Arg<User>());

    var result = await CreateHandler().Handle(new CreateUserByAdminCommand("invitado", "invitado@example.com", null, false), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.PasswordSetupEmailSent.Should().BeTrue();
    result.Data.ActivationEmailSent.Should().BeFalse();
    added!.IsActive.Should().BeTrue();
    added.IsEmailVerified.Should().BeFalse();
    added.PasswordResetToken.Should().Be(OneTimeTokenHasher.Hash("setup-token"), "en la base solo queda el hash");
    added.PasswordResetTokenExpiration.Should().BeAfter(TimeHelpers.GetCurrentMexicoTime().AddHours(71));
    await _resetEmailService.Received(1).SendPasswordSetupEmailAsync(added, "setup-token", Arg.Any<CancellationToken>(), false);
    await _activationEmailService.DidNotReceive().SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
  }

  [Theory(DisplayName = "Con contraseña, mustChangePassword es true salvo que se pida false")]
  [InlineData(null, true)]
  [InlineData(true, true)]
  [InlineData(false, false)]
  public async Task Handle_WithPassword_MustChangePasswordDefaultsToTrue(bool? requested, bool expected)
  {
    User? added = null;
    _repository.When(r => r.Add(Arg.Any<User>())).Do(c => added = c.Arg<User>());

    var result = await CreateHandler().Handle(new CreateUserByAdminCommand("temporal", "temporal@example.com", "SecPass123*", true, requested), CancellationToken.None);

    result.Data!.MustChangePassword.Should().Be(expected);
    added!.MustChangePassword.Should().Be(expected);
  }

  [Fact(DisplayName = "En una invitación (sin contraseña) mustChangePassword no aplica")]
  public async Task Handle_Invitation_DoesNotRequirePasswordChange()
  {
    User? added = null;
    _repository.When(r => r.Add(Arg.Any<User>())).Do(c => added = c.Arg<User>());

    var result = await CreateHandler().Handle(new CreateUserByAdminCommand("invita", "invita@example.com", null, false), CancellationToken.None);

    result.Data!.MustChangePassword.Should().BeFalse();
    added!.MustChangePassword.Should().BeFalse();
  }

  [Fact(DisplayName = "Si falla el correo de activación no se guarda nada")]
  public async Task Handle_WhenActivationEmailFails_DoesNotSave()
  {
    _activationEmailService
      .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
      .Returns(Task.FromResult(OperationResult.Fail<bool>(ErrorInfo.Status424Dependency, "smtp")));

    var result = await CreateHandler().Handle(new CreateUserByAdminCommand("smtp", "smtp@example.com", "SecPass123*", false), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().BeEquivalentTo(ErrorInfo.Status424Dependency);
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact(DisplayName = "Un correo ya registrado responde 409 EMAIL_EXISTS, sin respuesta neutra")]
  public async Task Handle_DuplicateEmail_ReturnsConflict()
  {
    _repository.ExistsByEmailAsync("dup@example.com", Arg.Any<CancellationToken>()).Returns(true);

    var result = await CreateHandler().Handle(new CreateUserByAdminCommand("dup", "dup@example.com", "SecPass123*", true), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().BeEquivalentTo(ErrorInfo.Status409Conflict);
    result.Error.Should().Be(UserValidationTypes.EmailExists.Message);
    _repository.DidNotReceive().Add(Arg.Any<User>());
  }

  [Fact(DisplayName = "Un nombre de usuario ya registrado responde 409 USERNAME_EXISTS")]
  public async Task Handle_DuplicateUsername_ReturnsConflict()
  {
    _repository.ExistsByUsernameAsync("dup", Arg.Any<CancellationToken>()).Returns(true);

    var result = await CreateHandler().Handle(new CreateUserByAdminCommand("dup", "nuevo@example.com", "SecPass123*", true), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.Error.Should().Be(UserValidationTypes.UsernameExists.Message);
    _repository.DidNotReceive().Add(Arg.Any<User>());
  }
}
