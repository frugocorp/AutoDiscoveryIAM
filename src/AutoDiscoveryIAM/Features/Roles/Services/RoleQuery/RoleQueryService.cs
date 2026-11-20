using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.RoleQuery;

/// <summary>
/// Implementación del servicio de consultas del dominio Roles.
/// Todas las consultas requieren parámetros explícitos sin dependencias de otros dominios.
/// </summary>
public class RoleQueryService : IRoleQueryService
{
    private readonly AutoDiscoveryIAMDbContext _context;

    public RoleQueryService(AutoDiscoveryIAMDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetRolePermissionsByPublicIdAsync(Guid rolePublicId)
    {
        var permissions = await _context.Roles
            .Where(r => r.PublicId == rolePublicId && r.IsActive)
            .SelectMany(r => r.RolePermissions)
            .Where(rp => rp.Permission.IsActive)
            .Select(rp => rp.Permission.Key)
            .ToListAsync();

        return permissions.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<Role?> GetRoleByPublicIdAsync(Guid rolePublicId)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.PublicId == rolePublicId);
    }

    /// <inheritdoc />
    public async Task<Role?> GetRoleByNameAsync(string roleName)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == roleName);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Role>> GetAllRolesAsync()
    {
        var roles = await _context.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return roles.AsReadOnly();
    }
}
