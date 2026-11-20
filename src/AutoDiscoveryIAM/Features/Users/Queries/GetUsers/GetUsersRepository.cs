// -----------------------------------------------------------------------
// <copyright file="GetUsersRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;

public interface IGetUsersRepository
{
  Task<(IReadOnlyList<User> Items, int TotalCount)> GetUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
}

public class GetUsersRepository(AutoDiscoveryIAMDbContext context) : IGetUsersRepository
{
  private readonly AutoDiscoveryIAMDbContext _context = context;

  public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetUsersAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
  {
    var query = _context.Users
        .Where(u => !u.IsDeleted)
        .OrderBy(u => u.Username);

    var totalCount = await query.CountAsync(cancellationToken);
    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    return (items, totalCount);
  }
}
