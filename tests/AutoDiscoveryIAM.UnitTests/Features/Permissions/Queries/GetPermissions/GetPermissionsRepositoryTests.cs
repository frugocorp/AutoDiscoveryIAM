using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetPermissions;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Queries.GetPermissions;

public class GetPermissionsRepositoryTests
{
  [Fact]
  public async Task GetPermissions_ShouldReturnOnlyActivePermissionsOrderedByGroupThenKey()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var p2 = new Permission { Key = "users.write", Group = "Users", IsDeleted = false, IsActive = true };
    var p1 = new Permission { Key = "users.read", Group = "Users", IsDeleted = false, IsActive = true };
    var p3 = new Permission { Key = "roles.read", Group = "Roles", IsDeleted = false, IsActive = true };
    var pDeleted = new Permission { Key = "zz.deleted", Group = "Z", IsDeleted = false, IsActive = true };

    context.Permissions.AddRange(p2, p1, p3, pDeleted);
    await context.SaveChangesAsync(CancellationToken.None);

    pDeleted.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetPermissionsRepository(context);

    var (items, totalCount) = await repository.GetPermissions(1, 20);

    totalCount.Should().Be(3);
    items.Should().HaveCount(3);
    items[0].Group.Should().Be("Roles");
    items[0].Key.Should().Be("roles.read");
    items[1].Key.Should().Be("users.read");
    items[2].Key.Should().Be("users.write");
  }
}
