using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Queries.GetUsers;

public class GetUsersRepositoryTests
{
    [Fact]
    public async Task GetUsersAsync_ShouldReturnOnlyActiveUsersOrderedByNameAndUsername()
    {
        var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
          .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
          .Options;

        await using var context = new AutoDiscoveryIAMDbContext(options);

        var userB = new User { Username = "zuser", Email = "z@test.com", PasswordHash = "hash", IsDeleted = false };
        var userA = new User { Username = "auser", Email = "a@test.com", PasswordHash = "hash", IsDeleted = false };
        var userDeleted = new User { Username = "deleted", Email = "d@test.com", PasswordHash = "hash", IsDeleted = false };

        context.Users.AddRange(userB, userA, userDeleted);
        await context.SaveChangesAsync(CancellationToken.None);

        userDeleted.IsDeleted = true;
        await context.SaveChangesAsync(CancellationToken.None);

        var repository = new GetUsersRepository(context);

        var (items, totalCount) = await repository.GetUsersAsync(1, 20, CancellationToken.None);

        totalCount.Should().Be(2);
        items.Should().HaveCount(2);
        items[0].Username.Should().Be("auser");
        items[1].Username.Should().Be("zuser");
    }
}
