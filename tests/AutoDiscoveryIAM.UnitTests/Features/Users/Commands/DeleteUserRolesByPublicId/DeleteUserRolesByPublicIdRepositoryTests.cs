using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.DeleteUserRolesByPublicId;

public class DeleteUserRolesByPublicIdRepositoryTests
{
  [Fact]
  public async Task GetRelationByPublicIdsAsync_ShouldReturnRelationIncludingUserAndRole()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);

    var user = new User
    {
      PublicId = Guid.NewGuid(),
      Username = "user.c",
      Email = "user.c@test.local",
      PasswordHash = "hash",
      IsDeleted = false,
    };
    var role = new Role { PublicId = Guid.NewGuid(), Name = "RoleC", IsActive = true, IsDeleted = false };
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

    var repository = new DeleteUserRolesByPublicIdRepository(context);

    var result = await repository.GetRelationByPublicIdsAsync(user.PublicId, role.PublicId, CancellationToken.None);

    result.Should().NotBeNull();
    result!.User.PublicId.Should().Be(user.PublicId);
    result.Role.PublicId.Should().Be(role.PublicId);
  }

  [Fact]
  public async Task GetRelationByPublicIdsAsync_ShouldReturnNullWhenNotFound()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var repository = new DeleteUserRolesByPublicIdRepository(context);

    var result = await repository.GetRelationByPublicIdsAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

    result.Should().BeNull();
  }
}
