using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;

public interface IClearBulkRoleHiddenRelationsRepository
{
  Task<Role?> GetRoleByPublicIdAsync(Guid rolePublicId, CancellationToken cancellationToken);
  Task<IReadOnlyList<UserRole>> GetHiddenUserRelationsAsync(long roleId, CancellationToken cancellationToken);
  Task<IReadOnlyList<RolePermission>> GetHiddenPermissionRelationsAsync(long roleId, CancellationToken cancellationToken);
}

public class ClearBulkRoleHiddenRelationsRepository(AutoDiscoveryIAMDbContext context) : IClearBulkRoleHiddenRelationsRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public Task<Role?> GetRoleByPublicIdAsync(Guid rolePublicId, CancellationToken cancellationToken)
    => _context.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(role => role.PublicId == rolePublicId, cancellationToken);

  public async Task<IReadOnlyList<UserRole>> GetHiddenUserRelationsAsync(long roleId, CancellationToken cancellationToken)
  {
    var relations = await _context.UserRoles
      .IgnoreQueryFilters()
      .Where(relation => relation.RoleId == roleId && !relation.IsDeleted)
      .Where(relation => !relation.User.IsActive || relation.User.IsDeleted)
      .Include(relation => relation.User)
      .ToListAsync(cancellationToken);
    return relations.AsReadOnly();
  }

  public async Task<IReadOnlyList<RolePermission>> GetHiddenPermissionRelationsAsync(long roleId, CancellationToken cancellationToken)
  {
    var relations = await _context.RolePermissions
      .IgnoreQueryFilters()
      .Where(relation => relation.RoleId == roleId && !relation.IsDeleted)
      .Where(relation => !relation.Permission.IsActive || relation.Permission.IsDeleted)
      .Include(relation => relation.Permission)
      .ToListAsync(cancellationToken);
    return relations.AsReadOnly();
  }
}
