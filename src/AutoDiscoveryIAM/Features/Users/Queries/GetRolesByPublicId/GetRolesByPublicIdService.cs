using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetRolesByPublicId;

public interface IGetRolesByPublicIdService
{
  Task<OperationResult<IEnumerable<GetRolesByPublicIdResponseDto>>> ExecuteAsync(Guid userPublicId, CancellationToken cancellationToken);
}

public class GetRolesByPublicIdService(IGetRolesByPublicIdRepository repository) : IGetRolesByPublicIdService
{
  private readonly IGetRolesByPublicIdRepository _repository = repository;

  public async Task<OperationResult<IEnumerable<GetRolesByPublicIdResponseDto>>> ExecuteAsync(Guid userPublicId, CancellationToken cancellationToken)
  {
    var exists = await _repository.UserExistsAsync(userPublicId, cancellationToken);
    if (!exists)
    {
      return OperationResult.Fail<IEnumerable<GetRolesByPublicIdResponseDto>>(
        ErrorInfo.Status404NotFound,
        string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserNotFound.Message, userPublicId));
    }

    var isActive = await _repository.UserIsActiveAsync(userPublicId, cancellationToken);
    if (!isActive)
    {
      return OperationResult.Fail<IEnumerable<GetRolesByPublicIdResponseDto>>(
        ErrorInfo.Status422Business,
        string.Format(System.Globalization.CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserInactiveOrDeleted.Message, userPublicId));
    }

    return OperationResult.Ok<IEnumerable<GetRolesByPublicIdResponseDto>>(await _repository.GetRolesAsync(userPublicId, cancellationToken));
  }
}
