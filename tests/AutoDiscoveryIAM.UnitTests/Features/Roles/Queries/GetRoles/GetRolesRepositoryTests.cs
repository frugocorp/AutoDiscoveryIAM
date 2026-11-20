using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoles;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Queries.GetRoles;

public class GetRolesRepositoryTests
{
  [Fact]
  public async Task GetRoles_ShouldReturnOnlyActiveRolesOrderedByCreatedAtDesc()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var oldDate = DateTime.UtcNow.AddMinutes(-20);
    var newDate = DateTime.UtcNow;

    var roleOld = new Role { Name = "Old", CreatedAt = oldDate, IsDeleted = false, IsActive = true };
    var roleNew = new Role { Name = "New", CreatedAt = newDate, IsDeleted = false, IsActive = true };
    var roleDeleted = new Role { Name = "Deleted", CreatedAt = DateTime.UtcNow.AddMinutes(-5), IsDeleted = false, IsActive = true };

    context.Roles.AddRange(roleOld, roleNew, roleDeleted);
    await context.SaveChangesAsync(CancellationToken.None);

    roleDeleted.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetRolesRepository(context);

    var (items, totalCount) = await repository.GetRoles(1, 20);

    totalCount.Should().Be(2);
    items.Should().HaveCount(2);
    items[0].Name.Should().Be("New");
    items[1].Name.Should().Be("Old");
  }
}
