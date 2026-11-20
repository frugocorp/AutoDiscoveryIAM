using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;

public interface ICreateRolePermissionsService
{
    Task<OperationResult<CreateRolePermissionsResponseDto>> ExecuteAsync(Guid rolePublicId, Guid permissionPublicId, CancellationToken cancellationToken, bool persistChanges = true);
}

public class CreateRolePermissionsService(
    ICreateRolePermissionsRepository repository,
    IRestoreRolePermissionsByPermissionPublicIdService restoreService,
    IAutoDiscoveryIAMUnitOfWork unitOfWork) : ICreateRolePermissionsService
{
    private readonly ICreateRolePermissionsRepository _repository = repository;
    private readonly IRestoreRolePermissionsByPermissionPublicIdService _restoreService = restoreService;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

    public async Task<OperationResult<CreateRolePermissionsResponseDto>> ExecuteAsync(Guid rolePublicId, Guid permissionPublicId, CancellationToken cancellationToken, bool persistChanges = true)
    {
        var roleResult = await ValidateRoleAsync(rolePublicId, cancellationToken);
        if (roleResult.IsFailure)
        {
            return roleResult.Error!;
        }

        var permissionResult = await ValidatePermissionAsync(permissionPublicId, cancellationToken);
        if (permissionResult.IsFailure)
        {
            return permissionResult.Error!;
        }

        var role = roleResult.Value!;
        var permission = permissionResult.Value!;

        if (role.IsDefaultForNewUser && (permission.RequiresDevelopmentAccess || permission.RequiresSuAdminAccess || PermissionSeeder.IsAdministrativeKey(permission.Key)))
        {
            return OperationResult.Fail<CreateRolePermissionsResponseDto>(
                ErrorInfo.Status422Business,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.DefaultRoleCannotHaveElevatedPermission.Message, role.Name, permission.Key));
        }

        return await AssignPermissionToRoleAsync(role, permission, rolePublicId, permissionPublicId, persistChanges, cancellationToken);
    }

    private async Task<ValidationResult<Role>> ValidateRoleAsync(Guid rolePublicId, CancellationToken cancellationToken)
    {
        var role = await _repository.GetRoleByPublicIdAsync(rolePublicId, cancellationToken);
        if (role == null)
        {
            return ValidationResult<Role>.Fail(OperationResult.Fail<CreateRolePermissionsResponseDto>(
                ErrorInfo.Status404NotFound,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleNotFound.Message, rolePublicId)));
        }

        if (!role.IsActive || role.IsDeleted)
        {
            return ValidationResult<Role>.Fail(OperationResult.Fail<CreateRolePermissionsResponseDto>(
                ErrorInfo.Status422Business,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleInactiveOrDeleted.Message, rolePublicId)));
        }

        return ValidationResult<Role>.Ok(role);
    }

    private async Task<ValidationResult<Permission>> ValidatePermissionAsync(Guid permissionPublicId, CancellationToken cancellationToken)
    {
        var permission = await _repository.GetPermissionByPublicIdAsync(permissionPublicId, cancellationToken);
        if (permission == null)
        {
            return ValidationResult<Permission>.Fail(OperationResult.Fail<CreateRolePermissionsResponseDto>(
                ErrorInfo.Status404NotFound,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionNotFound.Message, permissionPublicId)));
        }

        if (!permission.IsActive || permission.IsDeleted)
        {
            return ValidationResult<Permission>.Fail(OperationResult.Fail<CreateRolePermissionsResponseDto>(
                ErrorInfo.Status422Business,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionInactiveOrDeleted.Message, permissionPublicId)));
        }

        return ValidationResult<Permission>.Ok(permission);
    }

    private async Task<OperationResult<CreateRolePermissionsResponseDto>> AssignPermissionToRoleAsync(
      Role role, Permission permission, Guid rolePublicId, Guid permissionPublicId,
      bool persistChanges, CancellationToken cancellationToken)
    {
        var relation = await _repository.GetRelationAsync(role.Id, permission.Id, cancellationToken);

        if (relation != null && !relation.IsDeleted)
        {
            return OperationResult.Fail<CreateRolePermissionsResponseDto>(
                ErrorInfo.Status422Business,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RelationAlreadyAssigned.Message, permissionPublicId, rolePublicId));
        }

        if (relation != null && relation.IsDeleted)
        {
            var restoreResult = await _restoreService.ExecuteAsync(rolePublicId, permissionPublicId, cancellationToken, persistChanges);
            if (!restoreResult.Success)
            {
                return OperationResult.Fail<CreateRolePermissionsResponseDto>(restoreResult.ErrorInfo!, restoreResult.Error!);
            }

            return OperationResult.Ok(new CreateRolePermissionsResponseDto(rolePublicId, permissionPublicId, restoreResult.Data!.RestoredAt));
        }

        return await CreateNewRelationAsync(role.Id, permission.Id, rolePublicId, permissionPublicId, persistChanges, cancellationToken);
    }

    private async Task<OperationResult<CreateRolePermissionsResponseDto>> CreateNewRelationAsync(
            long roleId, long permissionId, Guid rolePublicId, Guid permissionPublicId,
      bool persistChanges, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await _repository.AddRelationAsync(new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            AssignedAt = now,
            CreatedAt = now,
            CreatedBy = 1,
        }, cancellationToken);

        if (persistChanges)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return OperationResult.Ok(new CreateRolePermissionsResponseDto(rolePublicId, permissionPublicId, now));
    }

    private sealed class ValidationResult<T>
    {
        public bool IsFailure { get; }
        public T? Value { get; }
        public OperationResult<CreateRolePermissionsResponseDto>? Error { get; }

        private ValidationResult(T value) { Value = value; IsFailure = false; }
        private ValidationResult(OperationResult<CreateRolePermissionsResponseDto> error) { Error = error; IsFailure = true; }

        public static ValidationResult<T> Ok(T value) => new(value);
        public static ValidationResult<T> Fail(OperationResult<CreateRolePermissionsResponseDto> error) => new(error);
    }
}
