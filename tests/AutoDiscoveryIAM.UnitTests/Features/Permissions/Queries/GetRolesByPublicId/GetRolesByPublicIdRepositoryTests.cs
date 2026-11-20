using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Queries.GetRolesByPublicId;

public class GetRolesByPublicIdRepositoryTests
{
  [Fact]
  public async Task PermissionExistsAsync_ShouldReturnTrueEvenIfDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var permission = new Permission { PublicId = Guid.NewGuid(), Key = "perm.exists", Group = "G", IsActive = false, IsDeleted = false };
    context.Permissions.Add(permission);
    await context.SaveChangesAsync(CancellationToken.None);

    permission.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetRolesByPublicIdRepository(context);

    var result = await repository.PermissionExistsAsync(permission.PublicId, CancellationToken.None);

    result.Should().BeTrue();
  }

  [Fact]
  public async Task PermissionIsActiveAsync_ShouldReturnFalseWhenInactive()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var permission = new Permission { PublicId = Guid.NewGuid(), Key = "perm.inactive", Group = "G", IsActive = false, IsDeleted = false };
    context.Permissions.Add(permission);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetRolesByPublicIdRepository(context);

    var result = await repository.PermissionIsActiveAsync(permission.PublicId, CancellationToken.None);

    result.Should().BeFalse();
  }

  [Fact]
  public async Task GetRolesAsync_ShouldReturnOnlyActiveRelationsOrderedByRoleName()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var permission = new Permission { PublicId = Guid.NewGuid(), Key = "perm.valid", Group = "Users", IsActive = true, IsDeleted = false };
    var roleB = new Role { PublicId = Guid.NewGuid(), Name = "Beta", IsActive = true, IsDeleted = false };
    var roleA = new Role { PublicId = Guid.NewGuid(), Name = "Alpha", IsActive = true, IsDeleted = false };
    var roleInactive = new Role { PublicId = Guid.NewGuid(), Name = "Inactive", IsActive = false, IsDeleted = false };

    context.Permissions.Add(permission);
    context.Roles.AddRange(roleB, roleA, roleInactive);
    await context.SaveChangesAsync(CancellationToken.None);

    context.RolePermissions.AddRange(
      new RolePermission { RoleId = roleB.Id, PermissionId = permission.Id, Role = roleB, Permission = permission, AssignedAt = DateTime.UtcNow.AddMinutes(-2), IsDeleted = false },
      new RolePermission { RoleId = roleA.Id, PermissionId = permission.Id, Role = roleA, Permission = permission, AssignedAt = DateTime.UtcNow.AddMinutes(-1), IsDeleted = false },
      new RolePermission { RoleId = roleInactive.Id, PermissionId = permission.Id, Role = roleInactive, Permission = permission, AssignedAt = DateTime.UtcNow, IsDeleted = false });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetRolesByPublicIdRepository(context);

    var result = await repository.GetRolesAsync(permission.PublicId, CancellationToken.None);

    result.Should().HaveCount(2);
    result[0].Name.Should().Be("Alpha");
    result[1].Name.Should().Be("Beta");
  }
}
