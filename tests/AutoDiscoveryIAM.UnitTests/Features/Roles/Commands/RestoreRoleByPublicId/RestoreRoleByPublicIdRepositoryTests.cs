using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.RestoreRoleByPublicId;

public class RestoreRoleByPublicIdRepositoryTests
{
  [Fact]
  public async Task GetDeletedRoleByPublicIdAsync_ShouldReturnDeletedRole()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var role = new Role { PublicId = Guid.NewGuid(), Name = "RoleDeleted", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    role.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RestoreRoleByPublicIdRepository(context);

    var result = await repository.GetDeletedRoleByPublicIdAsync(role.PublicId, CancellationToken.None);

    result.Should().NotBeNull();
    result!.PublicId.Should().Be(role.PublicId);
  }

  [Fact]
  public async Task ExistsActiveRoleWithNameAsync_ShouldReturnTrueWhenActiveRoleExists()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    context.Roles.Add(new Role { Name = "Admin", IsActive = true, IsDeleted = false });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RestoreRoleByPublicIdRepository(context);

    var exists = await repository.ExistsActiveRoleWithNameAsync("Admin", CancellationToken.None);

    exists.Should().BeTrue();
  }

  [Fact]
  public async Task RestoreRoleAsync_ShouldSetIsDeletedFalseAndReturnTrue()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var role = new Role { Name = "RestoreMe", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    role.IsDeleted = true;

    var repository = new RestoreRoleByPublicIdRepository(context);

    var result = await repository.RestoreRoleAsync(role, CancellationToken.None);

    result.Should().BeTrue();
    role.IsDeleted.Should().BeFalse();
  }
}
