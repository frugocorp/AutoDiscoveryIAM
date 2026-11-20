using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.Seeds;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRolePermissionsByPermissionPublicId;

public interface IRestoreRolePermissionsByPermissionPublicIdService
{
    Task<OperationResult<RestoreRolePermissionsByPermissionPublicIdResponseDto>> ExecuteAsync(Guid rolePublicId, Guid permissionPublicId, CancellationToken cancellationToken, bool persistChanges = true);
}

public class RestoreRolePermissionsByPermissionPublicIdService(
    IRestoreRolePermissionsByPermissionPublicIdRepository repository,
    IAutoDiscoveryIAMUnitOfWork unitOfWork) : IRestoreRolePermissionsByPermissionPublicIdService
{
    private readonly IRestoreRolePermissionsByPermissionPublicIdRepository _repository = repository;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

    public async Task<OperationResult<RestoreRolePermissionsByPermissionPublicIdResponseDto>> ExecuteAsync(Guid rolePublicId, Guid permissionPublicId, CancellationToken cancellationToken, bool persistChanges = true)
    {
        var relation = await _repository.GetDeletedRelationByPublicIdsAsync(rolePublicId, permissionPublicId, cancellationToken);
        if (relation == null)
        {
            return OperationResult.Fail<RestoreRolePermissionsByPermissionPublicIdResponseDto>(
                ErrorInfo.Status404NotFound,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.DeletedRelationNotFound.Message, rolePublicId, permissionPublicId));
        }

        if (!relation.Role.IsActive || relation.Role.IsDeleted || !relation.Permission.IsActive || relation.Permission.IsDeleted)
        {
            return OperationResult.Fail<RestoreRolePermissionsByPermissionPublicIdResponseDto>(ErrorInfo.Status422Business, RolePermissionAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
        }

        if (relation.Role.IsDefaultForNewUser && (relation.Permission.RequiresDevelopmentAccess || relation.Permission.RequiresSuAdminAccess || PermissionSeeder.IsAdministrativeKey(relation.Permission.Key)))
        {
            return OperationResult.Fail<RestoreRolePermissionsByPermissionPublicIdResponseDto>(
                ErrorInfo.Status422Business,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.DefaultRoleCannotHaveElevatedPermission.Message, relation.Role.Name, relation.Permission.Key));
        }

        var now = DateTime.UtcNow;
        relation.IsDeleted = false;
        relation.DeletedAt = null;
        relation.DeletedBy = null;
        relation.RestoredAt = now;
        relation.RestoredBy = 1;
        relation.AssignedAt = now;
        relation.UpdatedAt = now;
        relation.UpdatedBy = 1;

        if (persistChanges)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return OperationResult.Ok(new RestoreRolePermissionsByPermissionPublicIdResponseDto(rolePublicId, permissionPublicId, now));
    }
}
