using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.CreateUserRoles;

public class CreateUserRolesRepositoryTests
{
  [Fact]
  public async Task GetUserByPublicIdAsync_ShouldReturnUserEvenIfDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var user = new User
    {
      PublicId = Guid.NewGuid(),
      Username = "user.deleted",
      Email = "user.deleted@test.local",
      PasswordHash = "hash",
      IsDeleted = false,
    };
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    user.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new CreateUserRolesRepository(context);

    var result = await repository.GetUserByPublicIdAsync(user.PublicId, CancellationToken.None);

    result.Should().NotBeNull();
    result!.PublicId.Should().Be(user.PublicId);
  }

  [Fact]
  public async Task GetRelationAsync_ShouldReturnRelationFromIgnoreQueryFilters()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var user = new User { Username = "user.a", Email = "user.a@test.local", PasswordHash = "hash", IsDeleted = false };
    var role = new Role { Name = "RoleA", IsActive = true, IsDeleted = false };
    context.Users.Add(user);
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    var relation = new UserRole { UserId = user.Id, RoleId = role.Id, User = user, Role = role, AssignedAt = DateTime.UtcNow, IsDeleted = false };
    context.UserRoles.Add(relation);
    await context.SaveChangesAsync(CancellationToken.None);

    relation.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new CreateUserRolesRepository(context);

    var result = await repository.GetRelationAsync(user.Id, role.Id, CancellationToken.None);

    result.Should().NotBeNull();
    result!.UserId.Should().Be(user.Id);
    result.RoleId.Should().Be(role.Id);
  }

  [Fact]
  public async Task AddRelationAsync_ShouldTrackRelationInContext()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var repository = new CreateUserRolesRepository(context);

    var user = new User { Username = "user.b", Email = "user.b@test.local", PasswordHash = "hash", IsDeleted = false };
    var role = new Role { Name = "RoleB", IsActive = true, IsDeleted = false };
    context.Users.Add(user);
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);

    var relation = new UserRole { UserId = user.Id, RoleId = role.Id, User = user, Role = role, AssignedAt = DateTime.UtcNow, IsDeleted = false };

    await repository.AddRelationAsync(relation, CancellationToken.None);

    context.UserRoles.Local.Should().ContainSingle();
    context.UserRoles.Local.Single().UserId.Should().Be(user.Id);
  }
}
