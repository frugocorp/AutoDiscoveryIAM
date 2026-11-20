using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;

public interface IGetMyPermissionsRepository
{
    Task<bool> UserExistsAsync(Guid userPublicId, CancellationToken cancellationToken);
    Task<bool> UserIsActiveAsync(Guid userPublicId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GetMyPermissionsResponseDto>> GetPermissionsAsync(Guid userPublicId, CancellationToken cancellationToken);
}

public class GetMyPermissionsRepository(IGetPermissionsByPublicIdRepository repository) : IGetMyPermissionsRepository
{
    private readonly IGetPermissionsByPublicIdRepository _repository = repository;

    public Task<bool> UserExistsAsync(Guid userPublicId, CancellationToken cancellationToken)
    {
        return _repository.UserExistsAsync(userPublicId, cancellationToken);
    }

    public Task<bool> UserIsActiveAsync(Guid userPublicId, CancellationToken cancellationToken)
    {
        return _repository.UserIsActiveAsync(userPublicId, cancellationToken);
    }

    public async Task<IReadOnlyList<GetMyPermissionsResponseDto>> GetPermissionsAsync(Guid userPublicId, CancellationToken cancellationToken)
    {
        var permissions = await _repository.GetPermissionsAsync(userPublicId, cancellationToken);
        return permissions
          .Select(permission => new GetMyPermissionsResponseDto(permission.PublicId, permission.Key, permission.Description, permission.Group))
          .ToList()
          .AsReadOnly();
    }
}