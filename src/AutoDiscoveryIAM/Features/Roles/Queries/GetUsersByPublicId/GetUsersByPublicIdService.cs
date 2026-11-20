using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId;

public interface IGetUsersByPublicIdService
{
    Task<OperationResult<IEnumerable<GetUsersByPublicIdResponseDto>>> ExecuteAsync(Guid rolePublicId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}

public class GetUsersByPublicIdService(IGetUsersByPublicIdRepository repository) : IGetUsersByPublicIdService
{
    private readonly IGetUsersByPublicIdRepository _repository = repository;

    public async Task<OperationResult<IEnumerable<GetUsersByPublicIdResponseDto>>> ExecuteAsync(Guid rolePublicId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var exists = await _repository.RoleExistsAsync(rolePublicId, cancellationToken);
        if (!exists)
        {
            return OperationResult.Fail<IEnumerable<GetUsersByPublicIdResponseDto>>(
                ErrorInfo.Status404NotFound,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleNotFound.Message, rolePublicId));
        }

        var isActive = await _repository.RoleIsActiveAsync(rolePublicId, cancellationToken);
        if (!isActive)
        {
            return OperationResult.Fail<IEnumerable<GetUsersByPublicIdResponseDto>>(
                ErrorInfo.Status422Business,
                string.Format(System.Globalization.CultureInfo.InvariantCulture, RolePermissionAssignmentValidationTypes.RoleInactiveOrDeleted.Message, rolePublicId));
        }

        var data = await _repository.GetUsersAsync(rolePublicId, pageNumber, pageSize, cancellationToken);
        return OperationResult.Ok<IEnumerable<GetUsersByPublicIdResponseDto>>(data);
    }
}
