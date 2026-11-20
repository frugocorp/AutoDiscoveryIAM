using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId;

public interface IGetRolesByPublicIdRepository
{
    Task<bool> PermissionExistsAsync(Guid permissionPublicId, CancellationToken cancellationToken);
    Task<bool> PermissionIsActiveAsync(Guid permissionPublicId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GetRolesByPublicIdResponseDto>> GetRolesAsync(Guid permissionPublicId, CancellationToken cancellationToken);
}

public class GetRolesByPublicIdRepository(AutoDiscoveryIAMDbContext context) : IGetRolesByPublicIdRepository
{
    private readonly AutoDiscoveryIAMDbContext _context = context;

    public Task<bool> PermissionExistsAsync(Guid permissionPublicId, CancellationToken cancellationToken)
      => _context.Permissions.IgnoreQueryFilters().AnyAsync(permission => permission.PublicId == permissionPublicId, cancellationToken);

    public Task<bool> PermissionIsActiveAsync(Guid permissionPublicId, CancellationToken cancellationToken)
      => _context.Permissions.IgnoreQueryFilters().AnyAsync(permission => permission.PublicId == permissionPublicId && permission.IsActive && !permission.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<GetRolesByPublicIdResponseDto>> GetRolesAsync(Guid permissionPublicId, CancellationToken cancellationToken)
    {
        var roles = await _context.RolePermissions
          .Where(relation => relation.Permission.PublicId == permissionPublicId && !relation.IsDeleted)
          .Where(relation => relation.Permission.IsActive && !relation.Permission.IsDeleted)
          .Where(relation => relation.Role.IsActive && !relation.Role.IsDeleted)
          .OrderBy(relation => relation.Role.Name)
          .Select(relation => new GetRolesByPublicIdResponseDto(
            relation.Role.PublicId,
            relation.Role.Name,
            relation.Role.Description,
            relation.AssignedAt))
          .ToListAsync(cancellationToken);

        return roles.AsReadOnly();
    }
}
