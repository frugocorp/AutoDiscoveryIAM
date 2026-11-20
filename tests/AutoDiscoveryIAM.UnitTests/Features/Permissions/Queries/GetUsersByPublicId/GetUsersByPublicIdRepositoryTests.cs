using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetUsersByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Queries.GetUsersByPublicId;

public class GetUsersByPublicIdRepositoryTests
{
  [Fact]
  public async Task PermissionExistsAsync_ShouldReturnTrueEvenIfDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var permission = new Permission { PublicId = Guid.NewGuid(), Key = "perm.deleted", Group = "G", IsActive = false, IsDeleted = false };
    context.Permissions.Add(permission);
    await context.SaveChangesAsync(CancellationToken.None);

    permission.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new GetUsersByPublicIdRepository(context);

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

    var repository = new GetUsersByPublicIdRepository(context);

    var result = await repository.PermissionIsActiveAsync(permission.PublicId, CancellationToken.None);

    result.Should().BeFalse();
  }

}
