using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateUser;

public class UpdateUserHandlerTests
{
  [Fact]
  public async Task Handle_WhenUserNotFound_ShouldReturnNotFound()
  {
    var repository = Substitute.For<IUpdateUserRepository>();
    var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var logger = Substitute.For<IUserLogger>();
    var handler = new UpdateUserHandler(repository, unitOfWork, logger, Substitute.For<IAuthContextService>());
    var publicId = Guid.NewGuid();

    repository.GetByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns((User?)null);

    var result = await handler.Handle(new UpdateUserCommand(publicId, new UpdateUserBodyDto("usr", "usr@test.local", "notes")), CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
  }

  [Fact]
  public async Task Handle_WhenUserExists_ShouldReturnUpdatedResponse()
  {
    var repository = Substitute.For<IUpdateUserRepository>();
    var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var logger = Substitute.For<IUserLogger>();
    var handler = new UpdateUserHandler(repository, unitOfWork, logger, Substitute.For<IAuthContextService>());

    var user = new User { Id = 15, PublicId = Guid.NewGuid(), Username = "oldusr", Email = "old@test.local" };
    repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    repository.ExistsUsernameAsync("newusr", user.Id, Arg.Any<CancellationToken>()).Returns(false);
    repository.ExistsEmailAsync("new@test.local", user.Id, Arg.Any<CancellationToken>()).Returns(false);

    var cmd = new UpdateUserCommand(user.PublicId, new UpdateUserBodyDto("newusr", "new@test.local", "updated"));
    var result = await handler.Handle(cmd, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.PublicId.Should().Be(user.PublicId);
    result.Data.Username.Should().Be("newusr");
    result.Data.Email.Should().Be("new@test.local");
  }

  [Fact(DisplayName = "Handle retorna 409 cuando otro proceso modifico el usuario concurrentemente (issue #075)")]
  public async Task Handle_WhenConcurrentUpdateConflictOccurs_ShouldReturnConflict()
  {
    var repository = Substitute.For<IUpdateUserRepository>();
    var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var logger = Substitute.For<IUserLogger>();
    var handler = new UpdateUserHandler(repository, unitOfWork, logger, Substitute.For<IAuthContextService>());

    var user = new User { Id = 15, PublicId = Guid.NewGuid(), Username = "oldusr", Email = "old@test.local" };
    repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);
    repository.ExistsUsernameAsync("newusr", user.Id, Arg.Any<CancellationToken>()).Returns(false);
    repository.ExistsEmailAsync("new@test.local", user.Id, Arg.Any<CancellationToken>()).Returns(false);
    unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
      .Returns<Task<int>>(_ => throw new DbUpdateConcurrencyException("Row was modified concurrently."));

    var cmd = new UpdateUserCommand(user.PublicId, new UpdateUserBodyDto("newusr", "new@test.local", "updated"));
    var result = await handler.Handle(cmd, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
  }
  [Theory(DisplayName = "Handle: el email de una cuenta con flags especiales solo lo cambia un nivel igual o superior")]
  [InlineData("dev", "none", false)]
  [InlineData("dev", "admin", false)]
  [InlineData("dev", "su", false)]
  [InlineData("dev", "dev", true)]
  [InlineData("su", "none", false)]
  [InlineData("su", "admin", false)]
  [InlineData("su", "su", true)]
  [InlineData("su", "dev", true)]
  [InlineData("admin", "admin", false)]
  [InlineData("admin", "su", true)]
  [InlineData("none", "none", true)]
  public async Task Handle_EmailChangeOnPrivilegedTarget_RequiresEqualOrHigherLevel(string targetLevel, string callerLevel, bool expectedSuccess)
  {
    var repository = Substitute.For<IUpdateUserRepository>();
    var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
    var logger = Substitute.For<IUserLogger>();
    var authContext = Substitute.For<IAuthContextService>();
    var handler = new UpdateUserHandler(repository, unitOfWork, logger, authContext);

    var target = WithLevel(new User { Id = 15, PublicId = Guid.NewGuid(), Username = "target", Email = "old@test.local" }, targetLevel);
    var caller = WithLevel(new User { Id = 16, PublicId = Guid.NewGuid(), Username = "caller", Email = "caller@test.local" }, callerLevel);
    repository.GetByPublicIdAsync(target.PublicId, Arg.Any<CancellationToken>()).Returns(target);
    repository.ExistsEmailAsync("new@test.local", target.Id, Arg.Any<CancellationToken>()).Returns(false);
    authContext.GetCurrentUserAsync().Returns(caller);

    var result = await handler.Handle(new UpdateUserCommand(target.PublicId, new UpdateUserBodyDto(null, "new@test.local", null)), CancellationToken.None);

    result.Success.Should().Be(expectedSuccess);
    if (!expectedSuccess)
    {
      result.ErrorInfo.Should().Be(ErrorInfo.Status403Forbidden);
      target.Email.Should().Be("old@test.local");
    }
  }

  private static User WithLevel(User user, string level)
  {
    user.IsAdmin = level == "admin";
    user.IsSuAdmin = level == "su";
    user.IsDevelopment = level == "dev";
    return user;
  }
}
