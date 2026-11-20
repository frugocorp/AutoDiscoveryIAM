using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetUsersByPublicId;

public interface IGetUsersByPublicIdRepository
{
    Task<bool> PermissionExistsAsync(Guid permissionPublicId, CancellationToken cancellationToken);
    Task<bool> PermissionIsActiveAsync(Guid permissionPublicId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GetUsersByPublicIdResponseDto>> GetUsersAsync(Guid permissionPublicId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}

public class GetUsersByPublicIdRepository(AutoDiscoveryIAMDbContext context) : IGetUsersByPublicIdRepository
{
    private readonly AutoDiscoveryIAMDbContext _context = context;

    public Task<bool> PermissionExistsAsync(Guid permissionPublicId, CancellationToken cancellationToken)
      => _context.Permissions.IgnoreQueryFilters().AnyAsync(permission => permission.PublicId == permissionPublicId, cancellationToken);

    public Task<bool> PermissionIsActiveAsync(Guid permissionPublicId, CancellationToken cancellationToken)
      => _context.Permissions.IgnoreQueryFilters().AnyAsync(permission => permission.PublicId == permissionPublicId && permission.IsActive && !permission.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<GetUsersByPublicIdResponseDto>> GetUsersAsync(Guid permissionPublicId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var users = await _context.RolePermissions
          .Where(rolePermission => rolePermission.Permission.PublicId == permissionPublicId && !rolePermission.IsDeleted)
          .Where(rolePermission => rolePermission.Permission.IsActive && !rolePermission.Permission.IsDeleted)
          .Where(rolePermission => rolePermission.Role.IsActive && !rolePermission.Role.IsDeleted)
          .SelectMany(rolePermission => rolePermission.Role.UserRoles
            .Where(userRole => !userRole.IsDeleted && userRole.User.IsActive && !userRole.User.IsDeleted)
            .Select(userRole => new
            {
                userRole.User.PublicId,
                userRole.User.Username,
                userRole.User.Email,
                userRole.AssignedAt,
            }))
          .Distinct()
          .OrderBy(item => item.Username)
          .Select(item => new GetUsersByPublicIdResponseDto(
            item.PublicId,
            item.Username,
            item.Email,
            item.AssignedAt))
          .Skip((pageNumber - 1) * pageSize)
          .Take(pageSize)
          .ToListAsync(cancellationToken);

        return users.AsReadOnly();
    }
}
