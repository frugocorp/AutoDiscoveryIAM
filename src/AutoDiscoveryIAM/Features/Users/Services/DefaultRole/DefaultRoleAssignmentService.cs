// -----------------------------------------------------------------------
// <copyright file="DefaultRoleAssignmentService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.DefaultRole;

/// <inheritdoc />
public class DefaultRoleAssignmentService(AutoDiscoveryIAMDbContext context) : IDefaultRoleAssignmentService
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  /// <inheritdoc />
  public async Task<bool> AssignDefaultRoleAsync(User user, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(user);

    // El filtro global ya excluye los roles borrados; un rol por defecto desactivado tampoco se
    // asigna (el índice único garantiza que haya como mucho uno marcado).
    var defaultRole = await _context.Roles
      .Where(r => r.IsDefaultForNewUser && r.IsActive)
      .FirstOrDefaultAsync(cancellationToken);
    if (defaultRole == null)
    {
      return false;
    }

    // Por navegación y no por Id: el usuario todavía no tiene Id, EF lo resuelve al guardar.
    user.UserRoles.Add(new UserRole
    {
      User = user,
      Role = defaultRole,
      AssignedAt = DateTime.UtcNow,
    });

    return true;
  }
}
