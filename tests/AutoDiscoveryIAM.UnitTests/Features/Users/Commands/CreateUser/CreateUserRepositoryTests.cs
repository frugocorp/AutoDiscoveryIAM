using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.CreateUser;

public class CreateUserRepositoryTests
{
  [Fact]
  public async Task ExistsByEmailAsync_ShouldReturnTrueWhenEmailExists()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    context.Users.Add(new User
    {
      Username = "user1",
      Email = "user1@test.com",
      PasswordHash = "hash",
      IsDeleted = false,
    });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new CreateUserRepository(context);

    var exists = await repository.ExistsByEmailAsync("user1@test.com", CancellationToken.None);

    exists.Should().BeTrue();
  }

  [Fact]
  public async Task CreateUser_ShouldAddUserToContextAndReturnSameData()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var repository = new CreateUserRepository(context);
    var user = new User
    {
      Username = "newuser",
      Email = "new@test.com",
      PasswordHash = "hash",
      IsDeleted = false,
    };

    var result = await repository.CreateUser(user);

    result.Email.Should().Be("new@test.com");
    context.Users.Local.Should().ContainSingle();
    context.Users.Local.Single().Username.Should().Be("newuser");
  }
}
