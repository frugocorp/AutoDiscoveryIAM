using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Queries.GetRoleByPublicId;

public class GetRoleByPublicIdRepositoryTests
{
  [Fact]
  public async Task GetRoleByPublicId_ShouldReturnRoleWhenActiveAndNotDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var role = new Role { PublicId = Guid.NewGuid(), Name = "role.active", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetRoleByPublicIdRepository(context);

    var result = await repository.GetRoleByPublicId(role.PublicId);

    result.Should().NotBeNull();
    result!.PublicId.Should().Be(role.PublicId);
  }

  [Fact]
  public async Task GetRoleByPublicId_ShouldReturnNullWhenRoleIsDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var role = new Role { PublicId = Guid.NewGuid(), Name = "role.deleted", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    role.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetRoleByPublicIdRepository(context);

    var result = await repository.GetRoleByPublicId(role.PublicId);

    result.Should().BeNull();
  }
}
