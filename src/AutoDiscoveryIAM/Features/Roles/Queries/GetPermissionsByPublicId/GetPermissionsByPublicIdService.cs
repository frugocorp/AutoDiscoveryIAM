using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId;

public interface IGetPermissionsByPublicIdService
{
    Task<OperationResult<IEnumerable<GetPermissionsByPublicIdResponseDto>>> ExecuteAsync(Guid rolePublicId, CancellationToken cancellationToken);
}

public class GetPermissionsByPublicIdService(IGetPermissionsByPublicIdRepository repository) : IGetPermissionsByPublicIdService
{
    private readonly IGetPermissionsByPublicIdRepository _repository = repository;

    public async Task<OperationResult<IEnumerable<GetPermissionsByPublicIdResponseDto>>> ExecuteAsync(Guid rolePublicId, CancellationToken cancellationToken)
    {
        var exists = await _repository.RoleExistsAsync(rolePublicId, cancellationToken);
        if (!exists)
        {
            return OperationResult.Fail<IEnumerable<GetPermissionsByPublicIdResponseDto>>(
                ErrorInfo.Status404NotFound,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleNotFound.Message, rolePublicId));
        }

        var isActive = await _repository.RoleIsActiveAsync(rolePublicId, cancellationToken);
        if (!isActive)
        {
            return OperationResult.Fail<IEnumerable<GetPermissionsByPublicIdResponseDto>>(
                ErrorInfo.Status422Business,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleInactiveOrDeleted.Message, rolePublicId));
        }

        var data = await _repository.GetPermissionsAsync(rolePublicId, cancellationToken);
        return OperationResult.Ok<IEnumerable<GetPermissionsByPublicIdResponseDto>>(data);
    }
}
