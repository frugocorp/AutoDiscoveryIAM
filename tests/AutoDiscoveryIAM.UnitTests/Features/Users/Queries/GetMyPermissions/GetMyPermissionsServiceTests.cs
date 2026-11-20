using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Queries.GetMyPermissions;

public class GetMyPermissionsServiceTests
{
    private readonly IGetMyPermissionsRepository _repository;
    private readonly GetMyPermissionsService _service;

    public GetMyPermissionsServiceTests()
    {
        _repository = Substitute.For<IGetMyPermissionsRepository>();
        _service = new GetMyPermissionsService(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserContextIsMissing_ShouldReturnUnauthorized()
    {
        var result = await _service.ExecuteAsync(null, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status401Unauthorized);
        result.Error.Should().Be(AuthorizationValidationTypes.Messages.InvalidUserContext);
        await _repository.DidNotReceiveWithAnyArgs().UserExistsAsync(default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var userPublicId = Guid.NewGuid();
        _repository.UserExistsAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.ExecuteAsync(userPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserNotFound.Message, userPublicId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIsInactive_ShouldReturnBusinessError()
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
    public async Task ExecuteAsync_WhenUserIsActiveAndExists_ShouldReturnPermissions()
    {
        var userPublicId = Guid.NewGuid();
        var expected = new[]
        {
      new GetMyPermissionsResponseDto(Guid.NewGuid(), "users.read", "Can read users", "Users"),
      new GetMyPermissionsResponseDto(Guid.NewGuid(), "users.update", "Can update users", "Users"),
    };

        _repository.UserExistsAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.UserIsActiveAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(true);
        _repository.GetPermissionsAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _service.ExecuteAsync(userPublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(expected);
    }
}