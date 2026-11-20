using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.PasswordManagement;

public class PasswordManagementRepositoryTests
{
  private static AutoDiscoveryIAMDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new AutoDiscoveryIAMDbContext(options);
  }

  [Fact]
  public async Task GetByPublicIdAsync_ShouldReturnUser_WhenExistsAndNotDeleted()
  {
    await using var context = CreateContext();
    var publicId = Guid.NewGuid();
    context.Users.Add(new User
    {
      PublicId = publicId,
      Username = "user1",
      Email = "user1@test.com",
      PasswordHash = "hash",
      IsDeleted = false,
    });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new PasswordManagementRepository(context);
    var result = await repository.GetByPublicIdAsync(publicId, CancellationToken.None);

    result.Should().NotBeNull();
    result!.PublicId.Should().Be(publicId);
  }

  [Fact]
  public async Task GetByPublicIdAsync_ShouldReturnNull_WhenUserIsSoftDeleted()
  {
    await using var context = CreateContext();
    var publicId = Guid.NewGuid();
    var user = new User
    {
      PublicId = publicId,
      Username = "deleted-user",
      Email = "deleted@test.com",
      PasswordHash = "hash",
      IsDeleted = false,
    };
    context.Users.Add(user);
    await context.SaveChangesAsync(CancellationToken.None);

    user.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new PasswordManagementRepository(context);
    var result = await repository.GetByPublicIdAsync(publicId, CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task GetByEmailAsync_ShouldReturnUser_WhenEmailExists()
  {
    await using var context = CreateContext();
    context.Users.Add(new User
    {
      PublicId = Guid.NewGuid(),
      Username = "user2",
      Email = "user2@test.com",
      PasswordHash = "hash",
      IsDeleted = false,
    });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new PasswordManagementRepository(context);
    var result = await repository.GetByEmailAsync("user2@test.com", CancellationToken.None);

    result.Should().NotBeNull();
    result!.Email.Should().Be("user2@test.com");
  }

  [Fact]
  public async Task GetByEmailAsync_ShouldReturnNull_WhenEmailDoesNotExist()
  {
    await using var context = CreateContext();

    var repository = new PasswordManagementRepository(context);
    var result = await repository.GetByEmailAsync("missing@test.com", CancellationToken.None);

    result.Should().BeNull();
  }

  [Fact]
  public async Task GetByResetTokenHashAsync_ShouldReturnUser_WhenTokenMatches()
  {
    await using var context = CreateContext();
    context.Users.Add(new User
    {
      PublicId = Guid.NewGuid(),
      Username = "user3",
      Email = "user3@test.com",
      PasswordHash = "hash",
      PasswordResetToken = "reset-token-123",
      PasswordResetTokenExpiration = DateTime.UtcNow.AddMinutes(30),
      IsDeleted = false,
    });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new PasswordManagementRepository(context);
    var result = await repository.GetByResetTokenHashAsync("reset-token-123", CancellationToken.None);

    result.Should().NotBeNull();
    result!.Email.Should().Be("user3@test.com");
  }

  [Fact]
  public async Task GetByResetTokenHashAsync_ShouldReturnNull_WhenTokenDoesNotMatch()
  {
    await using var context = CreateContext();
    context.Users.Add(new User
    {
      PublicId = Guid.NewGuid(),
      Username = "user4",
      Email = "user4@test.com",
      PasswordHash = "hash",
      PasswordResetToken = "reset-token-abc",
      PasswordResetTokenExpiration = DateTime.UtcNow.AddMinutes(30),
      IsDeleted = false,
    });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new PasswordManagementRepository(context);
    var result = await repository.GetByResetTokenHashAsync("other-token", CancellationToken.None);

    result.Should().BeNull();
  }
}
