using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;

public interface ICreateUserRolesRepository
{
  Task<User?> GetUserByPublicIdAsync(Guid userPublicId, CancellationToken cancellationToken);
  Task<Role?> GetRoleByPublicIdAsync(Guid rolePublicId, CancellationToken cancellationToken);
  Task<UserRole?> GetRelationAsync(long userId, long roleId, CancellationToken cancellationToken);
  Task AddRelationAsync(UserRole userRole, CancellationToken cancellationToken);
}

public class CreateUserRolesRepository(AutoDiscoveryIAMDbContext context) : ICreateUserRolesRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public Task<User?> GetUserByPublicIdAsync(Guid userPublicId, CancellationToken cancellationToken)
  {
    return _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(user => user.PublicId == userPublicId, cancellationToken);
  }

  public Task<Role?> GetRoleByPublicIdAsync(Guid rolePublicId, CancellationToken cancellationToken)
  {
    return _context.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(role => role.PublicId == rolePublicId, cancellationToken);
  }

  public Task<UserRole?> GetRelationAsync(long userId, long roleId, CancellationToken cancellationToken)
  {
    return _context.UserRoles.IgnoreQueryFilters().FirstOrDefaultAsync(userRole => userRole.UserId == userId && userRole.RoleId == roleId, cancellationToken);
  }

  public Task AddRelationAsync(UserRole userRole, CancellationToken cancellationToken)
  {
    _context.UserRoles.Add(userRole);
    return Task.CompletedTask;
  }
}
