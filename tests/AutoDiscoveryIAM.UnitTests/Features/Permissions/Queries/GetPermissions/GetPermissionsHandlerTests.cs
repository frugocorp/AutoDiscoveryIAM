using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetPermissions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Queries.GetPermissions;

public class GetPermissionsHandlerTests
{
    private readonly IGetPermissionsRepository _repository;
    private readonly IPermissionLogger _logger;
    private readonly GetPermissionsHandler _handler;

    public GetPermissionsHandlerTests()
    {
        _repository = Substitute.For<IGetPermissionsRepository>();
        _logger = Substitute.For<IPermissionLogger>();
        _handler = new GetPermissionsHandler(_repository, _logger);
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsPermissions_ShouldMapAndReturnSuccess()
    {
        var permissions = new[]
        {
            new Permission
            {
                PublicId = Guid.NewGuid(),
                Key = "users.read",
                Description = "permiso de lectura",
                Group = "Users",
                IsActive = true,
                Route = "/users",
                HttpMethod = "GET",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
        };

        _repository.GetPermissions(Arg.Any<int>(), Arg.Any<int>()).Returns((permissions, permissions.Length));

        var result = await _handler.Handle(new GetPermissionsQuery(1, 20), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(permissions.Length);
        var dto = result.Data!.Items.Single();
        dto.PublicId.Should().Be(permissions[0].PublicId);
        dto.Key.Should().Be(permissions[0].Key);
        dto.Route.Should().Be(permissions[0].Route);
        dto.HttpMethod.Should().Be(permissions[0].HttpMethod);

        _logger.Received(1).LogPermissionsRetrieved(permissions.Length);
    }
}
