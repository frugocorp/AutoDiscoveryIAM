// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMUpgradeTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Upgrades;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Data.Upgrades;

/// <summary>
/// Issue #103: pasos de actualización de datos que corren una sola vez por base de datos, y el
/// ayudante que renombra una clave de permiso sin que los roles pierdan su asignación.
/// </summary>
public class AutoDiscoveryIAMUpgradeTests
{
  private static readonly CancellationToken Ct = CancellationToken.None;

  [Fact]
  public async Task RunAsync_ShouldApplyEachPendingStepOnce_AndRecordIt()
  {
    var databaseName = Guid.NewGuid().ToString();
    var step = new CountingStep("0.2.0-test-step");

    await using (var context = CreateContext(databaseName))
    {
      var applied = await AutoDiscoveryIAMUpgradeRunner.RunAsync(context, [step], Ct);
      applied.Should().ContainSingle().Which.Should().Be("0.2.0-test-step");
    }

    await using (var context = CreateContext(databaseName))
    {
      var applied = await AutoDiscoveryIAMUpgradeRunner.RunAsync(context, [step], Ct);
      applied.Should().BeEmpty();
      (await context.Upgrades.CountAsync(Ct)).Should().Be(1);
    }

    step.Calls.Should().Be(1);
  }

  [Fact]
  public async Task RunAsync_WithNoSteps_ShouldDoNothing()
  {
    await using var context = CreateContext(Guid.NewGuid().ToString());

    var applied = await AutoDiscoveryIAMUpgradeRunner.RunAsync(context, AutoDiscoveryIAMUpgradeSteps.All, Ct);

    applied.Should().BeEmpty();
    (await context.Upgrades.CountAsync(Ct)).Should().Be(0);
  }

  [Fact]
  public async Task RenameAsync_WhenNewKeyDoesNotExist_ShouldRenameInPlaceKeepingAssignments()
  {
    await using var context = CreateContext(Guid.NewGuid().ToString());
    var (role, permission) = await SeedRoleWithPermissionAsync(context, "users.activation");

    await PermissionKeyRenamer.RenameAsync(context, "users.activation", "users.account_review", Ct);
    await context.SaveChangesAsync(Ct);

    var renamed = await context.Permissions.SingleAsync(Ct);
    renamed.Key.Should().Be("users.account_review");
    renamed.PreviousKey.Should().Be("users.activation");
    renamed.Id.Should().Be(permission.Id);
    (await context.RolePermissions.CountAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id, Ct)).Should().Be(1);
  }

  /// <summary>
  /// Si alguien sincronizó antes de actualizar, la clave nueva ya existe sin asignar: las
  /// asignaciones pasan a la nueva y la anterior queda inactiva.
  /// </summary>
  [Fact]
  public async Task RenameAsync_WhenNewKeyAlreadyExists_ShouldMoveAssignmentsAndDeactivateOldKey()
  {
    await using var context = CreateContext(Guid.NewGuid().ToString());
    var (role, oldPermission) = await SeedRoleWithPermissionAsync(context, "users.activation");
    var newPermission = new Permission { Key = "users.account_review", Group = "Users", IsActive = true };
    context.Permissions.Add(newPermission);
    await context.SaveChangesAsync(Ct);

    await PermissionKeyRenamer.RenameAsync(context, "users.activation", "users.account_review", Ct);
    await context.SaveChangesAsync(Ct);

    (await context.RolePermissions.CountAsync(rp => rp.RoleId == role.Id && rp.PermissionId == newPermission.Id, Ct)).Should().Be(1);
    (await context.RolePermissions.CountAsync(rp => rp.RoleId == role.Id && rp.PermissionId == oldPermission.Id, Ct)).Should().Be(0);
    (await context.Permissions.IgnoreQueryFilters().SingleAsync(p => p.Id == oldPermission.Id, Ct)).IsActive.Should().BeFalse();
  }

  [Fact]
  public async Task RenameAsync_WhenOldKeyDoesNotExist_ShouldDoNothing()
  {
    await using var context = CreateContext(Guid.NewGuid().ToString());

    await PermissionKeyRenamer.RenameAsync(context, "users.activation", "users.account_review", Ct);

    context.ChangeTracker.HasChanges().Should().BeFalse();
  }

  private static async Task<(Role Role, Permission Permission)> SeedRoleWithPermissionAsync(AutoDiscoveryIAMDbContext context, string key)
  {
    var role = new Role { Name = "Gerente" };
    var permission = new Permission { Key = key, Group = "Users", IsActive = true };
    context.Roles.Add(role);
    context.Permissions.Add(permission);
    await context.SaveChangesAsync(Ct);
    context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
    await context.SaveChangesAsync(Ct);
    return (role, permission);
  }

  private static AutoDiscoveryIAMDbContext CreateContext(string databaseName)
  {
    var options = new DbContextOptionsBuilder<AutoDiscoveryIAMDbContext>()
      .UseInMemoryDatabase(databaseName)
      .Options;
    return new AutoDiscoveryIAMDbContext(options, Substitute.For<ICurrentAuditUserService>());
  }

  private sealed class CountingStep(string id) : AutoDiscoveryIAMUpgradeStep
  {
    public override string Id { get; } = id;

    public int Calls { get; private set; }

    public override Task ApplyAsync(AutoDiscoveryIAMDbContext dbContext, CancellationToken cancellationToken)
    {
      Calls++;
      return Task.CompletedTask;
    }
  }
}
