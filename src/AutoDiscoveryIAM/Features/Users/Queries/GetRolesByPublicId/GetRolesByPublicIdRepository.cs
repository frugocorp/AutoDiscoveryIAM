using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetRolesByPublicId;

public interface IGetRolesByPublicIdRepository
{
  Task<bool> UserExistsAsync(Guid userPublicId, CancellationToken cancellationToken);
  Task<bool> UserIsActiveAsync(Guid userPublicId, CancellationToken cancellationToken);
  Task<IReadOnlyList<GetRolesByPublicIdResponseDto>> GetRolesAsync(Guid userPublicId, CancellationToken cancellationToken);
}

public class GetRolesByPublicIdRepository(AutoDiscoveryIAMDbContext context) : IGetRolesByPublicIdRepository
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

  public async Task<IReadOnlyList<GetRolesByPublicIdResponseDto>> GetRolesAsync(Guid userPublicId, CancellationToken cancellationToken)
  {
    var roles = await _context.UserRoles
      .Where(userRole => userRole.User.PublicId == userPublicId && !userRole.IsDeleted && userRole.User.IsActive && !userRole.User.IsDeleted && userRole.Role.IsActive && !userRole.Role.IsDeleted)
      .OrderBy(userRole => userRole.Role.Name)
      .Select(userRole => new GetRolesByPublicIdResponseDto(userRole.Role.PublicId, userRole.Role.Name, userRole.Role.Description, userRole.Role.IsActive, userRole.AssignedAt))
      .ToListAsync(cancellationToken);
    return roles.AsReadOnly();
  }
}
