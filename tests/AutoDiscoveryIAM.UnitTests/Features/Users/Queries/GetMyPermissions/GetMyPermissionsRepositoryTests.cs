using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Queries.GetMyPermissions;

public class GetMyPermissionsRepositoryTests
{
    private static readonly string[] ExpectedKeys = ["users.read", "posts.read"];

    private readonly IGetPermissionsByPublicIdRepository _baseRepository;
    private readonly GetMyPermissionsRepository _repository;

    public GetMyPermissionsRepositoryTests()
    {
        _baseRepository = Substitute.For<IGetPermissionsByPublicIdRepository>();
        _repository = new GetMyPermissionsRepository(_baseRepository);
    }

    [Fact]
    public async Task UserExistsAsync_ShouldDelegateToBaseRepository()
    {
        var userPublicId = Guid.NewGuid();
        _baseRepository.UserExistsAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _repository.UserExistsAsync(userPublicId, CancellationToken.None);

        result.Should().BeTrue();
        await _baseRepository.Received(1).UserExistsAsync(userPublicId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UserIsActiveAsync_ShouldDelegateToBaseRepository()
    {
        var userPublicId = Guid.NewGuid();
        _baseRepository.UserIsActiveAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _repository.UserIsActiveAsync(userPublicId, CancellationToken.None);

        result.Should().BeTrue();
        await _baseRepository.Received(1).UserIsActiveAsync(userPublicId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPermissionsAsync_ShouldMapResponseDtosFromBaseRepository()
    {
        var userPublicId = Guid.NewGuid();
        var baseData = new[]
        {
      new GetPermissionsByPublicIdResponseDto(Guid.NewGuid(), "users.read", "Can read users", "Users"),
      new GetPermissionsByPublicIdResponseDto(Guid.NewGuid(), "posts.read", "Can read posts", "Posts"),
    };

        _baseRepository.GetPermissionsAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(baseData);

        var result = await _repository.GetPermissionsAsync(userPublicId, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(permission => permission.Key).Should().BeEquivalentTo(ExpectedKeys);
        result.All(permission => permission is GetMyPermissionsResponseDto).Should().BeTrue();
        await _baseRepository.Received(1).GetPermissionsAsync(userPublicId, Arg.Any<CancellationToken>());
    }
}