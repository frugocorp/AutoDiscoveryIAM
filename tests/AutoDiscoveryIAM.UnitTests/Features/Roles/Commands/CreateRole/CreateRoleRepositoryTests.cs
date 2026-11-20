using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.CreateRole;

public class CreateRoleRepositoryTests
{
  [Fact]
  public async Task CreateRole_ShouldAddRoleToContextAndReturnSameData()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var repository = new CreateRoleRepository(context);
    var role = new Role { Name = "NewRole", Description = "desc", IsActive = true, IsDeleted = false };

    var result = await repository.CreateRole(role);

    result.Name.Should().Be("NewRole");
    context.Roles.Local.Should().ContainSingle();
    context.Roles.Local.Single().Name.Should().Be("NewRole");
  }

  [Fact]
  public async Task RoleNameExistsAsync_ShouldReturnTrueWhenNameExists()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    context.Roles.Add(new Role { Name = "Admin", Description = "desc", IsActive = true, IsDeleted = false });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new CreateRoleRepository(context);

    var exists = await repository.RoleNameExistsAsync("Admin");

    exists.Should().BeTrue();
  }
}
