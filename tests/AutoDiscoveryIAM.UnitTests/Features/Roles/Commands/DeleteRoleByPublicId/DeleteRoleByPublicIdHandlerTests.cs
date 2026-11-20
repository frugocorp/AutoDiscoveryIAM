using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.DeleteRoleByPublicId;

public class DeleteRoleByPublicIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenRoleNotFound_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IDeleteRoleByPublicIdRepository>();
        var logger = Substitute.For<IRoleLogger>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var handler = new DeleteRoleByPublicIdHandler(repository, logger, unitOfWork);
        var publicId = Guid.NewGuid();

        repository.GetRoleByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await handler.Handle(new DeleteRoleByPublicIdCommand(publicId), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    }

    [Fact]
    public async Task Handle_WhenRoleExistsAndNoUsers_ShouldDelete()
    {
        var repository = Substitute.For<IDeleteRoleByPublicIdRepository>();
        var logger = Substitute.For<IRoleLogger>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var handler = new DeleteRoleByPublicIdHandler(repository, logger, unitOfWork);
        var role = new Role { Id = 9, PublicId = Guid.NewGuid(), Name = "a03-del", IsDeleted = false };

        repository.GetRoleByPublicIdAsync(role.PublicId, Arg.Any<CancellationToken>()).Returns(role);
        repository.HasActiveUsersAsync(role.Id, Arg.Any<CancellationToken>()).Returns(false);
        repository.DeleteRoleAsync(role, Arg.Any<CancellationToken>()).Returns(true);

        var result = await handler.Handle(new DeleteRoleByPublicIdCommand(role.PublicId), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PublicId.Should().Be(role.PublicId);
    }
}
