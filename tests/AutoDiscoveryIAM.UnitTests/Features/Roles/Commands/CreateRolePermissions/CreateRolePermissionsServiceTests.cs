using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.CreateRolePermissions;

public class CreateRolePermissionsServiceTests
{
    private readonly ICreateRolePermissionsRepository _repository;
    private readonly IRestoreRolePermissionsByPermissionPublicIdService _restoreService;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork;
    private readonly CreateRolePermissionsService _service;

    public CreateRolePermissionsServiceTests()
    {
        _repository = Substitute.For<ICreateRolePermissionsRepository>();
        _restoreService = Substitute.For<IRestoreRolePermissionsByPermissionPublicIdService>();
        _unitOfWork = Substitute.For<IAutoDiscoveryIAMUnitOfWork>();

        _service = new CreateRolePermissionsService(_repository, _restoreService, _unitOfWork);
    }

    /// <summary>
    /// El rol por defecto de los auto-registrados nunca recibe permisos que harían administrador a
    /// todo el que se registre: los elevados (Development/SuAdmin) ni, desde el issue #103, los de
    /// administración del paquete (que dejaron de exigir SuAdmin y pasaron a RBAC).
    /// </summary>
    [Theory]
    [InlineData("roles.create_role_permissions", false, false)]
    [InlineData("users.get_users", false, false)]
    [InlineData("users.update_user_type_admin", false, true)]
    [InlineData("permissions.sync_permissions", true, false)]
    public async Task ExecuteAsync_WhenRoleIsDefaultForNewUserAndPermissionWouldMakeEveryoneAdmin_ShouldReject(string key, bool requiresDevelopment, bool requiresSuAdmin)
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();
        var role = new Role { Id = 10, PublicId = rolePublicId, Name = "UserDefault", IsActive = true, IsDefaultForNewUser = true };
        var permission = new Permission
        {
            Id = 20,
            PublicId = permissionPublicId,
            Key = key,
            IsActive = true,
            RequiresDevelopmentAccess = requiresDevelopment,
            RequiresSuAdminAccess = requiresSuAdmin,
        };
        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetPermissionByPublicIdAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(permission);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.DefaultRoleCannotHaveElevatedPermission.Message, role.Name, key));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleIsDefaultForNewUserAndPermissionIsAnAppPermission_ShouldNotRejectIt()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();
        var role = new Role { Id = 10, PublicId = rolePublicId, Name = "UserDefault", IsActive = true, IsDefaultForNewUser = true };
        var permission = new Permission { Id = 20, PublicId = permissionPublicId, Key = "pedidos.crear_pedido", IsActive = true };
        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetPermissionByPublicIdAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(permission);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Error.Should().NotBe(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.DefaultRoleCannotHaveElevatedPermission.Message, role.Name, permission.Key));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleDoesNotExist_ShouldReturnNotFoundWithCentralizedMessage()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>())
            .Returns((Role?)null);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleNotFound.Message, rolePublicId));
    }

    [Fact]
    public async Task ExecuteAsync_WhenRelationAlreadyExists_ShouldReturnBusinessErrorWithCentralizedMessage()
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

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetPermissionByPublicIdAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(permission);
        _repository.GetRelationAsync(role.Id, permission.Id, Arg.Any<CancellationToken>())
            .Returns(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, IsDeleted = false });

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RelationAlreadyAssigned.Message, permissionPublicId, rolePublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDataIsValid_ShouldCreateRelationAndPersist()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        var role = new Role
        {
            Id = 100,
            PublicId = rolePublicId,
            Name = "role-ok",
            IsActive = true,
            IsDeleted = false,
        };

        var permission = new Permission
        {
            Id = 200,
            PublicId = permissionPublicId,
            Key = $"permissions.{Guid.NewGuid():N}",
            IsActive = true,
            IsDeleted = false,
        };

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetPermissionByPublicIdAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(permission);
        _repository.GetRelationAsync(role.Id, permission.Id, Arg.Any<CancellationToken>()).Returns((RolePermission?)null);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RolePublicId.Should().Be(rolePublicId);
        result.Data.PermissionPublicId.Should().Be(permissionPublicId);
        await _repository.Received(1).AddRelationAsync(
            Arg.Is<RolePermission>(relation => relation.RoleId == role.Id && relation.PermissionId == permission.Id && !relation.IsDeleted),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRoleIsInactiveOrDeleted_ShouldReturnBusinessError()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        var inactiveRole = new Role
        {
            Id = 11,
            PublicId = rolePublicId,
            Name = "role-inactive",
            IsActive = false,
            IsDeleted = false,
        };

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(inactiveRole);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleInactiveOrDeleted.Message, rolePublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPermissionDoesNotExist_ShouldReturnNotFound()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        var role = new Role
        {
            Id = 12,
            PublicId = rolePublicId,
            Name = "role-valid",
            IsActive = true,
            IsDeleted = false,
        };

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetPermissionByPublicIdAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns((Permission?)null);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status404NotFound);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionNotFound.Message, permissionPublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPermissionIsInactiveOrDeleted_ShouldReturnBusinessError()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        var role = new Role
        {
            Id = 13,
            PublicId = rolePublicId,
            Name = "role-valid2",
            IsActive = true,
            IsDeleted = false,
        };

        var deletedPermission = new Permission
        {
            Id = 21,
            PublicId = permissionPublicId,
            Key = $"permissions.{Guid.NewGuid():N}",
            IsActive = true,
            IsDeleted = true,
        };

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetPermissionByPublicIdAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(deletedPermission);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be(string.Format(CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionInactiveOrDeleted.Message, permissionPublicId));
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRelationIsDeleted_ShouldRestoreAndReturnSuccess()
    {
        var now = DateTime.UtcNow;
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        var role = new Role
        {
            Id = 14,
            PublicId = rolePublicId,
            Name = "role-restore",
            IsActive = true,
            IsDeleted = false,
        };

        var permission = new Permission
        {
            Id = 22,
            PublicId = permissionPublicId,
            Key = $"permissions.{Guid.NewGuid():N}",
            IsActive = true,
            IsDeleted = false,
        };

        var deletedRelation = new RolePermission { RoleId = role.Id, PermissionId = permission.Id, IsDeleted = true, CreatedAt = now };

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetPermissionByPublicIdAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(permission);
        _repository.GetRelationAsync(role.Id, permission.Id, Arg.Any<CancellationToken>()).Returns(deletedRelation);
        _restoreService.ExecuteAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>(), true)
            .Returns(OperationResult.Ok(new RestoreRolePermissionsByPermissionPublicIdResponseDto(rolePublicId, permissionPublicId, now)));

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RolePublicId.Should().Be(rolePublicId);
        result.Data.PermissionPublicId.Should().Be(permissionPublicId);
        await _restoreService.Received(1).ExecuteAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>(), true);
        await _repository.DidNotReceiveWithAnyArgs().AddRelationAsync(Arg.Any<RolePermission>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenRestoreFails_ShouldReturnError()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        var role = new Role
        {
            Id = 15,
            PublicId = rolePublicId,
            Name = "role-restore-fail",
            IsActive = true,
            IsDeleted = false,
        };

        var permission = new Permission
        {
            Id = 23,
            PublicId = permissionPublicId,
            Key = $"permissions.{Guid.NewGuid():N}",
            IsActive = true,
            IsDeleted = false,
        };

        var deletedRelation = new RolePermission { RoleId = role.Id, PermissionId = permission.Id, IsDeleted = true };

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetPermissionByPublicIdAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(permission);
        _repository.GetRelationAsync(role.Id, permission.Id, Arg.Any<CancellationToken>()).Returns(deletedRelation);
        _restoreService.ExecuteAsync(rolePublicId, permissionPublicId, Arg.Any<CancellationToken>(), true)
            .Returns(OperationResult.Fail<RestoreRolePermissionsByPermissionPublicIdResponseDto>(ErrorInfo.Status422Business, "restore failed"));

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorInfo.Should().Be(ErrorInfo.Status422Business);
        result.Error.Should().Be("restore failed");
    }

    [Fact]
    public async Task ExecuteAsync_WhenPersistChangesIsFalse_ShouldNotCallSaveChanges()
    {
        var rolePublicId = Guid.NewGuid();
        var permissionPublicId = Guid.NewGuid();

        var role = new Role
        {
            Id = 16,
            PublicId = rolePublicId,
            Name = "role-dry-run",
            IsActive = true,
            IsDeleted = false,
        };

        var permission = new Permission
        {
            Id = 24,
            PublicId = permissionPublicId,
            Key = $"permissions.{Guid.NewGuid():N}",
            IsActive = true,
            IsDeleted = false,
        };

        _repository.GetRoleByPublicIdAsync(rolePublicId, Arg.Any<CancellationToken>()).Returns(role);
        _repository.GetPermissionByPublicIdAsync(permissionPublicId, Arg.Any<CancellationToken>()).Returns(permission);
        _repository.GetRelationAsync(role.Id, permission.Id, Arg.Any<CancellationToken>()).Returns((RolePermission?)null);

        var result = await _service.ExecuteAsync(rolePublicId, permissionPublicId, CancellationToken.None, persistChanges: false);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        await _repository.Received(1).AddRelationAsync(Arg.Any<RolePermission>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
