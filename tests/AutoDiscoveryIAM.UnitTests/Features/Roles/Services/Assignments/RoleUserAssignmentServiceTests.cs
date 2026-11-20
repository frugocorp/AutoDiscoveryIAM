using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.Assignments;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RestoreUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Services.Assignments;

public class RoleUserAssignmentServiceTests
{
    private readonly IRoleUserAssignmentRepository _repository;
    private readonly ICreateUserRolesService _createUserRolesService;
    private readonly IDeleteUserRolesByPublicIdService _deleteUserRolesByPublicIdService;
    private readonly IRestoreUserRolesByPublicIdService _restoreUserRolesByPublicIdService;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork;
    private readonly RoleUserAssignmentService _service;

    public RoleUserAssignmentServiceTests()
    {
        _repository = Substitute.For<IRoleUserAssignmentRepository>();
        _createUserRolesService = Substitute.For<ICreateUserRolesService>();
        _deleteUserRolesByPublicIdService = Substitute.For<IDeleteUserRolesByPublicIdService>();
        _restoreUserRolesByPublicIdService = Substitute.For<IRestoreUserRolesByPublicIdService>();
        _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();

        _service = new RoleUserAssignmentService(
          _repository,
          _createUserRolesService,
          _deleteUserRolesByPublicIdService,
          _restoreUserRolesByPublicIdService,
          _unitOfWork);
    }

    [Fact]
    public async Task UpdateRoleUsersAsync_WhenRoleDoesNotExist_ShouldReturnNotFound()
    {
        var rolePublicId = Guid.NewGuid();
        _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _service.UpdateRoleUsersAsync(rolePublicId, [], dryRun: false, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Contain(rolePublicId.ToString());
        await _createUserRolesService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
        await _deleteUserRolesByPublicIdService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
        await _restoreUserRolesByPublicIdService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdateRoleUsersAsync_WithDryRun_ShouldNotPersistAndShouldReturnDiff()
    {
        var rolePublicId = Guid.NewGuid();
        var userPublicId = Guid.NewGuid();

        var role = new Role
        {
            Id = 100,
            PublicId = rolePublicId,
            Name = "role-test",
            IsActive = true,
            IsDeleted = false,
        };

        var user = new User
        {
            Id = 200,
            PublicId = userPublicId,
            Username = "user-test",
            Email = "user-test@example.com",
            PasswordHash = "hash",
            Status = "active",
            IsActive = true,
            IsDeleted = false,
        };

        _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetActiveUsersByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
          .Returns([user]);
        _repository.GetVisibleRelationsByRoleIdAsync(role.Id, Arg.Any<CancellationToken>())
          .Returns([]);
        _repository.GetDeletedRelationsByRoleIdAndUserIdsAsync(role.Id, Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>())
          .Returns([]);

        var result = await _service.UpdateRoleUsersAsync(rolePublicId, [userPublicId], dryRun: true, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.DryRun.Should().BeTrue();
        result.Data.TotalRequested.Should().Be(1);
        result.Data.TotalAssignedNew.Should().Be(1);
        result.Data.AssignedNewPublicIds.Should().ContainSingle().Which.Should().Be(userPublicId);
        await _createUserRolesService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
        await _deleteUserRolesByPublicIdService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
        await _restoreUserRolesByPublicIdService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdateRoleUsersAsync_WhenSomeRequestedUsersAreMissing_ShouldReturnNotFound()
    {
        var rolePublicId = Guid.NewGuid();
        var requestedUserA = Guid.NewGuid();
        var requestedUserB = Guid.NewGuid();

        var role = new Role
        {
            Id = 150,
            PublicId = rolePublicId,
            Name = "role-missing-users",
            IsActive = true,
            IsDeleted = false,
        };

        var onlyOneFoundUser = new User
        {
            Id = 250,
            PublicId = requestedUserA,
            Username = "user-found",
            Email = "user-found@example.com",
            PasswordHash = "hash",
            Status = "active",
            IsActive = true,
            IsDeleted = false,
        };

        _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetActiveUsersByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([onlyOneFoundUser]);

        var result = await _service.UpdateRoleUsersAsync(rolePublicId, [requestedUserA, requestedUserB], dryRun: false, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Contain(requestedUserB.ToString());
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdateRoleUsersAsync_WhenNotDryRun_ShouldApplyAllChangesAndPersist()
    {
        var now = DateTime.UtcNow;
        var rolePublicId = Guid.NewGuid();
        var userUnchangedPublicId = Guid.NewGuid();
        var userReactivatedPublicId = Guid.NewGuid();
        var userAssignedNewPublicId = Guid.NewGuid();
        var userToUnassignPublicId = Guid.NewGuid();

        var role = new Role
        {
            Id = 500,
            PublicId = rolePublicId,
            Name = "role-apply-users",
            IsActive = true,
            IsDeleted = false,
        };

        var userUnchanged = new User { Id = 601, PublicId = userUnchangedPublicId, Username = "u-unchanged", Email = "u-unchanged@example.com", PasswordHash = "hash", Status = "active", IsActive = true, IsDeleted = false };
        var userReactivated = new User { Id = 602, PublicId = userReactivatedPublicId, Username = "u-reactivated", Email = "u-reactivated@example.com", PasswordHash = "hash", Status = "active", IsActive = true, IsDeleted = false };
        var userAssignedNew = new User { Id = 603, PublicId = userAssignedNewPublicId, Username = "u-new", Email = "u-new@example.com", PasswordHash = "hash", Status = "active", IsActive = true, IsDeleted = false };
        var userToUnassign = new User { Id = 604, PublicId = userToUnassignPublicId, Username = "u-unassign", Email = "u-unassign@example.com", PasswordHash = "hash", Status = "active", IsActive = true, IsDeleted = false };

        var currentVisibleRelations = new List<UserRole>
        {
          new() { RoleId = role.Id, UserId = userUnchanged.Id, Role = role, User = userUnchanged, IsDeleted = false, AssignedAt = now },
          new() { RoleId = role.Id, UserId = userToUnassign.Id, Role = role, User = userToUnassign, IsDeleted = false, AssignedAt = now },
        };

        var deletedRelations = new List<UserRole>
        {
          new() { RoleId = role.Id, UserId = userReactivated.Id, Role = role, User = userReactivated, IsDeleted = true, AssignedAt = now },
        };

        _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetActiveUsersByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([userUnchanged, userReactivated, userAssignedNew]);
        _repository.GetVisibleRelationsByRoleIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns(currentVisibleRelations);
        _repository.GetDeletedRelationsByRoleIdAndUserIdsAsync(role.Id, Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns(deletedRelations);

        _deleteUserRolesByPublicIdService.ExecuteAsync(userToUnassignPublicId, rolePublicId, Arg.Any<CancellationToken>(), false)
          .Returns(OperationResult.Ok(new DeleteUserRolesByPublicIdResponseDto(userToUnassignPublicId, rolePublicId, now)));
        _restoreUserRolesByPublicIdService.ExecuteAsync(userReactivatedPublicId, rolePublicId, Arg.Any<CancellationToken>(), false)
          .Returns(OperationResult.Ok(new RestoreUserRolesByPublicIdResponseDto(userReactivatedPublicId, rolePublicId, now)));
        _createUserRolesService.ExecuteAsync(userAssignedNewPublicId, rolePublicId, Arg.Any<CancellationToken>(), false)
          .Returns(OperationResult.Ok(new CreateUserRolesResponseDto(userAssignedNewPublicId, rolePublicId, now)));

        var result = await _service.UpdateRoleUsersAsync(rolePublicId, [userUnchangedPublicId, userReactivatedPublicId, userAssignedNewPublicId], dryRun: false, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalRequested.Should().Be(3);
        result.Data.TotalAssignedNew.Should().Be(1);
        result.Data.TotalAssignedReactivated.Should().Be(1);
        result.Data.TotalUnassigned.Should().Be(1);
        result.Data.TotalUnchanged.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateRoleUsersAsync_WhenCreateStepFails_ShouldReturnErrorAndSkipPersist()
    {
        var rolePublicId = Guid.NewGuid();
        var requestedUserPublicId = Guid.NewGuid();

        var role = new Role
        {
            Id = 700,
            PublicId = rolePublicId,
            Name = "role-create-fail",
            IsActive = true,
            IsDeleted = false,
        };

        var requestedUser = new User
        {
            Id = 701,
            PublicId = requestedUserPublicId,
            Username = "u-create-fail",
            Email = "u-create-fail@example.com",
            PasswordHash = "hash",
            Status = "active",
            IsActive = true,
            IsDeleted = false,
        };

        _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetActiveUsersByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([requestedUser]);
        _repository.GetVisibleRelationsByRoleIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns([]);
        _repository.GetDeletedRelationsByRoleIdAndUserIdsAsync(role.Id, Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns([]);

        _createUserRolesService.ExecuteAsync(requestedUserPublicId, rolePublicId, Arg.Any<CancellationToken>(), false)
          .Returns(OperationResult.Fail<CreateUserRolesResponseDto>(ErrorInfo.Status422Business, "create failed"));

        var result = await _service.UpdateRoleUsersAsync(rolePublicId, [requestedUserPublicId], dryRun: false, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be("create failed");
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdateRoleUsersAsync_WhenDeleteStepFails_ShouldAbortFollowingStepsAndSkipPersist()
    {
        var rolePublicId = Guid.NewGuid();
        var requestedUserPublicId = Guid.NewGuid();
        var userToUnassignPublicId = Guid.NewGuid();

        var role = new Role
        {
            Id = 710,
            PublicId = rolePublicId,
            Name = "role-delete-fail",
            IsActive = true,
            IsDeleted = false,
        };

        var requestedUser = new User
        {
            Id = 711,
            PublicId = requestedUserPublicId,
            Username = "u-requested",
            Email = "u-requested@example.com",
            PasswordHash = "hash",
            Status = "active",
            IsActive = true,
            IsDeleted = false,
        };

        var userToUnassign = new User
        {
            Id = 712,
            PublicId = userToUnassignPublicId,
            Username = "u-unassign-fail",
            Email = "u-unassign-fail@example.com",
            PasswordHash = "hash",
            Status = "active",
            IsActive = true,
            IsDeleted = false,
        };

        _repository.GetActiveRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetActiveUsersByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([requestedUser]);
        _repository.GetVisibleRelationsByRoleIdAsync(role.Id, Arg.Any<CancellationToken>()).Returns([
          new UserRole { RoleId = role.Id, UserId = userToUnassign.Id, Role = role, User = userToUnassign, IsDeleted = false, AssignedAt = DateTime.UtcNow }
        ]);
        _repository.GetDeletedRelationsByRoleIdAndUserIdsAsync(role.Id, Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns([]);

        _deleteUserRolesByPublicIdService.ExecuteAsync(userToUnassignPublicId, rolePublicId, Arg.Any<CancellationToken>(), false)
          .Returns(OperationResult.Fail<DeleteUserRolesByPublicIdResponseDto>(ErrorInfo.Status422Business, "delete failed role-flow"));

        var result = await _service.UpdateRoleUsersAsync(rolePublicId, [requestedUserPublicId], dryRun: false, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be("delete failed role-flow");
        await _restoreUserRolesByPublicIdService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
        await _createUserRolesService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        var userPublicId = Guid.NewGuid();
        _repository.GetActiveUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _service.UpdateUserRolesAsync(userPublicId, [], dryRun: false, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Contain(userPublicId.ToString());
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WhenSomeRequestedRolesAreMissing_ShouldReturnNotFound()
    {
        var userPublicId = Guid.NewGuid();
        var requestedRoleA = Guid.NewGuid();
        var requestedRoleB = Guid.NewGuid();

        var user = new User
        {
            Id = 300,
            PublicId = userPublicId,
            Username = "user-missing-role",
            Email = "user-missing-role@example.com",
            PasswordHash = "hash",
            Status = "active",
            IsActive = true,
            IsDeleted = false,
        };

        var onlyOneFoundRole = new Role
        {
            Id = 400,
            PublicId = requestedRoleA,
            Name = "role-found",
            IsActive = true,
            IsDeleted = false,
        };

        _repository.GetActiveUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetActiveRolesByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([onlyOneFoundRole]);

        var result = await _service.UpdateUserRolesAsync(userPublicId, [requestedRoleA, requestedRoleB], dryRun: false, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Contain(requestedRoleB.ToString());
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WhenNotDryRun_ShouldApplyAllChangesAndPersist()
    {
        var now = DateTime.UtcNow;
        var userPublicId = Guid.NewGuid();
        var roleUnchangedPublicId = Guid.NewGuid();
        var roleReactivatedPublicId = Guid.NewGuid();
        var roleAssignedNewPublicId = Guid.NewGuid();
        var roleToUnassignPublicId = Guid.NewGuid();

        var user = new User
        {
            Id = 10,
            PublicId = userPublicId,
            Username = "user-apply",
            Email = "user-apply@example.com",
            PasswordHash = "hash",
            Status = "active",
            IsActive = true,
            IsDeleted = false,
        };

        var roleUnchanged = new Role { Id = 101, PublicId = roleUnchangedPublicId, Name = "role-unchanged", IsActive = true, IsDeleted = false };
        var roleReactivated = new Role { Id = 102, PublicId = roleReactivatedPublicId, Name = "role-reactivated", IsActive = true, IsDeleted = false };
        var roleAssignedNew = new Role { Id = 103, PublicId = roleAssignedNewPublicId, Name = "role-new", IsActive = true, IsDeleted = false };
        var roleToUnassign = new Role { Id = 104, PublicId = roleToUnassignPublicId, Name = "role-unassign", IsActive = true, IsDeleted = false };

        var currentVisibleRelations = new List<UserRole>
        {
          new() { UserId = user.Id, RoleId = roleUnchanged.Id, User = user, Role = roleUnchanged, IsDeleted = false, AssignedAt = now },
          new() { UserId = user.Id, RoleId = roleToUnassign.Id, User = user, Role = roleToUnassign, IsDeleted = false, AssignedAt = now },
        };

        var deletedRelations = new List<UserRole>
        {
          new() { UserId = user.Id, RoleId = roleReactivated.Id, User = user, Role = roleReactivated, IsDeleted = true, AssignedAt = now },
        };

        _repository.GetActiveUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetActiveRolesByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([roleUnchanged, roleReactivated, roleAssignedNew]);
        _repository.GetVisibleRelationsByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(currentVisibleRelations);
        _repository.GetDeletedRelationsByUserIdAndRoleIdsAsync(user.Id, Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns(deletedRelations);

        _deleteUserRolesByPublicIdService.ExecuteAsync(userPublicId, roleToUnassignPublicId, Arg.Any<CancellationToken>(), false)
          .Returns(OperationResult.Ok(new DeleteUserRolesByPublicIdResponseDto(userPublicId, roleToUnassignPublicId, now)));
        _restoreUserRolesByPublicIdService.ExecuteAsync(userPublicId, roleReactivatedPublicId, Arg.Any<CancellationToken>(), false)
          .Returns(OperationResult.Ok(new RestoreUserRolesByPublicIdResponseDto(userPublicId, roleReactivatedPublicId, now)));
        _createUserRolesService.ExecuteAsync(userPublicId, roleAssignedNewPublicId, Arg.Any<CancellationToken>(), false)
          .Returns(OperationResult.Ok(new CreateUserRolesResponseDto(userPublicId, roleAssignedNewPublicId, now)));

        var result = await _service.UpdateUserRolesAsync(userPublicId, [roleUnchangedPublicId, roleReactivatedPublicId, roleAssignedNewPublicId], dryRun: false, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalRequested.Should().Be(3);
        result.Data.TotalAssignedNew.Should().Be(1);
        result.Data.TotalAssignedReactivated.Should().Be(1);
        result.Data.TotalUnassigned.Should().Be(1);
        result.Data.TotalUnchanged.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WhenDeleteStepFails_ShouldReturnErrorAndSkipPersist()
    {
        var userPublicId = Guid.NewGuid();
        var requestedRolePublicId = Guid.NewGuid();
        var roleToUnassignPublicId = Guid.NewGuid();

        var user = new User
        {
            Id = 11,
            PublicId = userPublicId,
            Username = "user-delete-fail",
            Email = "user-delete-fail@example.com",
            PasswordHash = "hash",
            Status = "active",
            IsActive = true,
            IsDeleted = false,
        };

        var requestedRole = new Role { Id = 201, PublicId = requestedRolePublicId, Name = "role-requested", IsActive = true, IsDeleted = false };
        var roleToUnassign = new Role { Id = 202, PublicId = roleToUnassignPublicId, Name = "role-unassign", IsActive = true, IsDeleted = false };

        var visibleRelations = new List<UserRole>
        {
          new() { UserId = user.Id, RoleId = roleToUnassign.Id, User = user, Role = roleToUnassign, IsDeleted = false, AssignedAt = DateTime.UtcNow },
        };

        _repository.GetActiveUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetActiveRolesByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([requestedRole]);
        _repository.GetVisibleRelationsByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(visibleRelations);
        _repository.GetDeletedRelationsByUserIdAndRoleIdsAsync(user.Id, Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns([]);

        _deleteUserRolesByPublicIdService.ExecuteAsync(userPublicId, roleToUnassignPublicId, Arg.Any<CancellationToken>(), false)
          .Returns(OperationResult.Fail<DeleteUserRolesByPublicIdResponseDto>(ErrorInfo.Status422Business, "delete failed"));

        var result = await _service.UpdateUserRolesAsync(userPublicId, [requestedRolePublicId], dryRun: false, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be("delete failed");
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await _restoreUserRolesByPublicIdService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
        await _createUserRolesService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
    }

    [Fact]
    public async Task UpdateUserRolesAsync_WhenRestoreStepFails_ShouldAbortCreateAndSkipPersist()
    {
        var userPublicId = Guid.NewGuid();
        var requestedRoleReactivatedPublicId = Guid.NewGuid();
        var requestedRoleNewPublicId = Guid.NewGuid();

        var user = new User
        {
            Id = 12,
            PublicId = userPublicId,
            Username = "user-restore-fail",
            Email = "user-restore-fail@example.com",
            PasswordHash = "hash",
            Status = "active",
            IsActive = true,
            IsDeleted = false,
        };

        var requestedRoleReactivated = new Role { Id = 301, PublicId = requestedRoleReactivatedPublicId, Name = "role-reactivate", IsActive = true, IsDeleted = false };
        var requestedRoleNew = new Role { Id = 302, PublicId = requestedRoleNewPublicId, Name = "role-new", IsActive = true, IsDeleted = false };

        _repository.GetActiveUserByPublicIdAsync(userPublicId, Arg.Any<CancellationToken>()).Returns(user);
        _repository.GetActiveRolesByPublicIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>()).Returns([requestedRoleReactivated, requestedRoleNew]);
        _repository.GetVisibleRelationsByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns([]);
        _repository.GetDeletedRelationsByUserIdAndRoleIdsAsync(user.Id, Arg.Any<IReadOnlyCollection<long>>(), Arg.Any<CancellationToken>()).Returns([
            new UserRole { UserId = user.Id, RoleId = requestedRoleReactivated.Id, User = user, Role = requestedRoleReactivated, IsDeleted = true, AssignedAt = DateTime.UtcNow }
        ]);

        _restoreUserRolesByPublicIdService.ExecuteAsync(userPublicId, requestedRoleReactivatedPublicId, Arg.Any<CancellationToken>(), false)
            .Returns(OperationResult.Fail<RestoreUserRolesByPublicIdResponseDto>(ErrorInfo.Status422Business, "restore failed"));

        var result = await _service.UpdateUserRolesAsync(userPublicId, [requestedRoleReactivatedPublicId, requestedRoleNewPublicId], dryRun: false, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be("restore failed");
        await _createUserRolesService.DidNotReceiveWithAnyArgs().ExecuteAsync(default, default, default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}


