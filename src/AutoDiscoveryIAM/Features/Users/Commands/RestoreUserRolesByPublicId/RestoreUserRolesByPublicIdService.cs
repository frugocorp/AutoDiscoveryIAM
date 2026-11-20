using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RestoreUserRolesByPublicId;

public interface IRestoreUserRolesByPublicIdService
{
    Task<OperationResult<RestoreUserRolesByPublicIdResponseDto>> ExecuteAsync(Guid userPublicId, Guid rolePublicId, CancellationToken cancellationToken, bool persistChanges = true);
}

public class RestoreUserRolesByPublicIdService(
    IRestoreUserRolesByPublicIdRepository repository,
    IAutoDiscoveryIAMUnitOfWork unitOfWork) : IRestoreUserRolesByPublicIdService
{
    private readonly IRestoreUserRolesByPublicIdRepository _repository = repository;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

    public async Task<OperationResult<RestoreUserRolesByPublicIdResponseDto>> ExecuteAsync(Guid userPublicId, Guid rolePublicId, CancellationToken cancellationToken, bool persistChanges = true)
    {
        var relation = await _repository.GetDeletedRelationByPublicIdsAsync(userPublicId, rolePublicId, cancellationToken);
        if (relation == null)
        {
            return OperationResult.Fail<RestoreUserRolesByPublicIdResponseDto>(
              ErrorInfo.Status404NotFound,
              string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.DeletedRelationNotFound.Message, userPublicId, rolePublicId));
        }

        if (!relation.User.IsActive || relation.User.IsDeleted || !relation.Role.IsActive || relation.Role.IsDeleted)
        {
            return OperationResult.Fail<RestoreUserRolesByPublicIdResponseDto>(ErrorInfo.Status422Business, UserRoleAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
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

        return OperationResult.Ok(new RestoreUserRolesByPublicIdResponseDto(userPublicId, rolePublicId, now));
    }
}
