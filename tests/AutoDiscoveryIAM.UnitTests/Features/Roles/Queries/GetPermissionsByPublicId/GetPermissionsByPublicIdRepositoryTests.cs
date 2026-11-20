using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Queries.GetPermissionsByPublicId;

public class GetPermissionsByPublicIdRepositoryTests
{
  [Fact]
  public async Task RoleExistsAsync_ShouldReturnTrueEvenIfRoleIsDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var rolePublicId = Guid.NewGuid();
    var role = new Role
    {
      PublicId = rolePublicId,
      Name = "RoleDeleted",
      IsDeleted = false,
      IsActive = false,
    };

    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    role.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetPermissionsByPublicIdRepository(context);

    var exists = await repository.RoleExistsAsync(rolePublicId, CancellationToken.None);

    exists.Should().BeTrue();
  }

  [Fact]
  public async Task RoleIsActiveAsync_ShouldReturnFalseWhenRoleIsInactive()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var rolePublicId = Guid.NewGuid();

    context.Roles.Add(new Role
    {
      PublicId = rolePublicId,
      Name = "RoleInactive",
      IsDeleted = false,
      IsActive = false,
    });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetPermissionsByPublicIdRepository(context);

    var isActive = await repository.RoleIsActiveAsync(rolePublicId, CancellationToken.None);

    isActive.Should().BeFalse();
  }

  [Fact]
  public async Task GetPermissionsAsync_ShouldReturnOnlyValidRelationsOrderedByGroupThenKey()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var role = new Role
    {
      PublicId = Guid.NewGuid(),
      Name = "Role1",
      IsDeleted = false,
      IsActive = true,
    };

    var permissionUsersWrite = new Permission { PublicId = Guid.NewGuid(), Key = "users.write", Group = "Users", IsActive = true, IsDeleted = false };
    var permissionUsersRead = new Permission { PublicId = Guid.NewGuid(), Key = "users.read", Group = "Users", IsActive = true, IsDeleted = false };
    var permissionInactive = new Permission { PublicId = Guid.NewGuid(), Key = "roles.hidden", Group = "Roles", IsActive = false, IsDeleted = false };

    context.Roles.Add(role);
    context.Permissions.AddRange(permissionUsersWrite, permissionUsersRead, permissionInactive);
    await context.SaveChangesAsync(CancellationToken.None);

    context.RolePermissions.AddRange(
      new RolePermission { RoleId = role.Id, PermissionId = permissionUsersWrite.Id, Role = role, Permission = permissionUsersWrite, IsDeleted = false, AssignedAt = DateTime.UtcNow.AddMinutes(-2) },
      new RolePermission { RoleId = role.Id, PermissionId = permissionUsersRead.Id, Role = role, Permission = permissionUsersRead, IsDeleted = false, AssignedAt = DateTime.UtcNow.AddMinutes(-1) },
      new RolePermission { RoleId = role.Id, PermissionId = permissionInactive.Id, Role = role, Permission = permissionInactive, IsDeleted = false, AssignedAt = DateTime.UtcNow });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetPermissionsByPublicIdRepository(context);

    var result = await repository.GetPermissionsAsync(role.PublicId, CancellationToken.None);

    result.Should().HaveCount(2);
    result[0].Key.Should().Be("users.read");
    result[1].Key.Should().Be("users.write");
  }
}
