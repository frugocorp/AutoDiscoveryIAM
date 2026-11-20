using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.CreateRolePermissions;

public class CreateRolePermissionsRepositoryTests
{
  [Fact]
  public async Task GetRoleByPublicIdAsync_ShouldReturnRoleEvenIfDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var role = new Role { PublicId = Guid.NewGuid(), Name = "RoleDeleted", IsActive = false, IsDeleted = false };
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    role.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new CreateRolePermissionsRepository(context);

    var result = await repository.GetRoleByPublicIdAsync(role.PublicId, CancellationToken.None);

    result.Should().NotBeNull();
    result!.PublicId.Should().Be(role.PublicId);
  }

  [Fact]
  public async Task GetRelationAsync_ShouldReturnRelationFromIgnoreQueryFilters()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var role = new Role { Name = "RoleA", IsActive = true, IsDeleted = false };
    var permission = new Permission { Key = "perm.a", Group = "G", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    context.Permissions.Add(permission);
    await context.SaveChangesAsync(CancellationToken.None);

    var relation = new RolePermission { RoleId = role.Id, PermissionId = permission.Id, Role = role, Permission = permission, IsDeleted = false, AssignedAt = DateTime.UtcNow };
    context.RolePermissions.Add(relation);
    await context.SaveChangesAsync(CancellationToken.None);

    relation.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new CreateRolePermissionsRepository(context);

    var result = await repository.GetRelationAsync(role.Id, permission.Id, CancellationToken.None);

    result.Should().NotBeNull();
    result!.RoleId.Should().Be(role.Id);
    result.PermissionId.Should().Be(permission.Id);
  }

  [Fact]
  public async Task AddRelationAsync_ShouldTrackRelationInContext()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var repository = new CreateRolePermissionsRepository(context);

    var role = new Role { Name = "RoleB", IsActive = true, IsDeleted = false };
    var permission = new Permission { Key = "perm.b", Group = "G", IsActive = true, IsDeleted = false };
    context.Roles.Add(role);
    context.Permissions.Add(permission);
    await context.SaveChangesAsync(CancellationToken.None);

    var relation = new RolePermission { RoleId = role.Id, PermissionId = permission.Id, Role = role, Permission = permission, IsDeleted = false, AssignedAt = DateTime.UtcNow };

    await repository.AddRelationAsync(relation, CancellationToken.None);

    context.RolePermissions.Local.Should().ContainSingle();
    context.RolePermissions.Local.Single().RoleId.Should().Be(role.Id);
  }
}
