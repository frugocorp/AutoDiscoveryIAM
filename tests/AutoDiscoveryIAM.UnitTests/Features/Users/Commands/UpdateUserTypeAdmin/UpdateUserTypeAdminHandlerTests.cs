using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateUserTypeAdmin;

public class UpdateUserTypeAdminHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IUpdateUserTypeAdminRepository>();
        var logger = Substitute.For<IUserLogger>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var handler = new UpdateUserTypeAdminHandler(repository, logger, unitOfWork, Substitute.For<IAuthContextService>());
        var publicId = Guid.NewGuid();

        repository.GetByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await handler.Handle(new UpdateUserTypeAdminCommand(publicId, new UpdateUserTypeAdminBodyDto(true, "promote")), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ShouldUpdateAdminFlag()
    {
        var repository = Substitute.For<IUpdateUserTypeAdminRepository>();
        var logger = Substitute.For<IUserLogger>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var handler = new UpdateUserTypeAdminHandler(repository, logger, unitOfWork, Substitute.For<IAuthContextService>());

        var user = new User
        {
            PublicId = Guid.NewGuid(),
            Username = "target",
            Email = "target@test.local",
            IsAdmin = false,
            IsDeleted = false,
        };
        repository.GetByPublicIdAsync(user.PublicId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await handler.Handle(new UpdateUserTypeAdminCommand(user.PublicId, new UpdateUserTypeAdminBodyDto(true, "promote")), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PublicId.Should().Be(user.PublicId);
        result.Data.IsAdmin.Should().BeTrue();
    }
}
