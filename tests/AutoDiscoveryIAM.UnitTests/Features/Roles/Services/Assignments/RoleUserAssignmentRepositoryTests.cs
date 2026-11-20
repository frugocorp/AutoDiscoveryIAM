using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.Assignments;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Services.Assignments;

public class RoleUserAssignmentRepositoryTests
{
  [Fact]
  public async Task GetActiveUserByPublicIdAsync_ShouldReturnOnlyActiveNotDeletedUser()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var activeUser = new User
    {
      PublicId = Guid.NewGuid(),
      Username = "active-user",
      Email = "active-user@test.local",
      PasswordHash = "hash",
      IsActive = true,
      IsDeleted = false,
    };
    var deletedUser = new User
    {
      PublicId = Guid.NewGuid(),
      Username = "deleted-user",
      Email = "deleted-user@test.local",
      PasswordHash = "hash",
      IsActive = true,
      IsDeleted = false,
    };
    context.Users.Add(activeUser);
    context.Users.Add(deletedUser);
    await context.SaveChangesAsync(CancellationToken.None);

    deletedUser.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RoleUserAssignmentRepository(context);

    var result = await repository.GetActiveUserByPublicIdAsync(activeUser.PublicId, CancellationToken.None);
    var deletedResult = await repository.GetActiveUserByPublicIdAsync(deletedUser.PublicId, CancellationToken.None);

    result.Should().NotBeNull();
    result!.Id.Should().Be(activeUser.Id);
    deletedResult.Should().BeNull();
  }

  [Fact]
  public async Task GetActiveRolesByPublicIdsAsync_ShouldReturnOnlyActiveNotDeletedRoles()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var roleA = new Role { PublicId = Guid.NewGuid(), Name = "roleA", IsActive = true, IsDeleted = false };
    var roleB = new Role { PublicId = Guid.NewGuid(), Name = "roleB", IsActive = false, IsDeleted = false };
    context.Roles.Add(roleA);
    context.Roles.Add(roleB);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RoleUserAssignmentRepository(context);

    var result = await repository.GetActiveRolesByPublicIdsAsync([roleA.PublicId, roleB.PublicId], CancellationToken.None);

    result.Should().ContainSingle();
    result[0].Id.Should().Be(roleA.Id);
  }

  [Fact]
  public async Task GetVisibleRelationsByUserIdAsync_ShouldReturnOnlyNonDeletedWithActiveRole()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var user = new User { Username = "u1", Email = "u1@test.local", PasswordHash = "hash", IsActive = true, IsDeleted = false };
    var activeRole = new Role { Name = "r1", IsActive = true, IsDeleted = false };
    var inactiveRole = new Role { Name = "r2", IsActive = false, IsDeleted = false };
    context.Users.Add(user);
    context.Roles.AddRange(activeRole, inactiveRole);
    await context.SaveChangesAsync(CancellationToken.None);

    var activeVisibleRelation = new UserRole { UserId = user.Id, RoleId = activeRole.Id, User = user, Role = activeRole, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var inactiveRoleRelation = new UserRole { UserId = user.Id, RoleId = inactiveRole.Id, User = user, Role = inactiveRole, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var deletedRelation = new UserRole { UserId = user.Id, RoleId = activeRole.Id, User = user, Role = activeRole, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    context.UserRoles.AddRange(activeVisibleRelation, inactiveRoleRelation, deletedRelation);
    await context.SaveChangesAsync(CancellationToken.None);

    deletedRelation.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RoleUserAssignmentRepository(context);

    var result = await repository.GetVisibleRelationsByUserIdAsync(user.Id, CancellationToken.None);

    result.Should().ContainSingle();
    result[0].RoleId.Should().Be(activeRole.Id);
  }

  [Fact]
  public async Task GetDeletedRelationsByRoleIdAndUserIdsAsync_ShouldReturnOnlyDeletedWithActiveUser()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var role = new Role { Name = "role.deleted-filter", IsActive = true, IsDeleted = false };
    var activeUser = new User { Username = "u2", Email = "u2@test.local", PasswordHash = "hash", IsActive = true, IsDeleted = false };
    var inactiveUser = new User { Username = "u3", Email = "u3@test.local", PasswordHash = "hash", IsActive = false, IsDeleted = false };
    context.Roles.Add(role);
    context.Users.AddRange(activeUser, inactiveUser);
    await context.SaveChangesAsync(CancellationToken.None);

    var deletedActiveUserRelation = new UserRole { RoleId = role.Id, UserId = activeUser.Id, Role = role, User = activeUser, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var deletedInactiveUserRelation = new UserRole { RoleId = role.Id, UserId = inactiveUser.Id, Role = role, User = inactiveUser, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var activeRelation = new UserRole { RoleId = role.Id, UserId = activeUser.Id, Role = role, User = activeUser, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    context.UserRoles.AddRange(deletedActiveUserRelation, deletedInactiveUserRelation, activeRelation);
    await context.SaveChangesAsync(CancellationToken.None);

    deletedActiveUserRelation.IsDeleted = true;
    deletedInactiveUserRelation.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RoleUserAssignmentRepository(context);

    var result = await repository.GetDeletedRelationsByRoleIdAndUserIdsAsync(role.Id, [activeUser.Id, inactiveUser.Id], CancellationToken.None);

    result.Should().ContainSingle();
    result[0].UserId.Should().Be(activeUser.Id);
    result[0].IsDeleted.Should().BeTrue();
  }
}
