using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Data.Seeds;

public class UserSeederTests
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
  [InlineData("DEVELOPMENT")]
  [InlineData("Testing")]
  [InlineData("testing")]
  [InlineData("TESTING")]
  public async Task SeedDataAsync_ShouldCreateBootstrapAdmin_ForDevelopmentOrTestingRegardlessOfCase(string environmentName)
  {
    await using var context = CreateContext();

    await UserSeeder.SeedDataAsync(context, environmentName, CancellationToken.None);

    var admin = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin", CancellationToken.None);
    admin.Should().NotBeNull();
    admin!.IsAdmin.Should().BeTrue();
  }

  [Theory]
  [InlineData("Production")]
  [InlineData("MiEntorno")]
  [InlineData("Staging")]
  public async Task SeedDataAsync_ShouldNotCreateBootstrapAdmin_ForOtherEnvironments(string environmentName)
  {
    await using var context = CreateContext();

    await UserSeeder.SeedDataAsync(context, environmentName, CancellationToken.None);

    var adminExists = await context.Users.AnyAsync(u => u.Username == "admin", CancellationToken.None);
    adminExists.Should().BeFalse();
  }

  [Fact]
  public async Task SeedDataAsync_ShouldAlwaysCreateSystemUser_RegardlessOfEnvironment()
  {
    await using var context = CreateContext();

    await UserSeeder.SeedDataAsync(context, "Production", CancellationToken.None);

    var systemUser = await context.Users.FirstOrDefaultAsync(u => u.Username == AutoDiscoveryIAMDbContext.SystemUsername, CancellationToken.None);
    systemUser.Should().NotBeNull();
    systemUser!.IsActive.Should().BeFalse();
  }
}
