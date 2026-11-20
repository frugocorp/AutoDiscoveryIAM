using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Queries.GetUsersByPublicId;

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
    public async Task ExecuteAsync_WhenRoleDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        var rolePublicId = Guid.NewGuid();
        _repository.RoleExistsAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.ExecuteAsync(rolePublicId, 1, 20, CancellationToken.None);

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

        var result = await _service.ExecuteAsync(rolePublicId, 1, 20, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleInactiveOrDeleted.Message, rolePublicId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleIsValid_ShouldReturnUsers()
    {
        var rolePublicId = Guid.NewGuid();
        var expected = new[]
        {
            new GetUsersByPublicIdResponseDto(Guid.NewGuid(), "john", "john@test.local", DateTime.UtcNow),
        };

        _repository.RoleExistsAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.RoleIsActiveAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.GetUsersAsync(rolePublicId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _service.ExecuteAsync(rolePublicId, 1, 20, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(expected);
    }
}
