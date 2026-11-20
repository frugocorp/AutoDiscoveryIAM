using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Queries.GetUsersByPublicId;

public class GetUsersByPublicIdRepositoryTests
{
  [Fact]
  public async Task RoleExistsAsync_ShouldReturnTrueEvenIfDeleted()
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

    var repository = new GetUsersByPublicIdRepository(context);

    var result = await repository.RoleExistsAsync(role.PublicId, CancellationToken.None);

    result.Should().BeTrue();
  }

  [Fact]
  public async Task RoleIsActiveAsync_ShouldReturnFalseWhenInactive()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var role = new Role { PublicId = Guid.NewGuid(), Name = "RoleInactive", IsActive = false, IsDeleted = false };
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetUsersByPublicIdRepository(context);

    var result = await repository.RoleIsActiveAsync(role.PublicId, CancellationToken.None);

    result.Should().BeFalse();
  }

  [Fact]
  public async Task GetUsersAsync_ShouldReturnOnlyActiveUsersOrderedByNameAndUsername()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var role = new Role { PublicId = Guid.NewGuid(), Name = "RoleA", IsActive = true, IsDeleted = false };
    var userB = new User { PublicId = Guid.NewGuid(), Username = "zuser", Email = "z@test.com", PasswordHash = "hash", IsActive = true, IsDeleted = false };
    var userA = new User { PublicId = Guid.NewGuid(), Username = "auser", Email = "a@test.com", PasswordHash = "hash", IsActive = true, IsDeleted = false };
    var userInactive = new User { PublicId = Guid.NewGuid(), Username = "inactive", Email = "i@test.com", PasswordHash = "hash", IsActive = false, IsDeleted = false };

    context.Roles.Add(role);
    context.Users.AddRange(userB, userA, userInactive);
    await context.SaveChangesAsync(CancellationToken.None);

    context.UserRoles.AddRange(
      new UserRole { UserId = userB.Id, RoleId = role.Id, User = userB, Role = role, AssignedAt = DateTime.UtcNow.AddMinutes(-2), IsDeleted = false },
      new UserRole { UserId = userA.Id, RoleId = role.Id, User = userA, Role = role, AssignedAt = DateTime.UtcNow.AddMinutes(-1), IsDeleted = false },
      new UserRole { UserId = userInactive.Id, RoleId = role.Id, User = userInactive, Role = role, AssignedAt = DateTime.UtcNow, IsDeleted = false });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetUsersByPublicIdRepository(context);

    var result = await repository.GetUsersAsync(role.PublicId, 1, 20, CancellationToken.None);

    result.Should().HaveCount(2);
    result[0].Username.Should().Be("auser");
    result[1].Username.Should().Be("zuser");
  }
}
