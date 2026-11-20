using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.RestoreRolePermissionsByPermissionPublicId;

public class RestoreRolePermissionsByPermissionPublicIdServiceTests
{
    private readonly IRestoreRolePermissionsByPermissionPublicIdRepository _repository;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork;
    private readonly RestoreRolePermissionsByPermissionPublicIdService _service;

    public RestoreRolePermissionsByPermissionPublicIdServiceTests()
    {
        _repository = Substitute.For<IRestoreRolePermissionsByPermissionPublicIdRepository>();
        _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        _service = new RestoreRolePermissionsByPermissionPublicIdService(_repository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDeletedRelationDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        _repository.GetDeletedRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
            .Returns((RolePermission?)null);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.DeletedRelationNotFound.Message, rolePublicId, permissionPublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleOrPermissionIsInactive_ShouldReturnBusinessError()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();
        var relation = BuildRelation(rolePublicId, permissionPublicId, roleActive: true, permissionActive: false);

        _repository.GetDeletedRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(RolePermissionAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDeletedRelationExists_ShouldRestoreAndPersist()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();
        var relation = BuildRelation(rolePublicId, permissionPublicId, roleActive: true, permissionActive: true);

        _repository.GetDeletedRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        relation.IsDeleted.Should().BeFalse();
        relation.DeletedAt.Should().BeNull();
        relation.DeletedBy.Should().BeNull();
        relation.RestoredAt.Should().NotBeNull();
        relation.RestoredBy.Should().Be(1);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleIsDeleted_ShouldReturnBusinessError()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();
        var relation = new RolePermission
        {
            RoleId = 11,
            PermissionId = 21,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = 1,
            Role = new Role
            {
                Id = 11,
                PublicId = rolePublicId,
                Name = "role-deleted",
                IsActive = true,
                IsDeleted = true,
            },
            Permission = new Permission
            {
                Id = 21,
                PublicId = permissionPublicId,
                Key = $"permissions.{Guid.NewGuid():N}",
                IsActive = true,
                IsDeleted = false,
            },
        };

        _repository.GetDeletedRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(RolePermissionAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPermissionIsDeleted_ShouldReturnBusinessError()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();
        var relation = new RolePermission
        {
            RoleId = 12,
            PermissionId = 22,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = 1,
            Role = new Role
            {
                Id = 12,
                PublicId = rolePublicId,
                Name = "role-ok",
                IsActive = true,
                IsDeleted = false,
            },
            Permission = new Permission
            {
                Id = 22,
                PublicId = permissionPublicId,
                Key = $"permissions.{Guid.NewGuid():N}",
                IsActive = true,
                IsDeleted = true,
            },
        };

        _repository.GetDeletedRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(RolePermissionAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBothRoleAndPermissionAreInactive_ShouldReturnBusinessError()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();
        var relation = new RolePermission
        {
            RoleId = 13,
            PermissionId = 23,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = 1,
            Role = new Role
            {
                Id = 13,
                PublicId = rolePublicId,
                Name = "role-both-inactive",
                IsActive = false,
                IsDeleted = false,
            },
            Permission = new Permission
            {
                Id = 23,
                PublicId = permissionPublicId,
                Key = $"permissions.{Guid.NewGuid():N}",
                IsActive = false,
                IsDeleted = false,
            },
        };

        _repository.GetDeletedRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(RolePermissionAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPersistChangesIsFalse_ShouldNotSaveChanges()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();
        var relation = BuildRelation(rolePublicId, permissionPublicId, roleActive: true, permissionActive: true);

        _repository.GetDeletedRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None, persistChanges: false);

        result.Success.Should().BeTrue();
        relation.IsDeleted.Should().BeFalse();
        relation.RestoredAt.Should().NotBeNull();
        relation.RestoredBy.Should().Be(1);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAssignRestoreTimestampsAndClearDeleteTimestamps()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();
        var beforeExecution = DateTime.UtcNow;
        var relation = new RolePermission
        {
            RoleId = 14,
            PermissionId = 24,
            IsDeleted = true,
            DeletedAt = beforeExecution.AddMinutes(-10),
            DeletedBy = 1,
            RestoredAt = null,
            RestoredBy = null,
            AssignedAt = beforeExecution.AddMinutes(-15),
            UpdatedAt = beforeExecution.AddMinutes(-10),
            UpdatedBy = 1,
            Role = new Role
            {
                Id = 14,
                PublicId = rolePublicId,
                Name = "role-timestamps",
                IsActive = true,
                IsDeleted = false,
            },
            Permission = new Permission
            {
                Id = 24,
                PublicId = permissionPublicId,
                Key = $"permissions.{Guid.NewGuid():N}",
                IsActive = true,
                IsDeleted = false,
            },
        };

        _repository.GetDeletedRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        relation.IsDeleted.Should().BeFalse();
        relation.DeletedAt.Should().BeNull();
        relation.DeletedBy.Should().BeNull();
        relation.RestoredAt.Should().NotBeNull();
        relation.RestoredAt.Should().NotBeNull(); relation.RestoredAt.Should().BeGreaterThanOrEqualTo(beforeExecution);
        relation.RestoredBy.Should().Be(1);
        relation.AssignedAt.Should().NotBeNull();
        relation.AssignedAt.Should().NotBeNull(); relation.AssignedAt.Should().BeGreaterThanOrEqualTo(beforeExecution);
        relation.UpdatedAt.Should().BeGreaterThanOrEqualTo(beforeExecution);
        relation.UpdatedBy.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnResponseDtoWithCorrectPublicIds()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();
        var relation = BuildRelation(rolePublicId, permissionPublicId, roleActive: true, permissionActive: true);

        _repository.GetDeletedRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RolePublicId.Should().Be(rolePublicId);
        result.Data.PermissionPublicId.Should().Be(permissionPublicId);
        result.Data.RestoredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleIsInactiveButNotDeleted_ShouldReturnBusinessError()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();
        var relation = new RolePermission
        {
            RoleId = 15,
            PermissionId = 25,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = 1,
            Role = new Role
            {
                Id = 15,
                PublicId = rolePublicId,
                Name = "role-inactive-not-deleted",
                IsActive = false,
                IsDeleted = false,
            },
            Permission = new Permission
            {
                Id = 25,
                PublicId = permissionPublicId,
                Key = $"permissions.{Guid.NewGuid():N}",
                IsActive = true,
                IsDeleted = false,
            },
        };

        _repository.GetDeletedRelationByPublicIdsAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>())
            .Returns(relation);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(RolePermissionAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    private static RolePermission BuildRelation(Guid rolePublicId, Guid permissionPublicId, bool roleActive, bool permissionActive)
        => new()
        {
            RoleId = 10,
            PermissionId = 20,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = 1,
            Role = new Role
            {
                Id = 10,
                PublicId = rolePublicId,
                Name = "role-test",
                IsActive = roleActive,
                IsDeleted = !roleActive,
            },
            Permission = new Permission
            {
                Id = 20,
                PublicId = permissionPublicId,
                Key = $"permissions.{Guid.NewGuid():N}",
                IsActive = permissionActive,
                IsDeleted = !permissionActive,
            },
        };
}


