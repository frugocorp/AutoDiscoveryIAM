using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Repositories;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Infrastructure.Repositories;

public class PermissionsRepositoryTests
{
  [Fact]
  public async Task GetByKeyAsync_ShouldReturnMatchingPermission()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    context.Permissions.AddRange(
      new Permission { Key = "users.read", Group = "Users", IsActive = true },
      new Permission { Key = "users.write", Group = "Users", IsActive = true });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new PermissionsRepository(context);

    var result = await repository.GetByKeyAsync("users.write", CancellationToken.None);

    result.Should().NotBeNull();
    result!.Key.Should().Be("users.write");
  }

  /// <summary>
  /// Regresión: <c>GetByKeyAsync</c> debe encontrar permisos soft-deleted (<c>IsDeleted=true</c>),
  /// no solo activos — de lo contrario <c>SyncPermissionsService.ProcessNewPermissionsAsync</c> no
  /// puede reactivarlos y termina intentando insertar un duplicado que choca con el índice único de
  /// <c>Key</c> (ver issue #081).
  /// </summary>
  [Fact]
  public async Task GetByKeyAsync_ShouldFindSoftDeletedPermission()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var permission = new Permission { Key = "posts.get_all_posts", Group = "Posts", IsActive = true };
    context.Permissions.Add(permission);
    await context.SaveChangesAsync(CancellationToken.None);

    // Segundo SaveChanges como "Modified" (no "Added") para que el soft-delete persista de verdad —
    // AutoDiscoveryIAMDbContext.ApplyAuditInfoByState fuerza IsDeleted=false en el estado Added.
    permission.IsActive = false;
    permission.IsDeleted = true;
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new PermissionsRepository(context);

    var result = await repository.GetByKeyAsync("posts.get_all_posts", CancellationToken.None);

    result.Should().NotBeNull();
    result!.IsDeleted.Should().BeTrue();
  }

  [Fact]
  public async Task GetActivePermissionsAsync_ShouldReturnOnlyActiveOrderedByKey()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    context.Permissions.AddRange(
      new Permission { Key = "zeta", Group = "G", IsActive = true },
      new Permission { Key = "alpha", Group = "G", IsActive = true },
      new Permission { Key = "mid", Group = "G", IsActive = false });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new PermissionsRepository(context);

    var result = await repository.GetActivePermissionsAsync(CancellationToken.None);

    var keys = result.Select(x => x.Key).ToList();
    keys.Should().HaveCount(2);
    keys[0].Should().Be("alpha");
    keys[1].Should().Be("zeta");
  }

  [Fact]
  public async Task SearchPermissionsAsync_ShouldSearchInKeyDescriptionAndGroup()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    context.Permissions.AddRange(
      new Permission { Key = "users.read", Group = "Accounts", Description = "Read data" },
      new Permission { Key = "roles.assign", Group = "Security", Description = "Assign role" },
      new Permission { Key = "posts.read", Group = "Content", Description = "Read post" });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new PermissionsRepository(context);

    var byKey = await repository.SearchPermissionsAsync("roles", CancellationToken.None);
    var byDescription = await repository.SearchPermissionsAsync("post", CancellationToken.None);
    var byGroup = await repository.SearchPermissionsAsync("accounts", CancellationToken.None);

    byKey.Should().ContainSingle();
    byKey[0].Key.Should().Be("roles.assign");
    byDescription.Should().ContainSingle();
    byDescription[0].Key.Should().Be("posts.read");
    byGroup.Should().ContainSingle();
    byGroup[0].Key.Should().Be("users.read");
  }

  [Fact]
  public async Task GetPermissionsByAuthorizationTypeAsync_ShouldReturnEmptyWhenNoFiltersProvided()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    context.Permissions.Add(new Permission { Key = "users.read", Group = "Users" });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new PermissionsRepository(context);

    var result = await repository.GetPermissionsByAuthorizationTypeAsync(cancellationToken: CancellationToken.None);

    result.Should().BeEmpty();
  }

  [Fact]
  public async Task GetPermissionsByAuthorizationTypeAsync_ShouldApplyOrCombinationOfFilters()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    context.Permissions.AddRange(
      new Permission { Key = "a", Group = "G", HasPermissionsNotRequired = true },
      new Permission { Key = "b", Group = "G", RequiresDevelopmentAccess = true },
      new Permission { Key = "c", Group = "G", RequiresSuAdminAccess = true },
      new Permission { Key = "d", Group = "G", HasRequirePermissions = true },
      new Permission { Key = "e", Group = "G" });
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new PermissionsRepository(context);

    var result = await repository.GetPermissionsByAuthorizationTypeAsync(
      permissionsNotRequired: true,
      developmentAccess: true,
      suAdminAccess: false,
      explicitValidation: false,
      cancellationToken: CancellationToken.None);

    var keys = result.Select(x => x.Key).ToList();
    keys.Should().HaveCount(2);
    keys[0].Should().Be("a");
    keys[1].Should().Be("b");
  }
}
