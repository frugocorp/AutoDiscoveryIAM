using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.ClearBulkRoleHiddenRelations;

public class ClearBulkRoleHiddenRelationsServiceTests
{
    private readonly IClearBulkRoleHiddenRelationsRepository _repository;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork;
    private readonly ClearBulkRoleHiddenRelationsService _service;

    public ClearBulkRoleHiddenRelationsServiceTests()
    {
        _repository = Substitute.For<IClearBulkRoleHiddenRelationsRepository>();
        _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();
        _service = new ClearBulkRoleHiddenRelationsService(_repository, _unitOfWork);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        var rolePublicId = Guid.NewGuid();
        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _service.ExecuteAsync(rolePublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleNotFound.Message, rolePublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleHasHiddenRelations_ShouldSoftDeleteAllAndPersist()
    {
        var rolePublicId = Guid.NewGuid();
        var role = new Role
        {
            Id = 10,
            PublicId = rolePublicId,
            Name = $"role-{Guid.NewGuid():N}",
            IsActive = true,
            IsDeleted = false,
        };

        var hiddenUserRelations = new List<UserRole>
        {
            new()
            {
                Id = 1,
                UserId = 100,
                RoleId = role.Id,
                IsDeleted = false,
                User = new User { Id = 100, PublicId = Guid.NewGuid(), Username = "u1", Email = "u1@test.local", PasswordHash = "hash", IsActive = false, IsDeleted = true },
                Role = role,
            },
            new()
            {
                Id = 2,
                UserId = 101,
                RoleId = role.Id,
                IsDeleted = false,
                User = new User { Id = 101, PublicId = Guid.NewGuid(), Username = "u2", Email = "u2@test.local", PasswordHash = "hash", IsActive = true, IsDeleted = false },
                Role = role,
            },
        };

        var hiddenPermissionRelations = new List<RolePermission>
        {
            new()
            {
                Id = 11,
                RoleId = role.Id,
                PermissionId = 200,
                IsDeleted = false,
                Role = role,
                Permission = new Permission { Id = 200, PublicId = Guid.NewGuid(), Key = $"permissions.hidden.{Guid.NewGuid():N}", IsActive = false, IsDeleted = true },
            },
        };

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetHiddenUserRelationsAsync(role.Id, Arg.Any<CancellationToken>()).Returns(hiddenUserRelations);
        _repository.GetHiddenPermissionRelationsAsync(role.Id, Arg.Any<CancellationToken>()).Returns(hiddenPermissionRelations);

        var result = await _service.ExecuteAsync(rolePublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RolePublicId.Should().Be(rolePublicId);
        result.Data.ClearedUserRelations.Should().Be(2);
        result.Data.ClearedPermissionRelations.Should().Be(1);
        result.Data.TotalCleared.Should().Be(3);

        hiddenUserRelations.All(r => r.IsDeleted).Should().BeTrue("todas las relaciones de usuario deben estar eliminadas");
        hiddenPermissionRelations.All(r => r.IsDeleted).Should().BeTrue("todas las relaciones de permiso deben estar eliminadas");
        hiddenUserRelations.All(r => r.DeletedBy == 1).Should().BeTrue("DeletedBy de relaciones de usuario debe estar asignado");
        hiddenPermissionRelations.All(r => r.DeletedBy == 1).Should().BeTrue("DeletedBy de relaciones de permiso debe estar asignado");

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleHasNoHiddenRelations_ShouldReturnSuccessWithZeroCounters()
    {
        var rolePublicId = Guid.NewGuid();
        var role = new Role
        {
            Id = 500,
            PublicId = rolePublicId,
            Name = $"role-{Guid.NewGuid():N}",
            IsActive = true,
            IsDeleted = false,
        };

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetHiddenUserRelationsAsync(role.Id, Arg.Any<CancellationToken>()).Returns([]);
        _repository.GetHiddenPermissionRelationsAsync(role.Id, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _service.ExecuteAsync(rolePublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ClearedUserRelations.Should().Be(0);
        result.Data.ClearedPermissionRelations.Should().Be(0);
        result.Data.TotalCleared.Should().Be(0);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenOnlyHiddenUserRelationsExist_ShouldClearOnlyUserRelationsAndUpdateCounters()
    {
        var rolePublicId = Guid.NewGuid();
        var role = new Role
        {
            Id = 11,
            PublicId = rolePublicId,
            Name = $"role-user-only-{Guid.NewGuid():N}",
            IsActive = true,
            IsDeleted = false,
        };

        var hiddenUserRelations = new List<UserRole>
        {
            new()
            {
                Id = 3,
                UserId = 102,
                RoleId = role.Id,
                IsDeleted = false,
                User = new User { Id = 102, PublicId = Guid.NewGuid(), Username = "u3", Email = "u3@test.local", PasswordHash = "hash", IsActive = false, IsDeleted = false },
                Role = role,
            },
        };

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetHiddenUserRelationsAsync(role.Id, Arg.Any<CancellationToken>()).Returns(hiddenUserRelations);
        _repository.GetHiddenPermissionRelationsAsync(role.Id, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _service.ExecuteAsync(rolePublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.ClearedUserRelations.Should().Be(1);
        result.Data.ClearedPermissionRelations.Should().Be(0);
        result.Data.TotalCleared.Should().Be(1);
        hiddenUserRelations[0].IsDeleted.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenOnlyHiddenPermissionRelationsExist_ShouldClearOnlyPermissionRelationsAndUpdateCounters()
    {
        var rolePublicId = Guid.NewGuid();
        var role = new Role
        {
            Id = 12,
            PublicId = rolePublicId,
            Name = $"role-perm-only-{Guid.NewGuid():N}",
            IsActive = true,
            IsDeleted = false,
        };

        var hiddenPermissionRelations = new List<RolePermission>
        {
            new()
            {
                Id = 12,
                RoleId = role.Id,
                PermissionId = 201,
                IsDeleted = false,
                Role = role,
                Permission = new Permission { Id = 201, PublicId = Guid.NewGuid(), Key = $"permissions.perm-hidden.{Guid.NewGuid():N}", IsActive = true, IsDeleted = true },
            },
            new()
            {
                Id = 13,
                RoleId = role.Id,
                PermissionId = 202,
                IsDeleted = false,
                Role = role,
                Permission = new Permission { Id = 202, PublicId = Guid.NewGuid(), Key = $"permissions.perm-hidden2.{Guid.NewGuid():N}", IsActive = false, IsDeleted = false },
            },
        };

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetHiddenUserRelationsAsync(role.Id, Arg.Any<CancellationToken>()).Returns([]);
        _repository.GetHiddenPermissionRelationsAsync(role.Id, Arg.Any<CancellationToken>()).Returns(hiddenPermissionRelations);

        var result = await _service.ExecuteAsync(rolePublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data!.ClearedUserRelations.Should().Be(0);
        result.Data.ClearedPermissionRelations.Should().Be(2);
        result.Data.TotalCleared.Should().Be(2);
        hiddenPermissionRelations.All(r => r.IsDeleted).Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAssignDeletedAtAndDeletedByTimestampsCorrectly()
    {
        var rolePublicId = Guid.NewGuid();
        var role = new Role
        {
            Id = 13,
            PublicId = rolePublicId,
            Name = $"role-timestamps-{Guid.NewGuid():N}",
            IsActive = true,
            IsDeleted = false,
        };

        var beforeExecution = DateTime.UtcNow;

        var hiddenUserRelations = new List<UserRole>
        {
            new()
            {
                Id = 4,
                UserId = 103,
                RoleId = role.Id,
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null,
                UpdatedAt = beforeExecution.AddMinutes(-1),
                UpdatedBy = 1,
                User = new User { Id = 103, PublicId = Guid.NewGuid(), Username = "u4", Email = "u4@test.local", PasswordHash = "hash", IsActive = false, IsDeleted = false },
                Role = role,
            },
        };

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetHiddenUserRelationsAsync(role.Id, Arg.Any<CancellationToken>()).Returns(hiddenUserRelations);
        _repository.GetHiddenPermissionRelationsAsync(role.Id, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _service.ExecuteAsync(rolePublicId, CancellationToken.None);

        var afterExecution = DateTime.UtcNow;
        var relation = hiddenUserRelations[0];

        result.Success.Should().BeTrue();
        relation.IsDeleted.Should().BeTrue();
        relation.DeletedAt.Should().NotBeNull();
        relation.DeletedAt!.Value.Should().BeGreaterThanOrEqualTo(beforeExecution);
        // Verify timestamps are assigned by service
        relation.DeletedBy.Should().Be(1);
        relation.UpdatedAt.Should().BeGreaterThanOrEqualTo(beforeExecution);
        relation.UpdatedBy.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHiddenRelationsAreLarge_ShouldProcessAllAndCalculateCountersCorrectly()
    {
        var rolePublicId = Guid.NewGuid();
        var role = new Role
        {
            Id = 15,
            PublicId = rolePublicId,
            Name = $"role-large-{Guid.NewGuid():N}",
            IsActive = true,
            IsDeleted = false,
        };

        var hiddenUserRelations = Enumerable.Range(1, 50)
            .Select(i => new UserRole
            {
                Id = i,
                UserId = 200 + i,
                RoleId = role.Id,
                IsDeleted = false,
                User = new User { Id = 200 + i, PublicId = Guid.NewGuid(), Username = $"user-{i}", Email = $"u{i}@test.local", PasswordHash = "hash", IsActive = i % 2 == 0, IsDeleted = i % 3 == 0 },
                Role = role,
            })
            .ToList();

        var hiddenPermissionRelations = Enumerable.Range(1, 75)
            .Select(i => new RolePermission
            {
                Id = 100 + i,
                RoleId = role.Id,
                PermissionId = 300 + i,
                IsDeleted = false,
                Role = role,
                Permission = new Permission { Id = 300 + i, PublicId = Guid.NewGuid(), Key = $"permissions.bulk.{i}", IsActive = i % 2 == 0, IsDeleted = i % 5 == 0 },
            })
            .ToList();

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetHiddenUserRelationsAsync(role.Id, Arg.Any<CancellationToken>()).Returns(hiddenUserRelations);
        _repository.GetHiddenPermissionRelationsAsync(role.Id, Arg.Any<CancellationToken>()).Returns(hiddenPermissionRelations);

        var result = await _service.ExecuteAsync(rolePublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.ClearedUserRelations.Should().Be(50);
        result.Data.ClearedPermissionRelations.Should().Be(75);
        result.Data.TotalCleared.Should().Be(125);
        hiddenUserRelations.All(r => r.IsDeleted).Should().BeTrue();
        hiddenPermissionRelations.All(r => r.IsDeleted).Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}


