using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.ClearBulkRoleHiddenRelations;

public class ClearBulkRoleHiddenRelationsRepositoryTests
{
  [Fact]
  public async Task GetRoleByPublicIdAsync_ShouldReturnRoleEvenIfDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var role = new Role { PublicId = Guid.NewGuid(), Name = "role.hidden", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    role.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new ClearBulkRoleHiddenRelationsRepository(context);

    var result = await repository.GetRoleByPublicIdAsync(role.PublicId, CancellationToken.None);

    result.Should().NotBeNull();
    result!.Id.Should().Be(role.Id);
  }

  [Fact]
  public async Task GetHiddenUserRelationsAsync_ShouldReturnOnlyVisibleRelationsWithInactiveOrDeletedUsers()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var role = new Role { Name = "role.users", IsActive = true, IsDeleted = false };
    var inactiveUser = new User { Username = "u1", Email = "u1@test.local", PasswordHash = "hash", IsActive = false, IsDeleted = false };
    var activeUser = new User { Username = "u2", Email = "u2@test.local", PasswordHash = "hash", IsActive = true, IsDeleted = false };
    var deletedUser = new User { Username = "u3", Email = "u3@test.local", PasswordHash = "hash", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    context.Users.AddRange(inactiveUser, activeUser, deletedUser);
    await context.SaveChangesAsync(CancellationToken.None);

    var hiddenByInactive = new UserRole { RoleId = role.Id, UserId = inactiveUser.Id, Role = role, User = inactiveUser, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var visibleActive = new UserRole { RoleId = role.Id, UserId = activeUser.Id, Role = role, User = activeUser, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var hiddenByDeleted = new UserRole { RoleId = role.Id, UserId = deletedUser.Id, Role = role, User = deletedUser, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var relationDeleted = new UserRole { RoleId = role.Id, UserId = inactiveUser.Id, Role = role, User = inactiveUser, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    context.UserRoles.AddRange(hiddenByInactive, visibleActive, hiddenByDeleted, relationDeleted);
    await context.SaveChangesAsync(CancellationToken.None);

    deletedUser.IsDeleted = true;
    relationDeleted.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new ClearBulkRoleHiddenRelationsRepository(context);

    var result = await repository.GetHiddenUserRelationsAsync(role.Id, CancellationToken.None);

    result.Should().HaveCount(2);
    result.Select(x => x.UserId).Should().Contain(inactiveUser.Id);
    result.Select(x => x.UserId).Should().Contain(deletedUser.Id);
  }

  [Fact]
  public async Task GetHiddenPermissionRelationsAsync_ShouldReturnOnlyVisibleRelationsWithInactiveOrDeletedPermissions()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var role = new Role { Name = "role.perms", IsActive = true, IsDeleted = false };
    var inactivePermission = new Permission { Key = "perm.inactive", Group = "G", IsActive = false, IsDeleted = false };
    var activePermission = new Permission { Key = "perm.active", Group = "G", IsActive = true, IsDeleted = false };
    var deletedPermission = new Permission { Key = "perm.deleted", Group = "G", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    context.Permissions.AddRange(inactivePermission, activePermission, deletedPermission);
    await context.SaveChangesAsync(CancellationToken.None);

    var hiddenByInactive = new RolePermission { RoleId = role.Id, PermissionId = inactivePermission.Id, Role = role, Permission = inactivePermission, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var visibleActive = new RolePermission { RoleId = role.Id, PermissionId = activePermission.Id, Role = role, Permission = activePermission, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var hiddenByDeleted = new RolePermission { RoleId = role.Id, PermissionId = deletedPermission.Id, Role = role, Permission = deletedPermission, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var relationDeleted = new RolePermission { RoleId = role.Id, PermissionId = inactivePermission.Id, Role = role, Permission = inactivePermission, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    context.RolePermissions.AddRange(hiddenByInactive, visibleActive, hiddenByDeleted, relationDeleted);
    await context.SaveChangesAsync(CancellationToken.None);

    deletedPermission.IsDeleted = true;
    relationDeleted.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new ClearBulkRoleHiddenRelationsRepository(context);

    var result = await repository.GetHiddenPermissionRelationsAsync(role.Id, CancellationToken.None);

    result.Should().HaveCount(2);
    result.Select(x => x.PermissionId).Should().Contain(inactivePermission.Id);
    result.Select(x => x.PermissionId).Should().Contain(deletedPermission.Id);
  }
}
