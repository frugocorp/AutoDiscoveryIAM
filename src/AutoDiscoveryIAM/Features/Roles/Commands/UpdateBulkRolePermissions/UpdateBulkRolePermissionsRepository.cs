using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;

public interface IUpdateBulkRolePermissionsRepository
{
  Task<Role?> GetActiveRoleByPublicIdAsync(Guid rolePublicId, CancellationToken cancellationToken);
  Task<IReadOnlyList<Permission>> GetActivePermissionsByPublicIdsAsync(IReadOnlyCollection<Guid> permissionPublicIds, CancellationToken cancellationToken);
  Task<IReadOnlyList<RolePermission>> GetVisibleRelationsByRoleIdAsync(long roleId, CancellationToken cancellationToken);
  Task<IReadOnlyList<RolePermission>> GetDeletedRelationsByRoleIdAndPermissionIdsAsync(long roleId, IReadOnlyCollection<long> permissionIds, CancellationToken cancellationToken);
}

public class UpdateBulkRolePermissionsRepository(AutoDiscoveryIAMDbContext context) : IUpdateBulkRolePermissionsRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public Task<Role?> GetActiveRoleByPublicIdAsync(Guid rolePublicId, CancellationToken cancellationToken)
    => _context.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(role => role.PublicId == rolePublicId && role.IsActive && !role.IsDeleted, cancellationToken);

  public async Task<IReadOnlyList<Permission>> GetActivePermissionsByPublicIdsAsync(IReadOnlyCollection<Guid> permissionPublicIds, CancellationToken cancellationToken)
  {
    var permissions = await _context.Permissions
      .IgnoreQueryFilters()
      .Where(permission => permissionPublicIds.Contains(permission.PublicId) && permission.IsActive && !permission.IsDeleted)
      .ToListAsync(cancellationToken);
    return permissions.AsReadOnly();
  }

  public async Task<IReadOnlyList<RolePermission>> GetVisibleRelationsByRoleIdAsync(long roleId, CancellationToken cancellationToken)
  {
    var relations = await _context.RolePermissions
      .Where(relation => relation.RoleId == roleId && !relation.IsDeleted)
      .Where(relation => relation.Permission.IsActive && !relation.Permission.IsDeleted)
      .Include(relation => relation.Permission)
      .ToListAsync(cancellationToken);
    return relations.AsReadOnly();
  }

  public async Task<IReadOnlyList<RolePermission>> GetDeletedRelationsByRoleIdAndPermissionIdsAsync(long roleId, IReadOnlyCollection<long> permissionIds, CancellationToken cancellationToken)
  {
    var relations = await _context.RolePermissions
      .IgnoreQueryFilters()
      .Where(relation => relation.RoleId == roleId && relation.IsDeleted && permissionIds.Contains(relation.PermissionId))
      .Where(relation => relation.Permission.IsActive && !relation.Permission.IsDeleted)
      .Include(relation => relation.Permission)
      .ToListAsync(cancellationToken);
    return relations.AsReadOnly();
  }
}
