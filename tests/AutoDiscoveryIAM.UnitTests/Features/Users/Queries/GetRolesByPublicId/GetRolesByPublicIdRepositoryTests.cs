using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetRolesByPublicId;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Queries.GetRolesByPublicId;

public class GetRolesByPublicIdRepositoryTests
{
  [Fact]
  public async Task UserExistsAsync_ShouldReturnTrueEvenIfUserIsDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var userPublicId = Guid.NewGuid();
    var user = new User
    {
      PublicId = userPublicId,
      Username = "deleted-user",
      Email = "deleted@test.com",
      PasswordHash = "hash",
      IsDeleted = false,
      IsActive = false,
    };

    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    user.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetRolesByPublicIdRepository(context);

    var exists = await repository.UserExistsAsync(userPublicId, CancellationToken.None);

    exists.Should().BeTrue();
  }

  [Fact]
  public async Task UserIsActiveAsync_ShouldReturnFalseWhenUserIsInactive()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var userPublicId = Guid.NewGuid();

    context.Users.Add(new User
    {
      PublicId = userPublicId,
      Username = "inactive-user",
      Email = "inactive@test.com",
      PasswordHash = "hash",
      IsDeleted = false,
      IsActive = false,
    });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetRolesByPublicIdRepository(context);

    var isActive = await repository.UserIsActiveAsync(userPublicId, CancellationToken.None);

    isActive.Should().BeFalse();
  }

  [Fact]
  public async Task GetRolesAsync_ShouldReturnOnlyValidRelationsOrderedByRoleName()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var user = new User
    {
      PublicId = Guid.NewGuid(),
      Username = "user1",
      Email = "user1@test.com",
      PasswordHash = "hash",
      IsDeleted = false,
      IsActive = true,
    };

    var roleB = new Role { PublicId = Guid.NewGuid(), Name = "Beta", IsActive = true, IsDeleted = false };
    var roleA = new Role { PublicId = Guid.NewGuid(), Name = "Alpha", IsActive = true, IsDeleted = false };
    var roleInactive = new Role { PublicId = Guid.NewGuid(), Name = "Inactive", IsActive = false, IsDeleted = false };

    context.Users.Add(user);
    context.Roles.AddRange(roleB, roleA, roleInactive);
    await context.SaveChangesAsync(CancellationToken.None);

    context.UserRoles.AddRange(
      new UserRole { UserId = user.Id, RoleId = roleB.Id, User = user, Role = roleB, IsDeleted = false, AssignedAt = DateTime.UtcNow.AddMinutes(-2) },
      new UserRole { UserId = user.Id, RoleId = roleA.Id, User = user, Role = roleA, IsDeleted = false, AssignedAt = DateTime.UtcNow.AddMinutes(-1) },
      new UserRole { UserId = user.Id, RoleId = roleInactive.Id, User = user, Role = roleInactive, IsDeleted = false, AssignedAt = DateTime.UtcNow });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetRolesByPublicIdRepository(context);

    var result = await repository.GetRolesAsync(user.PublicId, CancellationToken.None);

    result.Should().HaveCount(2);
    result[0].Name.Should().Be("Alpha");
    result[1].Name.Should().Be("Beta");
  }
}
