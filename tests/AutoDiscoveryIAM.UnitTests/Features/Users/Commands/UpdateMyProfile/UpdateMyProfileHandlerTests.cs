using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateMyProfile;

public class UpdateMyProfileHandlerTests
{
  private static (UpdateMyProfileHandler Handler, IUpdateMyProfileRepository Repository, IAutoDiscoveryIAMUnitOfWork AutoDiscoveryIAMUnitOfWork, IUserLogger Logger) CreateSut()
  {
    var repository = Substitute.For<IUpdateMyProfileRepository>();
    var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var logger = Substitute.For<IUserLogger>();
    var handler = new UpdateMyProfileHandler(repository, unitOfWork, logger);
    return (handler, repository, unitOfWork, logger);
  }

  [Fact]
  public async Task Handle_WhenUserNotFound_ShouldReturnNotFound()
  {
    var (handler, repository, _, _) = CreateSut();
    var publicId = Guid.NewGuid();
    repository.GetByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns((User?)null);

    var result = await handler.Handle(new UpdateMyProfileCommand(publicId, null), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
  }

  [Fact]
  public async Task Handle_WhenValid_ShouldUpdateOnlyProvidedFields()
  {
    var (handler, repository, unitOfWork, logger) = CreateSut();
    var user = new User { Id = 1, PublicId = Guid.NewGuid(), PhoneNumber = "111" };
    repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);

    var result = await handler.Handle(new UpdateMyProfileCommand(user.PublicId, "+521234567890"), CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data!.PhoneNumber.Should().Be("+521234567890");

    await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    logger.Received(1).LogUserUpdatedSuccessfully(user.PublicId, Arg.Any<string>());
  }

  [Fact]
  public async Task Handle_WhenConcurrentUpdateConflictOccurs_ShouldReturnConflict()
  {
    var (handler, repository, unitOfWork, _) = CreateSut();
    var user = new User { Id = 1, PublicId = Guid.NewGuid() };
    repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
        .Returns<Task<int>>(_ => throw new DbUpdateConcurrencyException());

    var result = await handler.Handle(new UpdateMyProfileCommand(user.PublicId, null), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
  }
}
