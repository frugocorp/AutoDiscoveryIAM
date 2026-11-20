#pragma warning disable CS8602
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.CreateUser;

/// <summary>
/// Pruebas unitarias para CreateUserHandler, validando lógica de negocio y manejo de errores como duplicidad de email.
/// </summary>
public class CreateUserHandlerTests
{
  /// <summary>
  /// Verifica que el handler retorna error 409 y mensaje centralizado si el email ya existe.
  /// </summary>
  [Fact(DisplayName = "Handle con email ya registrado responde igual que un registro nuevo, no crea nada y avisa al dueño")]
  public async Task Handle_EmailDuplicado_ReturnsNeutralResponseAndNotifiesOwner()
  {
    var mockRepo = Substitute.For<ICreateUserRepository>();
    var mockUnitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var mockPasswordHasher = Substitute.For<IPasswordHasher>();
    var mockLogger = Substitute.For<IUserLogger>();
    var mockActivationService = Substitute.For<IActivationService>();
    var mockActivationEmailService = Substitute.For<IActivationEmailService>();

    mockRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

    var handler = new CreateUserHandler(mockRepo, mockLogger, mockUnitOfWork, mockPasswordHasher, mockActivationService, mockActivationEmailService);
    var command = new CreateUserCommand("testuser", "test@example.com", "TestPass123*");

    var result = await handler.Handle(command, CancellationToken.None);

    result.Should().NotBeNull();
    result.Success.Should().BeTrue("una respuesta distinta delataría qué correos están registrados");
    result.Data!.Email.Should().Be("test@example.com");
    result.Data.Status.Should().Be(UserStatusCatalog.Pending);
    await mockRepo.DidNotReceive().CreateUser(Arg.Any<User>());
    await mockUnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    await mockActivationEmailService.Received(1).SendRegistrationAttemptNoticeAsync("test@example.com", Arg.Any<CancellationToken>(), false);
    await mockActivationEmailService.DidNotReceive().SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
  }

  /// <summary>
  /// Verifica que el handler crea un usuario exitosamente cuando el email no existe.
  /// </summary>
  [Theory]
  [InlineData("user1", "user1@test.com")]
  [InlineData("user2", "user2@test.com")]
  [InlineData("user3", "user3@test.com")]
  public async Task Handle_ShouldCreateUser_WhenEmailDoesNotExist(string username, string email)
  {
    var mockRepo = Substitute.For<ICreateUserRepository>();
    var mockUnitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var mockPasswordHasher = Substitute.For<IPasswordHasher>();
    var mockLogger = Substitute.For<IUserLogger>();
    var mockActivationService = Substitute.For<IActivationService>();
    var mockActivationEmailService = Substitute.For<IActivationEmailService>();

    mockRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    mockPasswordHasher.HashPassword(Arg.Any<string>()).Returns("hashed_password_123");
    mockActivationEmailService
      .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(OperationResult.Ok(true)));

    var handler = new CreateUserHandler(mockRepo, mockLogger, mockUnitOfWork, mockPasswordHasher, mockActivationService, mockActivationEmailService);
    var command = new CreateUserCommand(username, email, "Pass123*");

    var result = await handler.Handle(command, CancellationToken.None);

    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data.Username.Should().Be(username);
    result.Data.Email.Should().Be(email);
    result.Error.Should().BeNull();
    result.ErrorInfo.Should().BeNull();

    await mockRepo.Received(1).ExistsByEmailAsync(email, Arg.Any<CancellationToken>());
    await mockRepo.Received(1).CreateUser(Arg.Is<User>(u => u.Username == username && u.Email == email));
    await mockUnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    mockPasswordHasher.Received(1).HashPassword("Pass123*");
    mockActivationService.Received(1).AssignActivationToken(Arg.Any<User>());
    await mockActivationEmailService.Received(1).SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    mockLogger.Received(1).LogUserCreated(Arg.Any<long>(), username, email);
  }

  /// <summary>
  /// Verifica que el handler hashea correctamente la contraseña antes de crear el usuario.
  /// </summary>
  [Theory]
  [InlineData("password123", "hashed_password123")]
  [InlineData("anotherpass456", "hashed_anotherpass456")]
  [InlineData("complexP@ss789!", "hashed_complexP@ss789!")]
  public async Task Handle_ShouldHashPassword_BeforeCreatingUser(string password, string expectedHash)
  {
    var mockRepo = Substitute.For<ICreateUserRepository>();
    var mockUnitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var mockPasswordHasher = Substitute.For<IPasswordHasher>();
    var mockLogger = Substitute.For<IUserLogger>();
    var mockActivationService = Substitute.For<IActivationService>();
    var mockActivationEmailService = Substitute.For<IActivationEmailService>();

    mockRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    mockPasswordHasher.HashPassword(password).Returns(expectedHash);
    mockActivationEmailService
      .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(OperationResult.Ok(true)));

    var handler = new CreateUserHandler(mockRepo, mockLogger, mockUnitOfWork, mockPasswordHasher, mockActivationService, mockActivationEmailService);
    var command = new CreateUserCommand("testuser", "test@example.com", password);

    var result = await handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeTrue();
    mockPasswordHasher.Received(1).HashPassword(password);
    await mockRepo.Received(1).CreateUser(Arg.Is<User>(u => u.PasswordHash == expectedHash));
  }

  /// <summary>
  /// Verifica que el handler propaga la excepción cuando SaveChanges falla.
  /// </summary>
  [Fact]
  public async Task Handle_ShouldPropagateException_WhenSaveChangesFails()
  {
    var mockRepo = Substitute.For<ICreateUserRepository>();
    var mockUnitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var mockPasswordHasher = Substitute.For<IPasswordHasher>();
    var mockLogger = Substitute.For<IUserLogger>();
    var mockActivationService = Substitute.For<IActivationService>();
    var mockActivationEmailService = Substitute.For<IActivationEmailService>();

    mockRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    mockPasswordHasher.HashPassword(Arg.Any<string>()).Returns("hashed_password");
    mockActivationEmailService
      .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(OperationResult.Ok(true)));
    mockUnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
        .Returns(Task.FromException<int>(new InvalidOperationException("Database error")));

    var handler = new CreateUserHandler(mockRepo, mockLogger, mockUnitOfWork, mockPasswordHasher, mockActivationService, mockActivationEmailService);
    var command = new CreateUserCommand("testuser", "test@example.com", "TestPass123*");

    var exception = (await ((Func<Task>)(() => handler.Handle(command, CancellationToken.None)))
        .Should().ThrowAsync<InvalidOperationException>()).Subject;
    exception.Message.Should().Be("Database error");

    await mockRepo.Received(1).CreateUser(Arg.Any<User>());
    await mockUnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  /// <summary>
  /// Verifica que el handler llama correctamente a todas las dependencias en el orden esperado.
  /// </summary>
  [Theory]
  [InlineData("testuser", "test@example.com")]
  [InlineData("anotheruser", "another@example.com")]
  public async Task Handle_ShouldCallDependenciesInCorrectOrder_WhenCreatingUser(string username, string email)
  {
    var mockRepo = Substitute.For<ICreateUserRepository>();
    var mockUnitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var mockPasswordHasher = Substitute.For<IPasswordHasher>();
    var mockLogger = Substitute.For<IUserLogger>();
    var mockActivationService = Substitute.For<IActivationService>();
    var mockActivationEmailService = Substitute.For<IActivationEmailService>();

    mockRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    mockPasswordHasher.HashPassword(Arg.Any<string>()).Returns("hashed_password");
    mockActivationEmailService
      .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(OperationResult.Ok(true)));

    var handler = new CreateUserHandler(mockRepo, mockLogger, mockUnitOfWork, mockPasswordHasher, mockActivationService, mockActivationEmailService);
    var command = new CreateUserCommand(username, email, "TestPass123*");

    var result = await handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeTrue();

    Received.InOrder(() =>
    {
      mockPasswordHasher.HashPassword("TestPass123*");
      mockRepo.ExistsByEmailAsync(email, Arg.Any<CancellationToken>());
      mockRepo.CreateUser(Arg.Any<User>());
      mockActivationService.AssignActivationToken(Arg.Any<User>());
      mockActivationEmailService.SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
      mockUnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
      mockLogger.LogUserCreated(Arg.Any<long>(), username, email);
    });
  }

  /// <summary>
  /// Verifica que la respuesta expone PublicId (Guid) y NO el Id interno (long).
  /// Protege contra regresiones de seguridad que expondrían el ID de base de datos.
  /// </summary>
  [Fact(DisplayName = "Handle retorna PublicId (Guid) en el DTO - no expone Id interno (long)")]
  public async Task Handle_WhenUserCreated_ResponseContainsPublicIdNotInternalId()
  {
    var mockRepo = Substitute.For<ICreateUserRepository>();
    var mockUnitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var mockPasswordHasher = Substitute.For<IPasswordHasher>();
    var mockLogger = Substitute.For<IUserLogger>();
    var mockActivationService = Substitute.For<IActivationService>();
    var mockActivationEmailService = Substitute.For<IActivationEmailService>();

    var expectedPublicId = Guid.NewGuid();
    mockRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    mockPasswordHasher.HashPassword(Arg.Any<string>()).Returns("hashed");
    mockActivationEmailService
      .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(OperationResult.Ok(true)));
    mockRepo.CreateUser(Arg.Do<User>(u => u.PublicId = expectedPublicId))
        .Returns(callInfo => Task.FromResult(callInfo.Arg<User>()));

    var handler = new CreateUserHandler(mockRepo, mockLogger, mockUnitOfWork, mockPasswordHasher, mockActivationService, mockActivationEmailService);
    var command = new CreateUserCommand("secuser", "sec@example.com", "SecPass123*");

    var result = await handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.PublicId.Should().Be(expectedPublicId);
    result.Data.PublicId.Should().NotBe(Guid.Empty);

    // Verificar en tiempo de compilación que CreateUserResponseDto.PublicId es Guid
    // (si el tipo fuera long, esta asignación no compilaría)
    Guid _ = result.Data.PublicId;
  }

  [Fact(DisplayName = "Handle retorna error de dependencia cuando falla envio de correo de activacion")]
  public async Task Handle_WhenActivationEmailFails_ReturnsDependencyError()
  {
    var mockRepo = Substitute.For<ICreateUserRepository>();
    var mockUnitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var mockPasswordHasher = Substitute.For<IPasswordHasher>();
    var mockLogger = Substitute.For<IUserLogger>();
    var mockActivationService = Substitute.For<IActivationService>();
    var mockActivationEmailService = Substitute.For<IActivationEmailService>();

    mockRepo.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
    mockPasswordHasher.HashPassword(Arg.Any<string>()).Returns("hashed");
    mockActivationEmailService
      .SendActivationEmailAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(OperationResult.Fail<bool>(ErrorInfo.Status424Dependency, "smtp error")));

    var handler = new CreateUserHandler(mockRepo, mockLogger, mockUnitOfWork, mockPasswordHasher, mockActivationService, mockActivationEmailService);
    var command = new CreateUserCommand("mailfail", "mailfail@example.com", "SecPass123*");

    var result = await handler.Handle(command, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().BeEquivalentTo(ErrorInfo.Status424Dependency);
    await mockUnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
