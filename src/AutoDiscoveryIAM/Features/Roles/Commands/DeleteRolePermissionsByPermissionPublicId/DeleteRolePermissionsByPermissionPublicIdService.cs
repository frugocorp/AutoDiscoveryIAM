using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;

public interface IDeleteRolePermissionsByPermissionPublicIdService
{
    Task<OperationResult<DeleteRolePermissionsByPermissionPublicIdResponseDto>> ExecuteAsync(Guid rolePublicId, Guid permissionPublicId, CancellationToken cancellationToken, bool persistChanges = true);
}

public class DeleteRolePermissionsByPermissionPublicIdService(
    IDeleteRolePermissionsByPermissionPublicIdRepository repository,
    IAutoDiscoveryIAMUnitOfWork unitOfWork) : IDeleteRolePermissionsByPermissionPublicIdService
{
    private readonly IDeleteRolePermissionsByPermissionPublicIdRepository _repository = repository;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

    public async Task<OperationResult<DeleteRolePermissionsByPermissionPublicIdResponseDto>> ExecuteAsync(Guid rolePublicId, Guid permissionPublicId, CancellationToken cancellationToken, bool persistChanges = true)
    {
        var relation = await _repository.GetRelationByPublicIdsAsync(rolePublicId, permissionPublicId, cancellationToken);
        if (relation == null || relation.IsDeleted)
        {
            return OperationResult.Fail<DeleteRolePermissionsByPermissionPublicIdResponseDto>(
                ErrorInfo.Status404NotFound,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.ActiveRelationNotFound.Message, rolePublicId, permissionPublicId));
        }

        if (!relation.Role.IsActive || relation.Role.IsDeleted || !relation.Permission.IsActive || relation.Permission.IsDeleted)
        {
            return OperationResult.Fail<DeleteRolePermissionsByPermissionPublicIdResponseDto>(ErrorInfo.Status422Business, RolePermissionAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
        }

        var now = DateTime.UtcNow;
        relation.IsDeleted = true;
        relation.DeletedAt = now;
        relation.DeletedBy = 1;
        relation.UpdatedAt = now;
        relation.UpdatedBy = 1;

        if (persistChanges)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return OperationResult.Ok(new DeleteRolePermissionsByPermissionPublicIdResponseDto(rolePublicId, permissionPublicId, now));
    }
}
