using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.DefaultRole;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.Provisioning;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Services.Provisioning;

/// <summary>
/// Pruebas del alta desde el código del consumidor: valida como el endpoint y deja enlazar la entidad
/// propia antes del único guardado.
/// </summary>
public class UserProvisioningServiceTests
{
  private const string Password = "SecPass123*";
  private readonly ICreateUserByAdminRepository _repository = Substitute.For<ICreateUserByAdminRepository>();
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
  private readonly IActivationService _activationService = Substitute.For<IActivationService>();
  private readonly IActivationEmailService _activationEmailService = Substitute.For<IActivationEmailService>();
  private readonly IPasswordResetEmailService _resetEmailService = Substitute.For<IPasswordResetEmailService>();

  public UserProvisioningServiceTests()
  {
    _activationService.AssignActivationToken(Arg.Any<User>()).Returns("activation-token");
    _activationEmailService
      .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
      .Returns(Task.FromResult(OperationResult.Ok(true)));
  }

  private UserProvisioningService CreateService()
  {
    var passwordHasher = Substitute.For<IPasswordHasher>();
    passwordHasher.HashPassword(Arg.Any<string>()).Returns("hashed");
    var handler = new CreateUserByAdminHandler(
      _repository,
      Substitute.For<IUserLogger>(),
      _unitOfWork,
      passwordHasher,
      _activationService,
      _activationEmailService,
      Substitute.For<IDefaultRoleAssignmentService>(),
      Substitute.For<IPasswordResetTokenService>(),
      _resetEmailService,
      Options.Create(new RegistrationSettings()));
    return new UserProvisioningService(new CreateUserByAdminCommandValidator(), handler);
  }

  [Theory(DisplayName = "Un dato que no cumple las reglas responde 400 y no agrega nada")]
  [InlineData("valido", "no-es-correo", Password, null)]
  [InlineData("valido", "valido@example.com", "123", null)]
  [InlineData("valido", "valido@example.com", null, true)]
  public async Task CreateAsync_InvalidData_ReturnsValidationError(string username, string email, string? password, bool? mustChange)
  {
    var result = await CreateService().CreateAsync(new ProvisionUserRequest(username, email, password, true, mustChange), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status400Validation);
    _repository.DidNotReceive().Add(Arg.Any<User>());
  }

  [Fact(DisplayName = "Un alta válida devuelve el usuario creado con las reglas del alta por administrador")]
  public async Task CreateAsync_Valid_ReturnsProvisionedUser()
  {
    var result = await CreateService().CreateAsync(new ProvisionUserRequest("nuevo", "nuevo@example.com", Password, true), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.Email.Should().Be("nuevo@example.com");
    result.Data.MustChangePassword.Should().BeTrue();
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact(DisplayName = "El enlace del consumidor corre antes del correo y del guardado")]
  public async Task CreateAsync_WithBeforeSave_RunsBeforeEmailAndSave()
  {
    var order = new List<string>();
    _activationEmailService
      .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
      .Returns(_ =>
      {
        order.Add("email");
        return Task.FromResult(OperationResult.Ok(true));
      });
    _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ =>
    {
      order.Add("save");
      return Task.FromResult(1);
    });

    var result = await CreateService().CreateAsync(
      new ProvisionUserRequest("enlace", "enlace@example.com", Password, false),
      (user, _) =>
      {
        user.Email.Should().Be("enlace@example.com");
        order.Add("link");
        return Task.CompletedTask;
      },
      CancellationToken.None);

    result.Success.Should().BeTrue();
    string.Join(",", order).Should().Be("link,email,save");
  }

  [Fact(DisplayName = "Si el enlace del consumidor falla: sin correo, sin guardar y el usuario sale del contexto")]
  public async Task CreateAsync_WhenBeforeSaveThrows_DiscardsUser()
  {
    Func<Task> act = () => CreateService().CreateAsync(
      new ProvisionUserRequest("falla", "falla@example.com", Password, false),
      (_, _) => throw new InvalidOperationException("fallo del consumidor"),
      CancellationToken.None);

    (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("fallo del consumidor");
    _repository.Received(1).Discard(Arg.Is<User>(u => u.Email == "falla@example.com"));
    await _activationEmailService.DidNotReceive().SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Theory(DisplayName = "Si el enlace cambia datos que fija la regla de alta, el alta se cancela")]
  [InlineData("IsAdmin")]
  [InlineData("IsSuAdmin")]
  [InlineData("IsEmailVerified")]
  [InlineData("MustChangePassword")]
  [InlineData("Email")]
  public async Task CreateAsync_WhenBeforeSaveChangesProtectedData_Throws(string field)
  {
    Func<Task> act = () => CreateService().CreateAsync(
      new ProvisionUserRequest("escala", "escala@example.com", Password, true),
      (user, _) =>
      {
        switch (field)
        {
          case "IsAdmin": user.IsAdmin = true; break;
          case "IsSuAdmin": user.IsSuAdmin = true; break;
          case "IsEmailVerified": user.IsEmailVerified = true; break;
          case "MustChangePassword": user.MustChangePassword = false; break;
          default: user.Email = "otro@example.com"; break;
        }

        return Task.CompletedTask;
      },
      CancellationToken.None);

    await act.Should().ThrowAsync<InvalidOperationException>();
    _repository.Received(1).Discard(Arg.Any<User>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact(DisplayName = "Si falla el correo, el usuario sale del contexto")]
  public async Task CreateAsync_WhenEmailFails_DiscardsUser()
  {
    _activationEmailService
      .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>())
      .Returns(Task.FromResult(OperationResult.Fail<bool>(ErrorInfo.Status424Dependency, "smtp caído")));

    var result = await CreateService().CreateAsync(new ProvisionUserRequest("smtp", "smtp@example.com", Password, false), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status424Dependency);
    _repository.Received(1).Discard(Arg.Any<User>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
