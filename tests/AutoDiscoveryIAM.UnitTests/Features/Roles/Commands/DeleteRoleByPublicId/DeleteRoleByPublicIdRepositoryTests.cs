using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.DeleteRoleByPublicId;

public class DeleteRoleByPublicIdRepositoryTests
{
  [Fact]
  public async Task GetRoleByPublicIdAsync_ShouldReturnNullWhenDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var role = new Role { PublicId = Guid.NewGuid(), Name = "DeletedRole", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    role.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new DeleteRoleByPublicIdRepository(context);

    var result = await repository.GetRoleByPublicIdAsync(role.PublicId, CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task HasActiveUsersAsync_ShouldReturnTrueWhenRelationExists()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var role = new Role { Name = "RoleA", IsActive = true, IsDeleted = false };
    var user = new User { Username = "user1", Email = "u1@test.com", PasswordHash = "hash", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    context.UserRoles.Add(new UserRole { RoleId = role.Id, UserId = user.Id, Role = role, User = user, IsDeleted = false, AssignedAt = DateTime.UtcNow });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new DeleteRoleByPublicIdRepository(context);

    var result = await repository.HasActiveUsersAsync(role.Id, CancellationToken.None);

    result.Should().BeTrue();
  }

  [Fact]
  public async Task DeleteRoleAsync_ShouldMarkEntityStateAsDeletedAndReturnTrue()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var role = new Role { Name = "RoleDelete", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new DeleteRoleByPublicIdRepository(context);

    var result = await repository.DeleteRoleAsync(role, CancellationToken.None);

    result.Should().BeTrue();
    context.Entry(role).State.Should().Be(EntityState.Deleted);
  }
}
