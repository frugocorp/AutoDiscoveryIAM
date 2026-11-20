using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;

public interface ICreateRolePermissionsRepository
{
  Task<Role?> GetRoleByPublicIdAsync(Guid rolePublicId, CancellationToken cancellationToken);
  Task<Permission?> GetPermissionByPublicIdAsync(Guid permissionPublicId, CancellationToken cancellationToken);
  Task<RolePermission?> GetRelationAsync(long roleId, long permissionId, CancellationToken cancellationToken);
  Task AddRelationAsync(RolePermission relation, CancellationToken cancellationToken);
}

public class CreateRolePermissionsRepository(AutoDiscoveryIAMDbContext context) : ICreateRolePermissionsRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public Task<Role?> GetRoleByPublicIdAsync(Guid rolePublicId, CancellationToken cancellationToken)
    => _context.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(role => role.PublicId == rolePublicId, cancellationToken);

  public Task<Permission?> GetPermissionByPublicIdAsync(Guid permissionPublicId, CancellationToken cancellationToken)
    => _context.Permissions.IgnoreQueryFilters().FirstOrDefaultAsync(permission => permission.PublicId == permissionPublicId, cancellationToken);

  public Task<RolePermission?> GetRelationAsync(long roleId, long permissionId, CancellationToken cancellationToken)
    => _context.RolePermissions.IgnoreQueryFilters().FirstOrDefaultAsync(relation => relation.RoleId == roleId && relation.PermissionId == permissionId, cancellationToken);

  public Task AddRelationAsync(RolePermission relation, CancellationToken cancellationToken)
  {
    _context.RolePermissions.Add(relation);
    return Task.CompletedTask;
  }
}
