using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetUsersByPublicId;

public interface IGetUsersByPublicIdService
{
    Task<OperationResult<IEnumerable<GetUsersByPublicIdResponseDto>>> ExecuteAsync(Guid permissionPublicId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}

public class GetUsersByPublicIdService(IGetUsersByPublicIdRepository repository) : IGetUsersByPublicIdService
{
    private readonly IGetUsersByPublicIdRepository _repository = repository;

    public async Task<OperationResult<IEnumerable<GetUsersByPublicIdResponseDto>>> ExecuteAsync(Guid permissionPublicId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var exists = await _repository.PermissionExistsAsync(permissionPublicId, cancellationToken);
        if (!exists)
        {
            return OperationResult.Fail<IEnumerable<GetUsersByPublicIdResponseDto>>(
                ErrorInfo.Status404NotFound,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionNotFound.Message, permissionPublicId));
        }

        var isActive = await _repository.PermissionIsActiveAsync(permissionPublicId, cancellationToken);
        if (!isActive)
        {
            return OperationResult.Fail<IEnumerable<GetUsersByPublicIdResponseDto>>(
                ErrorInfo.Status422Business,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.PermissionInactiveOrDeleted.Message, permissionPublicId));
        }

        var data = await _repository.GetUsersAsync(permissionPublicId, pageNumber, pageSize, cancellationToken);
        return OperationResult.Ok<IEnumerable<GetUsersByPublicIdResponseDto>>(data);
    }
}
