using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId;

public interface IGetPermissionsByPublicIdRepository
{
    Task<bool> RoleExistsAsync(Guid rolePublicId, CancellationToken cancellationToken);
    Task<bool> RoleIsActiveAsync(Guid rolePublicId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GetPermissionsByPublicIdResponseDto>> GetPermissionsAsync(Guid rolePublicId, CancellationToken cancellationToken);
}

public class GetPermissionsByPublicIdRepository(AutoDiscoveryIAMDbContext context) : IGetPermissionsByPublicIdRepository
{
    private readonly AutoDiscoveryIAMDbContext _context = context;

    public Task<bool> RoleExistsAsync(Guid rolePublicId, CancellationToken cancellationToken)
      => _context.Roles.IgnoreQueryFilters().AnyAsync(role => role.PublicId == rolePublicId, cancellationToken);

    public Task<bool> RoleIsActiveAsync(Guid rolePublicId, CancellationToken cancellationToken)
      => _context.Roles.IgnoreQueryFilters().AnyAsync(role => role.PublicId == rolePublicId && role.IsActive && !role.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<GetPermissionsByPublicIdResponseDto>> GetPermissionsAsync(Guid rolePublicId, CancellationToken cancellationToken)
    {
        var permissions = await _context.RolePermissions
          .Where(relation => relation.Role.PublicId == rolePublicId && !relation.IsDeleted)
          .Where(relation => relation.Role.IsActive && !relation.Role.IsDeleted)
          .Where(relation => relation.Permission.IsActive && !relation.Permission.IsDeleted)
          .OrderBy(relation => relation.Permission.Group)
          .ThenBy(relation => relation.Permission.Key)
          .Select(relation => new GetPermissionsByPublicIdResponseDto(
            relation.Permission.PublicId,
            relation.Permission.Key,
            relation.Permission.Description,
            relation.Permission.Group,
            relation.AssignedAt))
          .ToListAsync(cancellationToken);

        return permissions.AsReadOnly();
    }
}
