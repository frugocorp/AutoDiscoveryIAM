using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRolePermissionsByPermissionPublicId;

public interface IRestoreRolePermissionsByPermissionPublicIdRepository
{
    Task<RolePermission?> GetDeletedRelationByPublicIdsAsync(Guid rolePublicId, Guid permissionPublicId, CancellationToken cancellationToken);
}

public class RestoreRolePermissionsByPermissionPublicIdRepository(AutoDiscoveryIAMDbContext context) : IRestoreRolePermissionsByPermissionPublicIdRepository
{
    private readonly AutoDiscoveryIAMDbContext _context = context;

    public Task<RolePermission?> GetDeletedRelationByPublicIdsAsync(Guid rolePublicId, Guid permissionPublicId, CancellationToken cancellationToken)
      => _context.RolePermissions
        .IgnoreQueryFilters()
        .Include(relation => relation.Role)
        .Include(relation => relation.Permission)
        .FirstOrDefaultAsync(relation => relation.Role.PublicId == rolePublicId && relation.Permission.PublicId == permissionPublicId && relation.IsDeleted, cancellationToken);
}
