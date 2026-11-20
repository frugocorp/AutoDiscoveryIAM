using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId;

public interface IGetUsersByPublicIdRepository
{
    Task<bool> RoleExistsAsync(Guid rolePublicId, CancellationToken cancellationToken);
    Task<bool> RoleIsActiveAsync(Guid rolePublicId, CancellationToken cancellationToken);
    Task<IReadOnlyList<GetUsersByPublicIdResponseDto>> GetUsersAsync(Guid rolePublicId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}

public class GetUsersByPublicIdRepository(AutoDiscoveryIAMDbContext context) : IGetUsersByPublicIdRepository
{
    private readonly AutoDiscoveryIAMDbContext _context = context;

    public Task<bool> RoleExistsAsync(Guid rolePublicId, CancellationToken cancellationToken)
      => _context.Roles.IgnoreQueryFilters().AnyAsync(role => role.PublicId == rolePublicId, cancellationToken);

    public Task<bool> RoleIsActiveAsync(Guid rolePublicId, CancellationToken cancellationToken)
      => _context.Roles.IgnoreQueryFilters().AnyAsync(role => role.PublicId == rolePublicId && role.IsActive && !role.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<GetUsersByPublicIdResponseDto>> GetUsersAsync(Guid rolePublicId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var users = await _context.UserRoles
          .Where(relation => relation.Role.PublicId == rolePublicId && !relation.IsDeleted)
          .Where(relation => relation.Role.IsActive && !relation.Role.IsDeleted)
          .Where(relation => relation.User.IsActive && !relation.User.IsDeleted)
          .OrderBy(relation => relation.User.Username)
          .Select(relation => new GetUsersByPublicIdResponseDto(
            relation.User.PublicId,
            relation.User.Username,
            relation.User.Email,
            relation.AssignedAt))
          .Skip((pageNumber - 1) * pageSize)
          .Take(pageSize)
          .ToListAsync(cancellationToken);

        return users.AsReadOnly();
    }
}
