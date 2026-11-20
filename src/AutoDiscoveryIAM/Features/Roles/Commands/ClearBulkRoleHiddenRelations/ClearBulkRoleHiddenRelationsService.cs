using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;

public interface IClearBulkRoleHiddenRelationsService
{
    Task<OperationResult<ClearBulkRoleHiddenRelationsResponseDto>> ExecuteAsync(Guid rolePublicId, CancellationToken cancellationToken);
}

public class ClearBulkRoleHiddenRelationsService(
    IClearBulkRoleHiddenRelationsRepository repository,
    IAutoDiscoveryIAMUnitOfWork unitOfWork) : IClearBulkRoleHiddenRelationsService
{
    private readonly IClearBulkRoleHiddenRelationsRepository _repository = repository;
    private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;

    public async Task<OperationResult<ClearBulkRoleHiddenRelationsResponseDto>> ExecuteAsync(Guid rolePublicId, CancellationToken cancellationToken)
    {
        var role = await _repository.GetRoleByPublicIdAsync(rolePublicId, cancellationToken);
        if (role == null)
        {
            return OperationResult.Fail<ClearBulkRoleHiddenRelationsResponseDto>(
                ErrorInfo.Status404NotFound,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleNotFound.Message, rolePublicId));
        }

        var hiddenUserRelations = await _repository.GetHiddenUserRelationsAsync(role.Id, cancellationToken);
        var hiddenPermissionRelations = await _repository.GetHiddenPermissionRelationsAsync(role.Id, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var relation in hiddenUserRelations)
        {
            relation.IsDeleted = true;
            relation.DeletedAt = now;
            relation.DeletedBy = 1;
            relation.UpdatedAt = now;
            relation.UpdatedBy = 1;
        }

        foreach (var relation in hiddenPermissionRelations)
        {
            relation.IsDeleted = true;
            relation.DeletedAt = now;
            relation.DeletedBy = 1;
            relation.UpdatedAt = now;
            relation.UpdatedBy = 1;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new ClearBulkRoleHiddenRelationsResponseDto(
          RolePublicId: rolePublicId,
          ClearedUserRelations: hiddenUserRelations.Count,
          ClearedPermissionRelations: hiddenPermissionRelations.Count,
          TotalCleared: hiddenUserRelations.Count + hiddenPermissionRelations.Count,
          ExecutedAt: now);

        return OperationResult.Ok(response);
    }
}
