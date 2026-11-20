using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId;

public interface IGetPermissionsByPublicIdService
{
  Task<OperationResult<IEnumerable<GetPermissionsByPublicIdResponseDto>>> ExecuteAsync(Guid userPublicId, CancellationToken cancellationToken);
}

public class GetPermissionsByPublicIdService(IGetPermissionsByPublicIdRepository repository) : IGetPermissionsByPublicIdService
{
  private readonly IGetPermissionsByPublicIdRepository _repository = repository;

  public async Task<OperationResult<IEnumerable<GetPermissionsByPublicIdResponseDto>>> ExecuteAsync(Guid userPublicId, CancellationToken cancellationToken)
  {
    var exists = await _repository.UserExistsAsync(userPublicId, cancellationToken);
    if (!exists)
    {
      return OperationResult.Fail<IEnumerable<GetPermissionsByPublicIdResponseDto>>(
        ErrorInfo.Status404NotFound,
        string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserNotFound.Message, userPublicId));
    }

    var isActive = await _repository.UserIsActiveAsync(userPublicId, cancellationToken);
    if (!isActive)
    {
      return OperationResult.Fail<IEnumerable<GetPermissionsByPublicIdResponseDto>>(
        ErrorInfo.Status422Business,
        string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserInactiveOrDeleted.Message, userPublicId));
    }

    return OperationResult.Ok<IEnumerable<GetPermissionsByPublicIdResponseDto>>(await _repository.GetPermissionsAsync(userPublicId, cancellationToken));
  }
}
