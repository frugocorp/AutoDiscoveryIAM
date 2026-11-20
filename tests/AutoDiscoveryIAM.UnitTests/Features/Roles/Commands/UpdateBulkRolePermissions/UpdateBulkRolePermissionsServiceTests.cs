using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.UpdateBulkRolePermissions;

public class UpdateBulkRolePermissionsServiceTests
{
  private readonly IUpdateBulkRolePermissionsRepository _repository;
  private readonly ICreateRolePermissionsService _createService;
  private readonly IDeleteRolePermissionsByPermissionPublicIdService _deleteService;
  private readonly IRestoreRolePermissionsByPermissionPublicIdService _restoreService;
  private readonly UpdateBulkRolePermissionsService _service;

  public UpdateBulkRolePermissionsServiceTests()
  {
    _repository = Substitute.For<IUpdateBulkRolePermissionsRepository>();
    _createService = Substitute.For<ICreateRolePermissionsService>();
    _deleteService = Substitute.For<IDeleteRolePermissionsByPermissionPublicIdService>();
    _restoreService = Substitute.For<IRestoreRolePermissionsByPermissionPublicIdService>();

    _service = new UpdateBulkRolePermissionsService(_repository, _createService, _deleteService, _restoreService);
  }

  [Fact]
  public async Task ExecuteAsync_WhenRoleDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
  {
    var rolePublicId = Guid.NewGuid();

    _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>())
        .Returns((Role?)null);

    var result = await _service.ExecuteAsync(rolePublicId, [], false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.ActiveRoleNotFound.Message, rolePublicId));
  }

  [Fact]
  public async Task ExecuteAsync_WithDryRun_ShouldReturnDiffWithoutPersisting()
  {
    var rolePublicId = Guid.NewGuid();
    var permissionPublicId = Guid.NewGuid();
    var role = new Role
    {
      Id = 10,
      PublicId = rolePublicId,
      Name = "role-test",
      IsActive = true,
      IsDeleted = false,
    };
    var permission = new Permission
    {
      Id = 20,
      PublicId = permissionPublicId,
      Key = $"permissions.{Guid.NewGuid():N}",
      IsActive = true,
      IsDeleted = false,
    };

    _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
    _repository.GetActivePermissionsByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
        .Returns([permission]);
    _repository.GetVisibleRelationsByRoleIdAsync(role.Id, Arg.Any<CancellationToken>())
        .Returns([]);
    _repository.GetDeletedRelationsByRoleIdAndPermissionIdsAsync(role.Id, Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
        .Returns([]);

    var result = await _service.ExecuteAsync(rolePublicId, [permissionPublicId], true, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.DryRun.Should().BeTrue();
    result.Data.TotalAssignedNew.Should().Be(1);
    result.Data.AssignedNewPublicIds.Should().ContainSingle().Which.Should().Be(permissionPublicId);
    await _createService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
    await _deleteService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
    await _restoreService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
  }

  [Fact]
  public async Task ExecuteAsync_WhenChangesAreApplied_ShouldCallDeleteRestoreAndCreate()
  {
    var rolePublicId = Guid.NewGuid();
    var unchangedPermissionPublicId = Guid.NewGuid();
    var newPermissionPublicId = Guid.NewGuid();
    var reactivatedPermissionPublicId = Guid.NewGuid();
    var removedPermissionPublicId = Guid.NewGuid();

    var role = new Role
    {
      Id = 100,
      PublicId = rolePublicId,
      Name = "role-apply",
      IsActive = true,
      IsDeleted = false,
    };

    var unchangedPermission = BuildPermission(1, unchangedPermissionPublicId);
    var newPermission = BuildPermission(2, newPermissionPublicId);
    var reactivatedPermission = BuildPermission(3, reactivatedPermissionPublicId);
    var removedPermission = BuildPermission(4, removedPermissionPublicId);

    _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
    _repository.GetActivePermissionsByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
        .Returns([unchangedPermission, newPermission, reactivatedPermission]);
    _repository.GetVisibleRelationsByRoleIdAsync(role.Id, Arg.Any<CancellationToken>())
        .Returns([
            new RolePermission { RoleId = role.Id, PermissionId = unchangedPermission.Id, Permission = unchangedPermission, Role = role, IsDeleted = false },
                new RolePermission { RoleId = role.Id, PermissionId = removedPermission.Id, Permission = removedPermission, Role = role, IsDeleted = false },
        ]);
    _repository.GetDeletedRelationsByRoleIdAndPermissionIdsAsync(role.Id, Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
        .Returns([
            new RolePermission { RoleId = role.Id, PermissionId = reactivatedPermission.Id, Permission = reactivatedPermission, Role = role, IsDeleted = true },
        ]);

    _deleteService.ExecuteAsync(rolePublicId, removedPermissionPublicId, Arg.Any<CancellationToken>(), true)
        .Returns(OperationResult.Ok(new DeleteRolePermissionsByPermissionPublicIdResponseDto(rolePublicId, removedPermissionPublicId, DateTime.UtcNow)));
    _restoreService.ExecuteAsync(rolePublicId, reactivatedPermissionPublicId, Arg.Any<CancellationToken>(), true)
        .Returns(OperationResult.Ok(new RestoreRolePermissionsByPermissionPublicIdResponseDto(rolePublicId, reactivatedPermissionPublicId, DateTime.UtcNow)));
    _createService.ExecuteAsync(rolePublicId, newPermissionPublicId, Arg.Any<CancellationToken>(), true)
        .Returns(OperationResult.Ok(new CreateRolePermissionsResponseDto(rolePublicId, newPermissionPublicId, DateTime.UtcNow)));

    var result = await _service.ExecuteAsync(rolePublicId, [unchangedPermissionPublicId, newPermissionPublicId, reactivatedPermissionPublicId], false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.TotalAssignedNew.Should().Be(1);
    result.Data.TotalAssignedReactivated.Should().Be(1);
    result.Data.TotalUnassigned.Should().Be(1);
    result.Data.TotalUnchanged.Should().Be(1);
    await _deleteService.Received(1).ExecuteAsync(rolePublicId, removedPermissionPublicId, Arg.Any<CancellationToken>(), true);
    await _restoreService.Received(1).ExecuteAsync(rolePublicId, reactivatedPermissionPublicId, Arg.Any<CancellationToken>(), true);
    await _createService.Received(1).ExecuteAsync(rolePublicId, newPermissionPublicId, Arg.Any<CancellationToken>(), true);
  }

  [Fact]
  public async Task ExecuteAsync_WhenSomeRequestedPermissionsAreMissing_ShouldReturnNotFound()
  {
    var rolePublicId = Guid.NewGuid();
    var requestedPermissionA = Guid.NewGuid();
    var requestedPermissionB = Guid.NewGuid();

    var role = new Role
    {
      Id = 150,
      PublicId = rolePublicId,
      Name = "role-missing-perms",
      IsActive = true,
      IsDeleted = false,
    };

    var onlyOneFoundPermission = BuildPermission(200, requestedPermissionA);

    _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
    _repository.GetActivePermissionsByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
        .Returns([onlyOneFoundPermission]);

    var result = await _service.ExecuteAsync(rolePublicId, [requestedPermissionA, requestedPermissionB], false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
    result.Error.Should().Contain(requestedPermissionB.ToString());
    await _deleteService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
    await _restoreService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
    await _createService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
  }

  [Fact]
  public async Task ExecuteAsync_WhenNotDryRun_ShouldApplyAllChangesAndPersist()
  {
    var now = DateTime.UtcNow;
    var rolePublicId = Guid.NewGuid();
    var unchangedPermissionPublicId = Guid.NewGuid();
    var reactivatedPermissionPublicId = Guid.NewGuid();
    var newPermissionPublicId = Guid.NewGuid();
    var toUnassignPermissionPublicId = Guid.NewGuid();

    var role = new Role
    {
      Id = 160,
      PublicId = rolePublicId,
      Name = "role-apply-permissions",
      IsActive = true,
      IsDeleted = false,
    };

    var unchangedPerm = BuildPermission(210, unchangedPermissionPublicId);
    var reactivatedPerm = BuildPermission(211, reactivatedPermissionPublicId);
    var newPerm = BuildPermission(212, newPermissionPublicId);
    var toUnassignPerm = BuildPermission(213, toUnassignPermissionPublicId);

    var currentVisibleRelations = new List<RolePermission>
        {
            new() { RoleId = role.Id, PermissionId = unchangedPerm.Id, Permission = unchangedPerm, Role = role, IsDeleted = false, CreatedAt = now },
            new() { RoleId = role.Id, PermissionId = toUnassignPerm.Id, Permission = toUnassignPerm, Role = role, IsDeleted = false, CreatedAt = now },
        };

    var deletedRelations = new List<RolePermission>
        {
            new() { RoleId = role.Id, PermissionId = reactivatedPerm.Id, Permission = reactivatedPerm, Role = role, IsDeleted = true, CreatedAt = now },
        };

    _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
    _repository.GetActivePermissionsByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
        .Returns([unchangedPerm, reactivatedPerm, newPerm]);
    _repository.GetVisibleRelationsByRoleIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(currentVisibleRelations);
    _repository.GetDeletedRelationsByRoleIdAndPermissionIdsAsync(role.Id, Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
        .Returns(deletedRelations);

    _deleteService.ExecuteAsync(rolePublicId, toUnassignPermissionPublicId, Arg.Any<CancellationToken>(), true)
        .Returns(OperationResult.Ok(new DeleteRolePermissionsByPermissionPublicIdResponseDto(rolePublicId, toUnassignPermissionPublicId, now)));
    _restoreService.ExecuteAsync(rolePublicId, reactivatedPermissionPublicId, Arg.Any<CancellationToken>(), true)
        .Returns(OperationResult.Ok(new RestoreRolePermissionsByPermissionPublicIdResponseDto(rolePublicId, reactivatedPermissionPublicId, now)));
    _createService.ExecuteAsync(rolePublicId, newPermissionPublicId, Arg.Any<CancellationToken>(), true)
        .Returns(OperationResult.Ok(new CreateRolePermissionsResponseDto(rolePublicId, newPermissionPublicId, now)));

    var result = await _service.ExecuteAsync(rolePublicId, [unchangedPermissionPublicId, reactivatedPermissionPublicId, newPermissionPublicId], false, CancellationToken.None);

    result.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.TotalRequested.Should().Be(3);
    result.Data.TotalAssignedNew.Should().Be(1);
    result.Data.TotalAssignedReactivated.Should().Be(1);
    result.Data.TotalUnassigned.Should().Be(1);
    result.Data.TotalUnchanged.Should().Be(1);
    await _deleteService.Received(1).ExecuteAsync(rolePublicId, toUnassignPermissionPublicId, Arg.Any<CancellationToken>(), true);
    await _restoreService.Received(1).ExecuteAsync(rolePublicId, reactivatedPermissionPublicId, Arg.Any<CancellationToken>(), true);
    await _createService.Received(1).ExecuteAsync(rolePublicId, newPermissionPublicId, Arg.Any<CancellationToken>(), true);
  }

  [Fact]
  public async Task ExecuteAsync_WhenDeleteStepFails_ShouldReturnErrorAndSkipSubsequentSteps()
  {
    var rolePublicId = Guid.NewGuid();
    var requestedPermissionPublicId = Guid.NewGuid();
    var permissionToUnassignPublicId = Guid.NewGuid();

    var role = new Role
    {
      Id = 170,
      PublicId = rolePublicId,
      Name = "role-delete-fail",
      IsActive = true,
      IsDeleted = false,
    };

    var requestedPerm = BuildPermission(220, requestedPermissionPublicId);
    var toUnassignPerm = BuildPermission(221, permissionToUnassignPublicId);

    _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
    _repository.GetActivePermissionsByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
        .Returns([requestedPerm]);
    _repository.GetVisibleRelationsByRoleIdAsync(role.Id, Arg.Any<CancellationToken>())
        .Returns([new RolePermission { RoleId = role.Id, PermissionId = toUnassignPerm.Id, Permission = toUnassignPerm, Role = role, IsDeleted = false }]);
    _repository.GetDeletedRelationsByRoleIdAndPermissionIdsAsync(role.Id, Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
        .Returns([]);

    _deleteService.ExecuteAsync(rolePublicId, permissionToUnassignPublicId, Arg.Any<CancellationToken>(), true)
        .Returns(OperationResult.Fail<DeleteRolePermissionsByPermissionPublicIdResponseDto>(ErrorInfo.Status422Business, "delete failed"));

    var result = await _service.ExecuteAsync(rolePublicId, [requestedPermissionPublicId], false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    result.Error.Should().Be("delete failed");
    await _restoreService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
    await _createService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
  }

  [Fact]
  public async Task ExecuteAsync_WhenRestoreStepFails_ShouldReturnErrorAndSkipCreate()
  {
    var rolePublicId = Guid.NewGuid();
    var requestedPermissionPublicId = Guid.NewGuid();
    var reactivatedPermissionPublicId = Guid.NewGuid();

    var role = new Role
    {
      Id = 180,
      PublicId = rolePublicId,
      Name = "role-restore-fail",
      IsActive = true,
      IsDeleted = false,
    };

    var requestedPerm = BuildPermission(230, requestedPermissionPublicId);
    var reactivatedPerm = BuildPermission(231, reactivatedPermissionPublicId);

    _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
    _repository.GetActivePermissionsByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
        .Returns([requestedPerm, reactivatedPerm]);
    _repository.GetVisibleRelationsByRoleIdAsync(role.Id, Arg.Any<CancellationToken>())
        .Returns([]);
    _repository.GetDeletedRelationsByRoleIdAndPermissionIdsAsync(role.Id, Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
        .Returns([new RolePermission { RoleId = role.Id, PermissionId = reactivatedPerm.Id, Permission = reactivatedPerm, Role = role, IsDeleted = true }]);

    _restoreService.ExecuteAsync(rolePublicId, reactivatedPermissionPublicId, Arg.Any<CancellationToken>(), true)
        .Returns(OperationResult.Fail<RestoreRolePermissionsByPermissionPublicIdResponseDto>(ErrorInfo.Status422Business, "restore failed"));

    var result = await _service.ExecuteAsync(rolePublicId, [requestedPermissionPublicId, reactivatedPermissionPublicId], false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    result.Error.Should().Be("restore failed");
    await _createService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
  }

  [Fact]
  public async Task ExecuteAsync_WhenCreateStepFails_ShouldReturnErrorWithoutPersisting()
  {
    var rolePublicId = Guid.NewGuid();
    var newPermissionPublicId = Guid.NewGuid();

    var role = new Role
    {
      Id = 190,
      PublicId = rolePublicId,
      Name = "role-create-fail",
      IsActive = true,
      IsDeleted = false,
    };

    var newPerm = BuildPermission(240, newPermissionPublicId);

    _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
    _repository.GetActivePermissionsByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
        .Returns([newPerm]);
    _repository.GetVisibleRelationsByRoleIdAsync(role.Id, Arg.Any<CancellationToken>())
        .Returns([]);
    _repository.GetDeletedRelationsByRoleIdAndPermissionIdsAsync(role.Id, Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
        .Returns([]);

    _createService.ExecuteAsync(rolePublicId, newPermissionPublicId, Arg.Any<CancellationToken>(), true)
        .Returns(OperationResult.Fail<CreateRolePermissionsResponseDto>(ErrorInfo.Status422Business, "create failed"));

    var result = await _service.ExecuteAsync(rolePublicId, [newPermissionPublicId], false, CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
    result.Error.Should().Be("create failed");
  }

  private static Permission BuildPermission(int id, Guid publicId)
      => new()
      {
        Id = id,
        PublicId = publicId,
        Key = $"permissions.{publicId:N}",
        IsActive = true,
        IsDeleted = false,
      };
}


