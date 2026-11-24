using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.DefaultRole;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Services.DefaultRole;

/// <summary>
/// Pruebas del servicio que asigna el rol por defecto (IsDefaultForNewUser) a los usuarios nuevos.
/// </summary>
public class DefaultRoleAssignmentServiceTests
{
  private static AutoDiscoveryIAMDbContext CreateContext(string dbName)
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(databaseName: dbName)
      .Options;
    return new AutoDiscoveryIAMDbContext(options);
  }

  [Fact(DisplayName = "Con un rol por defecto activo, el usuario nuevo queda con ese rol al guardar")]
  public async Task AssignDefaultRoleAsync_WithActiveDefaultRole_AssignsIt()
  {
    using var context = CreateContext(nameof(AssignDefaultRoleAsync_WithActiveDefaultRole_AssignsIt));
    context.Roles.Add(new Role { Name = "Otro", IsActive = true });
    context.Roles.Add(new Role { Name = "UserDefault", IsActive = true, IsDefaultForNewUser = true });
    await context.SaveChangesAsync(CancellationToken.None);

    var user = new User { Username = "nuevo", Email = "nuevo@example.com", PasswordHash = "hash" };
    context.Users.Add(user);
    var assigned = await new DefaultRoleAssignmentService(context).AssignDefaultRoleAsync(user, CancellationToken.None);
    await context.SaveChangesAsync(CancellationToken.None);

    assigned.Should().BeTrue();
    var roleNames = await context.UserRoles
      .Where(ur => ur.UserId == user.Id)
      .Select(ur => ur.Role.Name)
      .ToListAsync(CancellationToken.None);
    roleNames.Should().ContainSingle().Which.Should().Be("UserDefault");
  }

  [Fact(DisplayName = "Sin rol por defecto, el usuario nuevo queda sin roles")]
  public async Task AssignDefaultRoleAsync_WithoutDefaultRole_AssignsNothing()
  {
    using var context = CreateContext(nameof(AssignDefaultRoleAsync_WithoutDefaultRole_AssignsNothing));
    context.Roles.Add(new Role { Name = "Otro", IsActive = true });
    await context.SaveChangesAsync(CancellationToken.None);

    var user = new User { Username = "nuevo", Email = "nuevo@example.com", PasswordHash = "hash" };
    context.Users.Add(user);
    var assigned = await new DefaultRoleAssignmentService(context).AssignDefaultRoleAsync(user, CancellationToken.None);

    assigned.Should().BeFalse();
    user.UserRoles.Should().BeEmpty();
  }

  [Theory(DisplayName = "Un rol por defecto desactivado o borrado no se asigna")]
  [InlineData(false, false)]
  [InlineData(true, true)]
  public async Task AssignDefaultRoleAsync_WithInactiveOrDeletedDefaultRole_AssignsNothing(bool isActive, bool isDeleted)
  {
    using var context = CreateContext($"{nameof(AssignDefaultRoleAsync_WithInactiveOrDeletedDefaultRole_AssignsNothing)}-{isActive}-{isDeleted}");
    var role = new Role { Name = "UserDefault", IsActive = isActive, IsDefaultForNewUser = true };
    context.Roles.Add(role);
    await context.SaveChangesAsync(CancellationToken.None);
    if (isDeleted)
    {
      // El interceptor de auditoría limpia IsDeleted al insertar: el borrado suave se aplica después.
      context.Roles.Remove(role);
      await context.SaveChangesAsync(CancellationToken.None);
    }

    var user = new User { Username = "nuevo", Email = "nuevo@example.com", PasswordHash = "hash" };
    var assigned = await new DefaultRoleAssignmentService(context).AssignDefaultRoleAsync(user, CancellationToken.None);

    assigned.Should().BeFalse();
    user.UserRoles.Should().BeEmpty();
  }
}
