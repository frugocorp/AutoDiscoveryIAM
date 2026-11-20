using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.UpdateRoleByPublicId;

public class UpdateRoleByPublicIdHandlerTests
{
    [Fact]
    public async Task Handle_WhenRoleNotFound_ShouldReturnNotFound()
    {
        var repository = Substitute.For<IUpdateRoleByPublicIdRepository>();
        var logger = Substitute.For<IRoleLogger>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var handler = new UpdateRoleByPublicIdHandler(repository, logger, unitOfWork);
        var publicId = Guid.NewGuid();

        repository.GetByPublicIdAsync(publicId, Arg.Any<CancellationToken>()).Returns((Role?)null);

        var command = new UpdateRoleByPublicIdCommand(publicId, new UpdateRoleByPublicIdBodyDto("a03-upd", "desc"));
        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    }

    [Fact]
    public async Task Handle_WhenRoleExists_ShouldUpdateAndReturnSuccess()
    {
        var repository = Substitute.For<IUpdateRoleByPublicIdRepository>();
        var logger = Substitute.For<IRoleLogger>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var handler = new UpdateRoleByPublicIdHandler(repository, logger, unitOfWork);

        var role = new Role { Id = 12, PublicId = Guid.NewGuid(), Name = "old", IsDeleted = false, CreatedAt = DateTime.UtcNow };
        repository.GetByPublicIdAsync(role.PublicId, Arg.Any<CancellationToken>()).Returns(role);
        repository.GetByNameAsync("a03-upd", Arg.Any<CancellationToken>()).Returns((Role?)null);
        repository.UpdateAsync(role, Arg.Any<CancellationToken>()).Returns(role);

        var command = new UpdateRoleByPublicIdCommand(role.PublicId, new UpdateRoleByPublicIdBodyDto("a03-upd", "updated"));
        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PublicId.Should().Be(role.PublicId);
        result.Data.Name.Should().Be("a03-upd");
    }

    [Fact(DisplayName = "Handle retorna 409 cuando otro proceso modifico el rol concurrentemente (issue #075)")]
    public async Task Handle_WhenConcurrentUpdateConflictOccurs_ShouldReturnConflict()
    {
        var repository = Substitute.For<IUpdateRoleByPublicIdRepository>();
        var logger = Substitute.For<IRoleLogger>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var handler = new UpdateRoleByPublicIdHandler(repository, logger, unitOfWork);

        var role = new Role { Id = 12, PublicId = Guid.NewGuid(), Name = "old", IsDeleted = false, CreatedAt = DateTime.UtcNow };
        repository.GetByPublicIdAsync(role.PublicId, Arg.Any<CancellationToken>()).Returns(role);
        repository.GetByNameAsync("a03-upd", Arg.Any<CancellationToken>()).Returns((Role?)null);
        repository.UpdateAsync(role, Arg.Any<CancellationToken>()).Returns(role);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new DbUpdateConcurrencyException("Row was modified concurrently."));

        var command = new UpdateRoleByPublicIdCommand(role.PublicId, new UpdateRoleByPublicIdBodyDto("a03-upd", "updated"));
        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status409Conflict);
    }
}
