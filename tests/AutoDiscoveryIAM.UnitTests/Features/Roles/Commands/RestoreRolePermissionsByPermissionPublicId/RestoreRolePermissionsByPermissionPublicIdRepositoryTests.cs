using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.RestoreRolePermissionsByPermissionPublicId;

public class RestoreRolePermissionsByPermissionPublicIdRepositoryTests
{
  [Fact]
  public async Task GetDeletedRelationByPublicIdsAsync_ShouldReturnOnlyDeletedRelation()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var role = new Role { PublicId = Guid.NewGuid(), Name = "RoleA", IsActive = true, IsDeleted = false };
    var permission = new Permission { PublicId = Guid.NewGuid(), Key = "perm.restore", Group = "G", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    context.Permissions.Add(permission);
    await context.SaveChangesAsync(CancellationToken.None);

    var relation = new RolePermission
    {
      RoleId = role.Id,
      PermissionId = permission.Id,
      Role = role,
      Permission = permission,
      AssignedAt = DateTime.UtcNow,
      IsDeleted = false,
    };
    context.RolePermissions.Add(relation);
    await context.SaveChangesAsync(CancellationToken.None);

    relation.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RestoreRolePermissionsByPermissionPublicIdRepository(context);

    var result = await repository.GetDeletedRelationByPublicIdsAsync(role.PublicId, permission.PublicId, CancellationToken.None);

    result.Should().NotBeNull();
    result!.IsDeleted.Should().BeTrue();
    result.Role.PublicId.Should().Be(role.PublicId);
    result.Permission.PublicId.Should().Be(permission.PublicId);
  }

  [Fact]
  public async Task GetDeletedRelationByPublicIdsAsync_ShouldReturnNullForActiveRelation()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var role = new Role { PublicId = Guid.NewGuid(), Name = "RoleB", IsActive = true, IsDeleted = false };
    var permission = new Permission { PublicId = Guid.NewGuid(), Key = "perm.active", Group = "G", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    context.Permissions.Add(permission);
    await context.SaveChangesAsync(CancellationToken.None);

    context.RolePermissions.Add(new RolePermission
    {
      RoleId = role.Id,
      PermissionId = permission.Id,
      Role = role,
      Permission = permission,
      AssignedAt = DateTime.UtcNow,
      IsDeleted = false,
    });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RestoreRolePermissionsByPermissionPublicIdRepository(context);

    var result = await repository.GetDeletedRelationByPublicIdsAsync(role.PublicId, permission.PublicId, CancellationToken.None);

    result.Should().BeNull();
  }
}
