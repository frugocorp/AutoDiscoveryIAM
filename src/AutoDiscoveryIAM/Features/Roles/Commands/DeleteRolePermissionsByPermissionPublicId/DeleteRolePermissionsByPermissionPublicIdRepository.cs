using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;

public interface IDeleteRolePermissionsByPermissionPublicIdRepository
{
    Task<RolePermission?> GetRelationByPublicIdsAsync(Guid rolePublicId, Guid permissionPublicId, CancellationToken cancellationToken);
}

public class DeleteRolePermissionsByPermissionPublicIdRepository(AutoDiscoveryIAMDbContext context) : IDeleteRolePermissionsByPermissionPublicIdRepository
{
    private readonly AutoDiscoveryIAMDbContext _context = context;

    public Task<RolePermission?> GetRelationByPublicIdsAsync(Guid rolePublicId, Guid permissionPublicId, CancellationToken cancellationToken)
      => _context.RolePermissions
        .IgnoreQueryFilters()
        .Include(relation => relation.Role)
        .Include(relation => relation.Permission)
        .FirstOrDefaultAsync(relation => relation.Role.PublicId == rolePublicId && relation.Permission.PublicId == permissionPublicId, cancellationToken);
}
