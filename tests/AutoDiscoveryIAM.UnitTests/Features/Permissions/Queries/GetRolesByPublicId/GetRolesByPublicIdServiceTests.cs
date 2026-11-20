using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Queries.GetRolesByPublicId;

public class GetRolesByPublicIdServiceTests
{
    private readonly IGetRolesByPublicIdRepository _repository;
    private readonly GetRolesByPublicIdService _service;

    public GetRolesByPublicIdServiceTests()
    {
        _repository = Substitute.For<IGetRolesByPublicIdRepository>();
        _service = new GetRolesByPublicIdService(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPermissionDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        var permissionPublicId = Guid.NewGuid();
        _repository.PermissionExistsAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.ExecuteAsync(permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionNotFound.Message, permissionPublicId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPermissionIsInactive_ShouldReturnBusinessErrorWithCentralizedMessage()
    {
        var permissionPublicId = Guid.NewGuid();
        _repository.PermissionExistsAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.PermissionIsActiveAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.ExecuteAsync(permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionInactiveOrDeleted.Message, permissionPublicId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPermissionIsValid_ShouldReturnRoles()
    {
        var permissionPublicId = Guid.NewGuid();
        var expected = new[]
        {
            new GetRolesByPublicIdResponseDto(Guid.NewGuid(), "admin", "desc", DateTime.UtcNow),
        };

        _repository.PermissionExistsAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.PermissionIsActiveAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.GetRolesAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _service.ExecuteAsync(permissionPublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(expected);
    }
}
