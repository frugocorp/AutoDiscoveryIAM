using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.Assignments;

public interface IRoleUserAssignmentRepository
{
  Task<User?> GetActiveUserByPublicIdAsync(Guid userPublicId, CancellationToken cancellationToken);
  Task<Role?> GetActiveRoleByPublicIdAsync(Guid rolePublicId, CancellationToken cancellationToken);
  Task<IReadOnlyList<Role>> GetActiveRolesByPublicIdsAsync(IReadOnlyCollection<Guid> rolePublicIds, CancellationToken cancellationToken);
  Task<IReadOnlyList<User>> GetActiveUsersByPublicIdsAsync(IReadOnlyCollection<Guid> userPublicIds, CancellationToken cancellationToken);
  Task<IReadOnlyList<UserRole>> GetVisibleRelationsByUserIdAsync(long userId, CancellationToken cancellationToken);
  Task<IReadOnlyList<UserRole>> GetVisibleRelationsByRoleIdAsync(long roleId, CancellationToken cancellationToken);
  Task<IReadOnlyList<UserRole>> GetDeletedRelationsByUserIdAndRoleIdsAsync(long userId, IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken);
  Task<IReadOnlyList<UserRole>> GetDeletedRelationsByRoleIdAndUserIdsAsync(long roleId, IReadOnlyCollection<long> userIds, CancellationToken cancellationToken);
}

public class RoleUserAssignmentRepository(AutoDiscoveryIAMDbContext context) : IRoleUserAssignmentRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public Task<User?> GetActiveUserByPublicIdAsync(Guid userPublicId, CancellationToken cancellationToken)
  {
    return _context.Users
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(user => user.PublicId == userPublicId && user.IsActive && !user.IsDeleted, cancellationToken);
  }

  public Task<Role?> GetActiveRoleByPublicIdAsync(Guid rolePublicId, CancellationToken cancellationToken)
  {
    return _context.Roles
      .IgnoreQueryFilters()
      .FirstOrDefaultAsync(role => role.PublicId == rolePublicId && role.IsActive && !role.IsDeleted, cancellationToken);
  }

  public async Task<IReadOnlyList<Role>> GetActiveRolesByPublicIdsAsync(IReadOnlyCollection<Guid> rolePublicIds, CancellationToken cancellationToken)
  {
    var roles = await _context.Roles
      .IgnoreQueryFilters()
      .Where(role => rolePublicIds.Contains(role.PublicId) && role.IsActive && !role.IsDeleted)
      .ToListAsync(cancellationToken);
    return roles.AsReadOnly();
  }

  public async Task<IReadOnlyList<User>> GetActiveUsersByPublicIdsAsync(IReadOnlyCollection<Guid> userPublicIds, CancellationToken cancellationToken)
  {
    var users = await _context.Users
      .IgnoreQueryFilters()
      .Where(user => userPublicIds.Contains(user.PublicId) && user.IsActive && !user.IsDeleted)
      .ToListAsync(cancellationToken);
    return users.AsReadOnly();
  }

  public async Task<IReadOnlyList<UserRole>> GetVisibleRelationsByUserIdAsync(long userId, CancellationToken cancellationToken)
  {
    var relations = await _context.UserRoles
      .Where(userRole => userRole.UserId == userId && !userRole.IsDeleted)
      .Where(userRole => userRole.Role.IsActive && !userRole.Role.IsDeleted)
      .Include(userRole => userRole.Role)
      .Include(userRole => userRole.User)
      .ToListAsync(cancellationToken);
    return relations.AsReadOnly();
  }

  public async Task<IReadOnlyList<UserRole>> GetVisibleRelationsByRoleIdAsync(long roleId, CancellationToken cancellationToken)
  {
    var relations = await _context.UserRoles
      .Where(userRole => userRole.RoleId == roleId && !userRole.IsDeleted)
      .Where(userRole => userRole.User.IsActive && !userRole.User.IsDeleted)
      .Include(userRole => userRole.Role)
      .Include(userRole => userRole.User)
      .ToListAsync(cancellationToken);
    return relations.AsReadOnly();
  }

  public async Task<IReadOnlyList<UserRole>> GetDeletedRelationsByUserIdAndRoleIdsAsync(long userId, IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken)
  {
    var relations = await _context.UserRoles
      .IgnoreQueryFilters()
      .Where(userRole => userRole.UserId == userId && userRole.IsDeleted && roleIds.Contains(userRole.RoleId))
      .Where(userRole => userRole.Role.IsActive && !userRole.Role.IsDeleted)
      .Include(userRole => userRole.Role)
      .Include(userRole => userRole.User)
      .ToListAsync(cancellationToken);
    return relations.AsReadOnly();
  }

  public async Task<IReadOnlyList<UserRole>> GetDeletedRelationsByRoleIdAndUserIdsAsync(long roleId, IReadOnlyCollection<long> userIds, CancellationToken cancellationToken)
  {
    var relations = await _context.UserRoles
      .IgnoreQueryFilters()
      .Where(userRole => userRole.RoleId == roleId && userRole.IsDeleted && userIds.Contains(userRole.UserId))
      .Where(userRole => userRole.User.IsActive && !userRole.User.IsDeleted)
      .Include(userRole => userRole.Role)
      .Include(userRole => userRole.User)
      .ToListAsync(cancellationToken);
    return relations.AsReadOnly();
  }
}
