using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.CreateRole;

public class CreateRoleHandlerTests
{
    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ShouldReturnBusinessError()
    {
        var repository = Substitute.For<ICreateRoleRepository>();
        var logger = Substitute.For<IRoleLogger>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var handler = new CreateRoleHandler(repository, logger, unitOfWork);

        repository.RoleNameExistsAsync("admin").Returns(true);

        var result = await handler.Handle(new CreateRoleCommand("admin", "desc", true), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    }

    [Fact]
    public async Task Handle_WhenNameIsNew_ShouldCreateRole()
    {
        var repository = Substitute.For<ICreateRoleRepository>();
        var logger = Substitute.For<IRoleLogger>();
        var unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        var handler = new CreateRoleHandler(repository, logger, unitOfWork);

        repository.RoleNameExistsAsync(Arg.Any<string>()).Returns(false);
        repository.CreateRole(Arg.Any<Role>()).Returns(ci => ci.Arg<Role>());

        var result = await handler.Handle(new CreateRoleCommand("a03-role", "desc", true), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("a03-role");
    }
}
