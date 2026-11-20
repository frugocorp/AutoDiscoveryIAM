using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Queries.GetPermissionsByPublicId;

public class GetPermissionsByPublicIdServiceTests
{
    private readonly IGetPermissionsByPublicIdRepository _repository;
    private readonly GetPermissionsByPublicIdService _service;

    public GetPermissionsByPublicIdServiceTests()
    {
        _repository = Substitute.For<IGetPermissionsByPublicIdRepository>();
        _service = new GetPermissionsByPublicIdService(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        var rolePublicId = Guid.NewGuid();
        _repository.RoleExistsAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.ExecuteAsync(rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleNotFound.Message, rolePublicId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleIsInactive_ShouldReturnBusinessErrorWithCentralizedMessage()
    {
        var rolePublicId = Guid.NewGuid();
        _repository.RoleExistsAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.RoleIsActiveAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.ExecuteAsync(rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleInactiveOrDeleted.Message, rolePublicId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleIsValid_ShouldReturnPermissions()
    {
        var rolePublicId = Guid.NewGuid();
        var expected = new[]
        {
            new GetPermissionsByPublicIdResponseDto(Guid.NewGuid(), "roles.read", "desc", "roles", DateTime.UtcNow),
        };

        _repository.RoleExistsAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.RoleIsActiveAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.GetPermissionsAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _service.ExecuteAsync(rolePublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(expected);
    }
}
