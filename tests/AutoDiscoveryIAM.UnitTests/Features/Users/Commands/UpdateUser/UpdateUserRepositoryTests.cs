using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateUser;

public class UpdateUserRepositoryTests
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
      Username = "deleteduser",
      Email = "deleted@test.com",
      PasswordHash = "hash",
      IsDeleted = false,
    };
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    user.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new UpdateUserRepository(context);

    var result = await repository.GetByPublicIdAsync(user.PublicId, CancellationToken.None);

    result.Should().BeNull();
  }

  // Un usuario borrado suavemente sigue ocupando el username en el índice único: contarlo evita un
  // 500 por violación de índice al guardar.
  [Fact]
  public async Task ExistsUsernameAsync_ShouldIgnoreCurrentUserButCountDeletedUsers()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var current = new User { Username = "same", Email = "current@test.com", PasswordHash = "hash", IsDeleted = false };
    var deleted = new User { Username = "same", Email = "deleted@test.com", PasswordHash = "hash", IsDeleted = false };
    context.Users.AddRange(current, deleted);
    await context.SaveChangesAsync(CancellationToken.None);

    deleted.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new UpdateUserRepository(context);

    var exists = await repository.ExistsUsernameAsync("same", current.Id, CancellationToken.None);

    exists.Should().BeTrue();
  }

  [Fact]
  public async Task ExistsEmailAsync_ShouldReturnTrueForAnotherActiveUser()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var current = new User { Username = "u1", Email = "current@test.com", PasswordHash = "hash", IsDeleted = false };
    var other = new User { Username = "u2", Email = "dup@test.com", PasswordHash = "hash", IsDeleted = false };
    context.Users.AddRange(current, other);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new UpdateUserRepository(context);

    var exists = await repository.ExistsEmailAsync("dup@test.com", current.Id, CancellationToken.None);

    exists.Should().BeTrue();
  }
}
