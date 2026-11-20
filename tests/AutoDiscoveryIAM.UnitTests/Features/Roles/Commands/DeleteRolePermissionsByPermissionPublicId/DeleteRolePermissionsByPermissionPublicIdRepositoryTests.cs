using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;

public class DeleteRolePermissionsByPermissionPublicIdRepositoryTests
{
  [Fact]
  public async Task GetRelationByPublicIdsAsync_ShouldReturnRelationIncludingRoleAndPermission()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var role = new Role { PublicId = Guid.NewGuid(), Name = "RoleA", IsActive = true, IsDeleted = false };
    var permission = new Permission { PublicId = Guid.NewGuid(), Key = "perm.a", Group = "G", IsActive = true, IsDeleted = false };
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

    var repository = new DeleteRolePermissionsByPermissionPublicIdRepository(context);

    var result = await repository.GetRelationByPublicIdsAsync(role.PublicId, permission.PublicId, CancellationToken.None);

    result.Should().NotBeNull();
    result!.Role.PublicId.Should().Be(role.PublicId);
    result.Permission.PublicId.Should().Be(permission.PublicId);
  }

  [Fact]
  public async Task GetRelationByPublicIdsAsync_ShouldReturnNullWhenNotFound()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var repository = new DeleteRolePermissionsByPermissionPublicIdRepository(context);

    var result = await repository.GetRelationByPublicIdsAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

    result.Should().BeNull();
  }
}
