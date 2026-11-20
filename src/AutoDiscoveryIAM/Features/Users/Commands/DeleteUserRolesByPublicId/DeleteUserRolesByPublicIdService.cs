using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;

public interface IDeleteUserRolesByPublicIdService
{
    Task<OperationResult<DeleteUserRolesByPublicIdResponseDto>> ExecuteAsync(Guid userPublicId, Guid rolePublicId, CancellationToken cancellationToken, bool persistChanges = true);
}

public class DeleteUserRolesByPublicIdService(
    IDeleteUserRolesByPublicIdRepository repository,
    IAutoDiscoveryIAMUnitOfWork unitOfWork) : IDeleteUserRolesByPublicIdService
{
    private readonly IDeleteUserRolesByPublicIdRepository _repository = repository;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

    public async Task<OperationResult<DeleteUserRolesByPublicIdResponseDto>> ExecuteAsync(Guid userPublicId, Guid rolePublicId, CancellationToken cancellationToken, bool persistChanges = true)
    {
        var relation = await _repository.GetRelationByPublicIdsAsync(userPublicId, rolePublicId, cancellationToken);
        if (relation == null || relation.IsDeleted)
        {
            return OperationResult.Fail<DeleteUserRolesByPublicIdResponseDto>(
              ErrorInfo.Status404NotFound,
              string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.ActiveRelationNotFound.Message, userPublicId, rolePublicId));
        }

        if (!relation.User.IsActive || relation.User.IsDeleted || !relation.Role.IsActive || relation.Role.IsDeleted)
        {
            return OperationResult.Fail<DeleteUserRolesByPublicIdResponseDto>(ErrorInfo.Status422Business, UserRoleAssignmentValidationTypes.RelationCannotBeModifiedInactive.Message);
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

        return OperationResult.Ok(new DeleteUserRolesByPublicIdResponseDto(userPublicId, rolePublicId, now));
    }
}
