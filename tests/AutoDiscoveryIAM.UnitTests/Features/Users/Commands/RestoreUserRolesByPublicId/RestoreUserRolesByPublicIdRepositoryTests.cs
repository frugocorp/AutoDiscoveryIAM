using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RestoreUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.RestoreUserRolesByPublicId;

public class RestoreUserRolesByPublicIdRepositoryTests
{
  [Fact]
  public async Task GetDeletedRelationByPublicIdsAsync_ShouldReturnOnlyDeletedRelation()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var user = new User
    {
      PublicId = Guid.NewGuid(),
      Username = "user.d",
      Email = "user.d@test.local",
      PasswordHash = "hash",
      IsDeleted = false,
    };
    var role = new Role { PublicId = Guid.NewGuid(), Name = "RoleD", IsActive = true, IsDeleted = false };
    context.Users.Add(user);
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    var relation = new UserRole
    {
      UserId = user.Id,
      RoleId = role.Id,
      User = user,
      Role = role,
      AssignedAt = DateTime.UtcNow,
      IsDeleted = false,
    };
    context.UserRoles.Add(relation);
    await context.SaveChangesAsync(CancellationToken.None);

    relation.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RestoreUserRolesByPublicIdRepository(context);

    var result = await repository.GetDeletedRelationByPublicIdsAsync(user.PublicId, role.PublicId, CancellationToken.None);

    result.Should().NotBeNull();
    result!.IsDeleted.Should().BeTrue();
    result.User.PublicId.Should().Be(user.PublicId);
    result.Role.PublicId.Should().Be(role.PublicId);
  }

  [Fact]
  public async Task GetDeletedRelationByPublicIdsAsync_ShouldReturnNullForActiveRelation()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var user = new User
    {
      PublicId = Guid.NewGuid(),
      Username = "user.e",
      Email = "user.e@test.local",
      PasswordHash = "hash",
      IsDeleted = false,
    };
    var role = new Role { PublicId = Guid.NewGuid(), Name = "RoleE", IsActive = true, IsDeleted = false };
    context.Users.Add(user);
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    context.UserRoles.Add(new UserRole
    {
      UserId = user.Id,
      RoleId = role.Id,
      User = user,
      Role = role,
      AssignedAt = DateTime.UtcNow,
      IsDeleted = false,
    });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new RestoreUserRolesByPublicIdRepository(context);

    var result = await repository.GetDeletedRelationByPublicIdsAsync(user.PublicId, role.PublicId, CancellationToken.None);

    result.Should().BeNull();
  }
}
