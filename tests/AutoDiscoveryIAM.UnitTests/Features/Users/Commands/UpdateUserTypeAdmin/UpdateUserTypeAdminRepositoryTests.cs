using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateUserTypeAdmin;

public class UpdateUserTypeAdminRepositoryTests
{
  [Fact]
  public async Task GetByPublicIdAsync_ShouldReturnNullWhenDeleted()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var user = new User
    {
      PublicId = Guid.NewGuid(),
      Username = "deleted-admin",
      Email = "deleted-admin@test.com",
      PasswordHash = "hash",
      IsDeleted = false,
    };

    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    user.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new UpdateUserTypeAdminRepository(context);

    var result = await repository.GetByPublicIdAsync(user.PublicId, CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task UpdateAsync_ShouldTrackUserAsModified()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var user = new User
    {
      PublicId = Guid.NewGuid(),
      Username = "admin-user",
      Email = "admin-user@test.com",
      PasswordHash = "hash",
      IsDeleted = false,
      IsAdmin = false,
    };

    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    user.IsAdmin = true;

    var repository = new UpdateUserTypeAdminRepository(context);

    await repository.UpdateAsync(user, CancellationToken.None);

    context.Entry(user).State.Should().Be(EntityState.Modified);
  }
}
