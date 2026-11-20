using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetUsersByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Queries.GetUsersByPublicId;

public class GetUsersByPublicIdServiceTests
{
    private readonly IGetUsersByPublicIdRepository _repository;
    private readonly GetUsersByPublicIdService _service;

    public GetUsersByPublicIdServiceTests()
    {
        _repository = Substitute.For<IGetUsersByPublicIdRepository>();
        _service = new GetUsersByPublicIdService(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPermissionDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        var permissionPublicId = Guid.NewGuid();
        _repository.PermissionExistsAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.ExecuteAsync(permissionPublicId, 1, 20, CancellationToken.None);

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

        var result = await _service.ExecuteAsync(permissionPublicId, 1, 20, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionInactiveOrDeleted.Message, permissionPublicId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenPermissionIsValid_ShouldReturnUsers()
    {
        var permissionPublicId = Guid.NewGuid();
        var expected = new[]
        {
            new GetUsersByPublicIdResponseDto(Guid.NewGuid(), "john", "john@test.local", DateTime.UtcNow),
        };

        _repository.PermissionExistsAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.PermissionIsActiveAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.GetUsersAsync(permissionPublicId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _service.ExecuteAsync(permissionPublicId, 1, 20, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(expected);
    }
}
