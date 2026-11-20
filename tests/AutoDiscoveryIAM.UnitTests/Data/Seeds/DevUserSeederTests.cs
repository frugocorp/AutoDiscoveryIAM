using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Data.Seeds;

public class DevUserSeederTests
{
  private static AutoDiscoveryIAMDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .Options;

    return new AutoDiscoveryIAMDbContext(options);
  }

  [Theory]
  [InlineData("Development")]
  [InlineData("development")]
  [InlineData("Testing")]
  [InlineData("testing")]
  public async Task SeedDataAsync_ShouldCreateAllDevUsers_ForDevelopmentOrTestingRegardlessOfCase(string environmentName)
  {
    await using var context = CreateContext();

    await DevUserSeeder.SeedDataAsync(context, environmentName, CancellationToken.None);

    var usernames = await context.Users.Select(u => u.Username).ToListAsync(CancellationToken.None);
    usernames.Should().Contain("user");
    usernames.Should().Contain("manager");
    usernames.Should().Contain("inactive");
    usernames.Should().Contain("test");
    usernames.Should().Contain("dev");
    usernames.Should().Contain("su");
  }

  [Theory]
  [InlineData("Production")]
  [InlineData("MiEntorno")]
  public async Task SeedDataAsync_ShouldNotCreateAnyDevUser_ForOtherEnvironments(string environmentName)
  {
    await using var context = CreateContext();

    await DevUserSeeder.SeedDataAsync(context, environmentName, CancellationToken.None);

    var anyDevUser = await context.Users.AnyAsync(CancellationToken.None);
    anyDevUser.Should().BeFalse();
  }

  [Fact]
  public async Task SeedDataAsync_DevUser_ShouldHaveIsDevelopmentFlagSet()
  {
    await using var context = CreateContext();

    await DevUserSeeder.SeedDataAsync(context, "Development", CancellationToken.None);

    var devUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "dev", CancellationToken.None);
    devUser.Should().NotBeNull();
    devUser!.IsDevelopment.Should().BeTrue();
  }

  [Fact]
  public async Task SeedDataAsync_SuUser_ShouldHaveIsSuAdminFlagSet()
  {
    await using var context = CreateContext();

    await DevUserSeeder.SeedDataAsync(context, "Development", CancellationToken.None);

    var suUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "su", CancellationToken.None);
    suUser.Should().NotBeNull();
    suUser!.IsSuAdmin.Should().BeTrue();
  }
}
