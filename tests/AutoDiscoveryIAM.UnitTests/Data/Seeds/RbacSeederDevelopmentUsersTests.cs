using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Data.Seeds;

/// <summary>
/// Las cuentas con contraseña pública (admin de bootstrap y usuarios de desarrollo) requieren opt-in
/// explícito además del ambiente: un despliegue con <c>ASPNETCORE_ENVIRONMENT=Development</c> por
/// descuido no debe nacer con ellas.
/// </summary>
public class RbacSeederDevelopmentUsersTests
{
  private static AutoDiscoveryIAMDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
      .Options;

    return new AutoDiscoveryIAMDbContext(options);
  }

  [Fact(DisplayName = "Development sin opt-in: no se siembra ninguna cuenta con contraseña pública")]
  public async Task SeedAllAsync_InDevelopmentWithoutOptIn_SeedsNoKnownPasswordAccounts()
  {
    await using var context = CreateContext();

    await RbacSeeder.SeedAllAsync(context, "Development", includeDevelopmentUsers: false, CancellationToken.None);

    (await RbacSeeder.FindActiveKnownPasswordAccountsAsync(context, CancellationToken.None)).Should().BeEmpty();
    (await context.Users.AnyAsync(u => u.Email == UserSeeder.AdminBootstrapUser.Email, CancellationToken.None)).Should().BeFalse();
    (await context.Users.AnyAsync(u => u.IsDevelopment || u.IsSuAdmin, CancellationToken.None)).Should().BeFalse();
  }

  [Fact(DisplayName = "Development con opt-in: se siembran el admin de bootstrap y los usuarios de desarrollo")]
  public async Task SeedAllAsync_InDevelopmentWithOptIn_SeedsKnownPasswordAccounts()
  {
    await using var context = CreateContext();

    await RbacSeeder.SeedAllAsync(context, "Development", includeDevelopmentUsers: true, CancellationToken.None);

    var accounts = await RbacSeeder.FindActiveKnownPasswordAccountsAsync(context, CancellationToken.None);
    accounts.Should().Contain(UserSeeder.AdminBootstrapUser.Email);
    accounts.Should().Contain("dev@example.com");
    accounts.Should().Contain("su@example.com");
    accounts.Should().NotContain("inactive@example.com", "solo cuentan las activas");
  }

  [Fact(DisplayName = "Production con opt-in: el opt-in se ignora")]
  public async Task SeedAllAsync_InProductionWithOptIn_SeedsNoKnownPasswordAccounts()
  {
    await using var context = CreateContext();

    await RbacSeeder.SeedAllAsync(context, "Production", includeDevelopmentUsers: true, CancellationToken.None);

    (await RbacSeeder.FindActiveKnownPasswordAccountsAsync(context, CancellationToken.None)).Should().BeEmpty();
  }

  [Fact(DisplayName = "Sin opt-in se siembra igual lo que existe en todo ambiente (roles y usuario de sistema)")]
  public async Task SeedAllAsync_WithoutOptIn_StillSeedsRolesAndSystemUser()
  {
    await using var context = CreateContext();

    await RbacSeeder.SeedAllAsync(context, "Development", includeDevelopmentUsers: false, CancellationToken.None);

    (await context.Roles.AnyAsync(r => r.Name == RoleSeeder.AdminAutoDiscoveryIamRoleName, CancellationToken.None)).Should().BeTrue();
    (await context.Users.AnyAsync(u => u.Username == "system", CancellationToken.None)).Should().BeTrue();
  }
}
