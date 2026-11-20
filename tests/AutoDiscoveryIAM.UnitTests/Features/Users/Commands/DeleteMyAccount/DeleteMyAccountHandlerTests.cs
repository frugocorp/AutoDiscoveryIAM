using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.DeleteMyAccount;

public class DeleteMyAccountHandlerTests
{
  private static (DeleteMyAccountHandler Handler, IDeleteMyAccountRepository Repository, IAutoDiscoveryIAMUnitOfWork AutoDiscoveryIAMUnitOfWork, IUserLogger Logger) CreateSut()
  {
    var repository = Substitute.For<IDeleteMyAccountRepository>();
    var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var logger = Substitute.For<IUserLogger>();
    var passwordHasher = Substitute.For<IPasswordHasher>();
    passwordHasher.VerifyPassword("correct-password", "stored-hash").Returns(true);
    var handler = new DeleteMyAccountHandler(repository, unitOfWork, logger, passwordHasher);
    return (handler, repository, unitOfWork, logger);
  }

  [Fact]
  public async Task Handle_WhenUserNotFound_ShouldReturnNotFound()
  {
    var (handler, repository, _, _) = CreateSut();
    var publicId = Guid.NewGuid();
    repository.GetByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns((User?)null);

    var result = await handler.Handle(new DeleteMyAccountCommand(publicId, "correct-password"), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
  }

  [Fact]
  public async Task Handle_WhenValid_ShouldSoftDeleteOwnAccount()
  {
    var (handler, repository, unitOfWork, logger) = CreateSut();
    var user = new User { Id = 3, PublicId = Guid.NewGuid(), Username = "self-delete", PasswordHash = "stored-hash" };
    repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);

    var result = await handler.Handle(new DeleteMyAccountCommand(user.PublicId, "correct-password"), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.PublicId.Should().Be(user.PublicId);
    result.Data.Status.Should().Be("Eliminado");

    repository.Received(1).DeleteAccount(user);
    await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    logger.Received(1).LogUserDeleted(user.Id, user.Username);
  }

  [Theory(DisplayName = "Sin la contraseña actual correcta no se elimina la cuenta")]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("wrong-password")]
  public async Task Handle_WithoutCorrectPassword_ShouldNotDelete(string? password)
  {
    var (handler, repository, unitOfWork, _) = CreateSut();
    var user = new User { Id = 4, PublicId = Guid.NewGuid(), Username = "stolen-token", PasswordHash = "stored-hash" };
    repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);

    var result = await handler.Handle(new DeleteMyAccountCommand(user.PublicId, password), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
    repository.DidNotReceive().DeleteAccount(Arg.Any<User>());
    await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
