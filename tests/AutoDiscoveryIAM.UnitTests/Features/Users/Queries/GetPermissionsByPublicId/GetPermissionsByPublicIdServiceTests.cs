using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Queries.GetPermissionsByPublicId;

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
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        var userPublicId = Guid.NewGuid();
        _repository.UserExistsAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.ExecuteAsync(userPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserNotFound.Message, userPublicId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsInactive_ShouldReturnBusinessErrorWithCentralizedMessage()
    {
        var userPublicId = Guid.NewGuid();
        _repository.UserExistsAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.UserIsActiveAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.ExecuteAsync(userPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserInactiveOrDeleted.Message, userPublicId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsValid_ShouldReturnPermissions()
    {
        var userPublicId = Guid.NewGuid();
        var expected = new[]
        {
            new GetPermissionsByPublicIdResponseDto(Guid.NewGuid(), "users.read", "desc", "users"),
        };

        _repository.UserExistsAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.UserIsActiveAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.GetPermissionsAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _service.ExecuteAsync(userPublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(expected);
    }
}
