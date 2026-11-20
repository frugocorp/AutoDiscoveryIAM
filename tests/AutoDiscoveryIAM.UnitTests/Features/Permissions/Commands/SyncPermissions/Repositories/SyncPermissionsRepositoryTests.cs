using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Repositories;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Permissions.Commands.SyncPermissions.Repositories;

public class SyncPermissionsRepositoryTests
{
  [Fact]
  public async Task MarkAsObsoleteAsync_ShouldDeactivateAndSoftDeletePermission()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var permission = new Permission { Key = "perm.sync", Group = "G", IsActive = true, IsDeleted = false };
    context.Permissions.Add(permission);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new SyncPermissionsRepository(context);

    await repository.MarkAsObsoleteAsync(permission.Key, CancellationToken.None);

    permission.IsActive.Should().BeFalse();
    permission.IsDeleted.Should().BeTrue();
    permission.DeletedBy.Should().Be(1);
    permission.UpdatedBy.Should().Be(1);
  }

  [Fact]
  public async Task RecoverPermissionAsync_ShouldRestoreAndSetAuditFields()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var permission = new Permission { Key = "perm.recover", Group = "G", IsActive = false, IsDeleted = true };
    context.Permissions.Add(permission);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new SyncPermissionsRepository(context);

    await repository.RecoverPermissionAsync(permission, CancellationToken.None);

    permission.IsActive.Should().BeTrue();
    permission.IsDeleted.Should().BeFalse();
    permission.RestoredBy.Should().Be(1);
    permission.UpdatedBy.Should().Be(1);
  }

  [Fact]
  public async Task UpdateLastSyncAsync_ShouldUpdateFieldOnlyWhenPermissionExists()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var permission = new Permission { Key = "perm.sync-date", Group = "G", IsActive = true, IsDeleted = false, LastSyncAt = null };
    context.Permissions.Add(permission);
    await context.SaveChangesAsync(CancellationToken.None);

    var syncDate = DateTime.UtcNow.AddMinutes(-3);
    var repository = new SyncPermissionsRepository(context);

    await repository.UpdateLastSyncAsync(permission.Key, syncDate, CancellationToken.None);
    await repository.UpdateLastSyncAsync("perm.not-found", DateTime.UtcNow, CancellationToken.None);

    permission.LastSyncAt.Should().Be(syncDate);
  }

  [Fact]
  public async Task GetOutOfSyncPermissionsAsync_ShouldReturnActivePermissionsNotSyncedSinceCutoff()
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;

    await using var context = new AutoDiscoveryIAMDbContext(options);
    var cutoff = DateTime.UtcNow.AddHours(-1);

    var oldSync = new Permission { Key = "perm.old", Group = "G", IsActive = true, IsDeleted = false, LastSyncAt = cutoff.AddHours(-1) };
    var noSync = new Permission { Key = "perm.none", Group = "G", IsActive = true, IsDeleted = false, LastSyncAt = null };
    var freshSync = new Permission { Key = "perm.fresh", Group = "G", IsActive = true, IsDeleted = false, LastSyncAt = cutoff.AddMinutes(5) };
    var inactive = new Permission { Key = "perm.inactive", Group = "G", IsActive = false, IsDeleted = false, LastSyncAt = null };
    context.Permissions.AddRange(oldSync, noSync, freshSync, inactive);
    await context.SaveChangesAsync(CancellationToken.None);

    var repository = new SyncPermissionsRepository(context);

    var result = await repository.GetOutOfSyncPermissionsAsync(cutoff, CancellationToken.None);

    var keys = result.Select(x => x.Key).ToList();
    keys.Should().Contain("perm.old");
    keys.Should().Contain("perm.none");
    keys.Should().NotContain("perm.fresh");
    keys.Should().NotContain("perm.inactive");
  }
}
