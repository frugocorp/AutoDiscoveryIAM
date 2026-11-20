using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Queries.GetUserByPublicId;

public class GetUserByPublicIdRepositoryTests
{
  [Fact]
  public async Task GetUserByPublicIdAsync_ShouldReturnUserWhenNotDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var user = new User { PublicId = Guid.NewGuid(), Username = "active.user", Email = "a@test.com", PasswordHash = "hash", IsDeleted = false };
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetUserByPublicIdRepository(context);

    var result = await repository.GetUserByPublicIdAsync(user.PublicId, CancellationToken.None);

    result.Should().NotBeNull();
    result!.PublicId.Should().Be(user.PublicId);
  }

  [Fact]
  public async Task GetUserByPublicIdAsync_ShouldReturnNullWhenUserIsDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var user = new User { PublicId = Guid.NewGuid(), Username = "deleted.user", Email = "d@test.com", PasswordHash = "hash", IsDeleted = false };
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    user.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetUserByPublicIdRepository(context);

    var result = await repository.GetUserByPublicIdAsync(user.PublicId, CancellationToken.None);

    result.Should().BeNull();
  }
}
