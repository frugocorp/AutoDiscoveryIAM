// -----------------------------------------------------------------------
// <copyright file="GetRolesRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoles;

public interface IGetRolesRepository
{
  Task<(IReadOnlyList<Role> Items, int TotalCount)> GetRoles(int pageNumber, int pageSize);
}

public class GetRolesRepository(AutoDiscoveryIAMDbContext context) : IGetRolesRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public async Task<(IReadOnlyList<Role> Items, int TotalCount)> GetRoles(int pageNumber, int pageSize)
  {
    var query = _context.Roles.OrderByDescending(r => r.CreatedAt);

    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return (items, totalCount);
  }
}
