using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;

public interface IGetMyPermissionsService
{
    Task<OperationResult<IEnumerable<GetMyPermissionsResponseDto>>> ExecuteAsync(Guid? userPublicId, CancellationToken cancellationToken);
}

public class GetMyPermissionsService(IGetMyPermissionsRepository repository) : IGetMyPermissionsService
{
    private readonly IGetMyPermissionsRepository _repository = repository;

    public async Task<OperationResult<IEnumerable<GetMyPermissionsResponseDto>>> ExecuteAsync(Guid? userPublicId, CancellationToken cancellationToken)
    {
        if (userPublicId == null)
        {
            return OperationResult.Fail<IEnumerable<GetMyPermissionsResponseDto>>(
              ErrorInfo.Status401Unauthorized,
              AuthorizationValidationTypes.Messages.InvalidUserContext);
        }

        var exists = await _repository.UserExistsAsync(userPublicId.Value, cancellationToken);
        if (!exists)
        {
            return OperationResult.Fail<IEnumerable<GetMyPermissionsResponseDto>>(
              ErrorInfo.Status404NotFound,
              string.Format(CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserNotFound.Message, userPublicId.Value));
        }

        var isActive = await _repository.UserIsActiveAsync(userPublicId.Value, cancellationToken);
        if (!isActive)
        {
            return OperationResult.Fail<IEnumerable<GetMyPermissionsResponseDto>>(
              ErrorInfo.Status422Business,
              string.Format(CultureInfo.InvariantCulture, UserRoleAssignmentValidationTypes.UserInactiveOrDeleted.Message, userPublicId.Value));
        }

        var permissions = await _repository.GetPermissionsAsync(userPublicId.Value, cancellationToken);
        return OperationResult.Ok<IEnumerable<GetMyPermissionsResponseDto>>(permissions);
    }
}