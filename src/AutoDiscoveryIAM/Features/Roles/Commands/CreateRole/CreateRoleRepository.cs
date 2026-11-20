// -----------------------------------------------------------------------
// <copyright file="CreateRoleRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;

public interface ICreateRoleRepository
{
    Task<Role> CreateRole(Role role);
    Task<bool> RoleNameExistsAsync(string name);
}

public class CreateRoleRepository(AutoDiscoveryIAMDbContext context) : ICreateRoleRepository
{
    private readonly AutoDiscoveryIAMDbContext _context = context;

    public Task<Role> CreateRole(Role role)
    {
        _context.Roles.Add(role);
        return Task.FromResult(role);
    }

    public async Task<bool> RoleNameExistsAsync(string name)
    {
        return await _context.Roles.AnyAsync(r => r.Name == name);
    }
}
