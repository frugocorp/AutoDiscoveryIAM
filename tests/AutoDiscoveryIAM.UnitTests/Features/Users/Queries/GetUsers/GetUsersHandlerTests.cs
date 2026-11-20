using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Queries.GetUsers;

public class GetUsersHandlerTests
{
    private readonly IGetUsersRepository _repository;
    private readonly GetUsersHandler _handler;

    public GetUsersHandlerTests()
    {
        _repository = Substitute.For<IGetUsersRepository>();
        _handler = new GetUsersHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenQueryIsNull_ShouldThrowArgumentNullException()
    {
        await ((Func<Task>)(() => _handler.Handle(null!, CancellationToken.None))).Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsUsers_ShouldMapAndReturnSuccess()
    {
        var user = new User
        {
            Id = 1,
            PublicId = Guid.NewGuid(),
            Username = "john",
            Email = "john@test.local",
            PasswordHash = "hash",
            IsActive = true,
            IsAdmin = false,
            IsDevelopment = true,
            IsSuAdmin = false,
            IsDeleted = false,
        };

        _repository.GetUsersAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((new List<User> { user }, 1));

        var result = await _handler.Handle(new GetUsersQuery(1, 20), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalCount.Should().Be(1);
        var dto = result.Data!.Items.Single();
        dto.Username.Should().Be(user.Username);
        dto.Email.Should().Be(user.Email);
    }
}
