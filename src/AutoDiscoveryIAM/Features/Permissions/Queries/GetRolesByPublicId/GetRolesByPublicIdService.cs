using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId;

public interface IGetRolesByPublicIdService
{
    Task<OperationResult<IEnumerable<GetRolesByPublicIdResponseDto>>> ExecuteAsync(Guid permissionPublicId, CancellationToken cancellationToken);
}

public class GetRolesByPublicIdService(IGetRolesByPublicIdRepository repository) : IGetRolesByPublicIdService
{
    private readonly IGetRolesByPublicIdRepository _repository = repository;

    public async Task<OperationResult<IEnumerable<GetRolesByPublicIdResponseDto>>> ExecuteAsync(Guid permissionPublicId, CancellationToken cancellationToken)
    {
        var exists = await _repository.PermissionExistsAsync(permissionPublicId, cancellationToken);
        if (!exists)
        {
            return OperationResult.Fail<IEnumerable<GetRolesByPublicIdResponseDto>>(
                ErrorInfo.Status404NotFound,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionNotFound.Message, permissionPublicId));
        }

        var isActive = await _repository.PermissionIsActiveAsync(permissionPublicId, cancellationToken);
        if (!isActive)
        {
            return OperationResult.Fail<IEnumerable<GetRolesByPublicIdResponseDto>>(
                ErrorInfo.Status422Business,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionInactiveOrDeleted.Message, permissionPublicId));
        }

        var data = await _repository.GetRolesAsync(permissionPublicId, cancellationToken);
        return OperationResult.Ok<IEnumerable<GetRolesByPublicIdResponseDto>>(data);
    }
}
