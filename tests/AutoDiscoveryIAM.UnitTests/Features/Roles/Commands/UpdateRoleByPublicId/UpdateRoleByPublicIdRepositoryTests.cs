using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.UpdateRoleByPublicId;

public class UpdateRoleByPublicIdRepositoryTests
{
  [Fact]
  public async Task GetByNameAsync_ShouldIgnoreDeletedRoles()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var activeRole = new Role { Name = "support", IsActive = true, IsDeleted = false };
    var deletedRole = new Role { Name = "support", IsActive = true, IsDeleted = false };
    context.Roles.Add(activeRole);
    context.Roles.Add(deletedRole);
    await context.SaveChangesAsync(CancellationToken.None);

    deletedRole.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new UpdateRoleByPublicIdRepository(context);

    var result = await repository.GetByNameAsync("support", CancellationToken.None);

    result.Should().NotBeNull();
    result!.Id.Should().Be(activeRole.Id);
  }

  [Fact]
  public async Task GetByPublicIdAsync_ShouldReturnNullWhenDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var role = new Role { PublicId = Guid.NewGuid(), Name = "role.to-delete", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    role.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new UpdateRoleByPublicIdRepository(context);

    var result = await repository.GetByPublicIdAsync(role.PublicId, CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task UpdateAsync_ShouldTrackEntityAsModified()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var role = new Role { Name = "editor", Description = "before", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    role.Description = "after";

    var repository = new UpdateRoleByPublicIdRepository(context);

    var updated = await repository.UpdateAsync(role, CancellationToken.None);

    updated.Description.Should().Be("after");
    context.Entry(role).State.Should().Be(EntityState.Modified);
  }
}
