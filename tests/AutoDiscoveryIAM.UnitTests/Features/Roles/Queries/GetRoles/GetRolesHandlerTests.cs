using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoles;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Queries.GetRoles;

public class GetRolesHandlerTests
{
    private readonly IGetRolesRepository _repository;
    private readonly IRoleLogger _logger;
    private readonly GetRolesHandler _handler;

    public GetRolesHandlerTests()
    {
        _repository = Substitute.For<IGetRolesRepository>();
        _logger = Substitute.For<IRoleLogger>();
        _handler = new GetRolesHandler(_repository, _logger);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsRoles_ShouldMapAndReturnSuccess()
    {
        var roles = new[]
        {
            new Role
            {
                PublicId = Guid.NewGuid(),
                Name = "admin",
                Description = "administrador",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

        _repository.GetRoles(Arg.Any<int>(), Arg.Any<int>()).Returns((roles, roles.Length));

        var result = await _handler.Handle(new GetRolesQuery(1, 20), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(roles.Length);
        var dto = result.Data!.Items.Single();
        dto.PublicId.Should().Be(roles[0].PublicId);
        dto.Name.Should().Be(roles[0].Name);
        dto.Description.Should().Be(roles[0].Description);
        dto.IsDeleted.Should().Be(roles[0].IsDeleted);

        _logger.Received(1).LogRolesListed(roles.Length);
    }
}
