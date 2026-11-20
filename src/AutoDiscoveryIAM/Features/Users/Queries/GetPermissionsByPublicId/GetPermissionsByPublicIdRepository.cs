using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetPermissionsByPublicId;

public interface IGetPermissionsByPublicIdRepository
{
  Task<bool> UserExistsAsync(Guid userPublicId, CancellationToken cancellationToken);
  Task<bool> UserIsActiveAsync(Guid userPublicId, CancellationToken cancellationToken);
  Task<IReadOnlyList<GetPermissionsByPublicIdResponseDto>> GetPermissionsAsync(Guid userPublicId, CancellationToken cancellationToken);
}

public class GetPermissionsByPublicIdRepository(AutoDiscoveryIAMDbContext context) : IGetPermissionsByPublicIdRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public Task<bool> UserExistsAsync(Guid userPublicId, CancellationToken cancellationToken)
  {
    return _context.Users.IgnoreQueryFilters().AnyAsync(user => user.PublicId == userPublicId, cancellationToken);
  }

  public Task<bool> UserIsActiveAsync(Guid userPublicId, CancellationToken cancellationToken)
  {
    return _context.Users.IgnoreQueryFilters().AnyAsync(user => user.PublicId == userPublicId && user.IsActive && !user.IsDeleted, cancellationToken);
  }

  public async Task<IReadOnlyList<GetPermissionsByPublicIdResponseDto>> GetPermissionsAsync(Guid userPublicId, CancellationToken cancellationToken)
  {
    var rows = await _context.UserRoles
      .Where(userRole => userRole.User.PublicId == userPublicId && !userRole.IsDeleted && userRole.User.IsActive && !userRole.User.IsDeleted && userRole.Role.IsActive && !userRole.Role.IsDeleted)
      .SelectMany(userRole => userRole.Role.RolePermissions)
      .Where(rolePermission => !rolePermission.IsDeleted && rolePermission.Permission.IsActive && !rolePermission.Permission.IsDeleted)
      .Select(rolePermission => new
      {
        rolePermission.Permission.PublicId,
        rolePermission.Permission.Key,
        rolePermission.Permission.Description,
        rolePermission.Permission.Group,
      })
      .ToListAsync(cancellationToken);

    var permissions = rows
      .GroupBy(row => new { row.PublicId, row.Key, row.Description, row.Group })
      .Select(group => new GetPermissionsByPublicIdResponseDto(group.Key.PublicId, group.Key.Key, group.Key.Description, group.Key.Group))
      .OrderBy(permission => permission.Group)
      .ThenBy(permission => permission.Key)
      .ToList();

    return permissions.AsReadOnly();
  }
}
