using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.UpdateBulkRolePermissions;

public class UpdateBulkRolePermissionsRepositoryTests
{
  [Fact]
  public async Task GetActiveRoleByPublicIdAsync_ShouldReturnOnlyActiveNotDeletedRole()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var activeRole = new Role { PublicId = Guid.NewGuid(), Name = "role.active", IsActive = true, IsDeleted = false };
    var inactiveRole = new Role { PublicId = Guid.NewGuid(), Name = "role.inactive", IsActive = false, IsDeleted = false };
    context.Roles.AddRange(activeRole, inactiveRole);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new UpdateBulkRolePermissionsRepository(context);

    var activeResult = await repository.GetActiveRoleByPublicIdAsync(activeRole.PublicId, CancellationToken.None);
    var inactiveResult = await repository.GetActiveRoleByPublicIdAsync(inactiveRole.PublicId, CancellationToken.None);

    activeResult.Should().NotBeNull();
    inactiveResult.Should().BeNull();
  }

  [Fact]
  public async Task GetActivePermissionsByPublicIdsAsync_ShouldFilterInactiveOrDeletedPermissions()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var activePermission = new Permission { PublicId = Guid.NewGuid(), Key = "perm.active", Group = "G", IsActive = true, IsDeleted = false };
    var inactivePermission = new Permission { PublicId = Guid.NewGuid(), Key = "perm.inactive", Group = "G", IsActive = false, IsDeleted = false };
    context.Permissions.AddRange(activePermission, inactivePermission);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new UpdateBulkRolePermissionsRepository(context);

    var result = await repository.GetActivePermissionsByPublicIdsAsync([activePermission.PublicId, inactivePermission.PublicId], CancellationToken.None);

    result.Should().ContainSingle();
    result[0].Id.Should().Be(activePermission.Id);
  }

  [Fact]
  public async Task GetVisibleRelationsByRoleIdAsync_ShouldReturnOnlyNonDeletedWithActivePermission()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var role = new Role { Name = "role.visible", IsActive = true, IsDeleted = false };
    var activePermission = new Permission { Key = "perm.visible", Group = "G", IsActive = true, IsDeleted = false };
    var inactivePermission = new Permission { Key = "perm.inactive", Group = "G", IsActive = false, IsDeleted = false };
    context.Roles.Add(role);
    context.Permissions.AddRange(activePermission, inactivePermission);
    await context.SaveChangesAsync(CancellationToken.None);

    var visible = new RolePermission { RoleId = role.Id, PermissionId = activePermission.Id, Role = role, Permission = activePermission, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var hiddenByInactivePermission = new RolePermission { RoleId = role.Id, PermissionId = inactivePermission.Id, Role = role, Permission = inactivePermission, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var deletedRelation = new RolePermission { RoleId = role.Id, PermissionId = activePermission.Id, Role = role, Permission = activePermission, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    context.RolePermissions.AddRange(visible, hiddenByInactivePermission, deletedRelation);
    await context.SaveChangesAsync(CancellationToken.None);

    deletedRelation.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new UpdateBulkRolePermissionsRepository(context);

    var result = await repository.GetVisibleRelationsByRoleIdAsync(role.Id, CancellationToken.None);

    result.Should().ContainSingle();
    result[0].PermissionId.Should().Be(activePermission.Id);
  }

  [Fact]
  public async Task GetDeletedRelationsByRoleIdAndPermissionIdsAsync_ShouldReturnDeletedWithActivePermission()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var role = new Role { Name = "role.deleted", IsActive = true, IsDeleted = false };
    var activePermission = new Permission { Key = "perm.deleted.active", Group = "G", IsActive = true, IsDeleted = false };
    var inactivePermission = new Permission { Key = "perm.deleted.inactive", Group = "G", IsActive = false, IsDeleted = false };
    context.Roles.Add(role);
    context.Permissions.AddRange(activePermission, inactivePermission);
    await context.SaveChangesAsync(CancellationToken.None);

    var deletedWithActivePermission = new RolePermission { RoleId = role.Id, PermissionId = activePermission.Id, Role = role, Permission = activePermission, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var deletedWithInactivePermission = new RolePermission { RoleId = role.Id, PermissionId = inactivePermission.Id, Role = role, Permission = inactivePermission, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    var activeRelation = new RolePermission { RoleId = role.Id, PermissionId = activePermission.Id, Role = role, Permission = activePermission, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    context.RolePermissions.AddRange(deletedWithActivePermission, deletedWithInactivePermission, activeRelation);
    await context.SaveChangesAsync(CancellationToken.None);

    deletedWithActivePermission.IsDeleted = true;
    deletedWithInactivePermission.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new UpdateBulkRolePermissionsRepository(context);

    var result = await repository.GetDeletedRelationsByRoleIdAndPermissionIdsAsync(role.Id, [activePermission.Id, inactivePermission.Id], CancellationToken.None);

    result.Should().ContainSingle();
    result[0].PermissionId.Should().Be(activePermission.Id);
    result[0].IsDeleted.Should().BeTrue();
  }
}
