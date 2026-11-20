// -----------------------------------------------------------------------
// <copyright file="GetPermissionsRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetPermissions;

public interface IGetPermissionsRepository
{
    Task<(IReadOnlyList<Permission> Items, int TotalCount)> GetPermissions(int pageNumber, int pageSize);
}

public class GetPermissionsRepository(AutoDiscoveryIAMDbContext context) : IGetPermissionsRepository
{
    private readonly AutoDiscoveryIAMDbContext _context = context;

    public async Task<(IReadOnlyList<Permission> Items, int TotalCount)> GetPermissions(int pageNumber, int pageSize)
    {
        var query = _context.Permissions
            .OrderBy(p => p.Group)
            .ThenBy(p => p.Key);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
