using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.RestoreRoleByPublicId;

public class RestoreRoleByPublicIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenDeletedRoleNotFound_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IRestoreRoleByPublicIdRepository>();
        var logger = Substitute.For<IRoleLogger>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var handler = new RestoreRoleByPublicIdHandler(repository, logger, unitOfWork);
        var publicId = Guid.NewGuid();

        repository.GetDeletedRoleByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await handler.Handle(new RestoreRoleByPublicIdRequestDto(publicId), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    }

    [Fact]
    public async Task Handle_WhenDeletedRoleExists_ShouldRestore()
    {
        var repository = Substitute.For<IRestoreRoleByPublicIdRepository>();
        var logger = Substitute.For<IRoleLogger>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var handler = new RestoreRoleByPublicIdHandler(repository, logger, unitOfWork);
        var role = new Role { Id = 7, PublicId = Guid.NewGuid(), Name = "a03-restore", IsDeleted = true };

        repository.GetDeletedRoleByPublicIdAsync(role.PublicId, Arg.Any<CancellationToken>()).Returns(role);
        repository.ExistsActiveRoleWithNameAsync(role.Name, Arg.Any<CancellationToken>()).Returns(false);
        repository.RestoreRoleAsync(role, Arg.Any<CancellationToken>()).Returns(true);

        var result = await handler.Handle(new RestoreRoleByPublicIdRequestDto(role.PublicId), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PublicId.Should().Be(role.PublicId);
    }
}
