using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RestoreUserRolesByPublicId;

public interface IRestoreUserRolesByPublicIdRepository
{
    Task<UserRole?> GetDeletedRelationByPublicIdsAsync(Guid userPublicId, Guid rolePublicId, CancellationToken cancellationToken);
}

public class RestoreUserRolesByPublicIdRepository(AutoDiscoveryIAMDbContext context) : IRestoreUserRolesByPublicIdRepository
{
    private readonly AutoDiscoveryIAMDbContext _context = context;

    public Task<UserRole?> GetDeletedRelationByPublicIdsAsync(Guid userPublicId, Guid rolePublicId, CancellationToken cancellationToken)
    {
        return _context.UserRoles
          .IgnoreQueryFilters()
          .Include(userRole => userRole.User)
          .Include(userRole => userRole.Role)
          .FirstOrDefaultAsync(userRole => userRole.User.PublicId == userPublicId && userRole.Role.PublicId == rolePublicId && userRole.IsDeleted, cancellationToken);
    }
}
