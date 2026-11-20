using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.UserQuery;

/// <summary>
/// Implementación del servicio de consultas del dominio Users.
/// Todas las consultas requieren parámetros explícitos sin dependencias de otros dominios.
/// </summary>
public class UserQueryService : IUserQueryService
{
    private readonly AutoDiscoveryIAMDbContext _context;

    public UserQueryService(AutoDiscoveryIAMDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Role>> GetUserRolesByPublicIdAsync(Guid userPublicId)
    {
        var roles = await _context.Users
            .Where(u => u.PublicId == userPublicId && u.IsActive && !u.IsDeleted)
            .SelectMany(u => u.UserRoles)
            .Where(ur => !ur.IsDeleted)
            .Select(ur => ur.Role)
            .Where(r => r.IsActive && !r.IsDeleted)
            .ToListAsync();

        return roles.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetUserPermissionsByPublicIdAsync(Guid userPublicId)
    {
        // Obtener permisos directos de los roles del usuario
        var permissions = await _context.Users
            .Where(u => u.PublicId == userPublicId && u.IsActive && !u.IsDeleted)
            .SelectMany(u => u.UserRoles)
            .Where(ur => !ur.IsDeleted && ur.Role.IsActive && !ur.Role.IsDeleted)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Where(rp => !rp.IsDeleted && rp.Permission.IsActive && !rp.Permission.IsDeleted)
            .Select(rp => rp.Permission.Key)
            .Distinct()
            .ToListAsync();

        // Expandir wildcards
        var expandedPermissions = ExpandWildcardPermissions(permissions);

        return expandedPermissions.Distinct().ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionByPublicIdAsync(Guid userPublicId, string permission)
    {
        var userPermissions = await GetUserPermissionsByPublicIdAsync(userPublicId);

        // Verificación directa
        if (userPermissions.Contains(permission))
        {
            return true;
        }

        // Verificación de wildcards
        var permissionParts = permission.Split('.');
        if (permissionParts.Length >= 2)
        {
            var wildcardPattern = $"{permissionParts[0]}.*";
            if (userPermissions.Contains(wildcardPattern))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByPublicIdAsync(Guid userPublicId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.PublicId == userPublicId);
    }

    /// <inheritdoc />
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Expande permisos wildcards a permisos específicos.
    /// Ejemplo: "posts.*" se expande a ["posts.read", "posts.create", "posts.update", "posts.delete"]
    /// </summary>
    /// <param name="permissions">Lista de permisos que puede incluir wildcards.</param>
    /// <returns>Lista expandida de permisos específicos.</returns>
    private List<string> ExpandWildcardPermissions(List<string> permissions)
    {
        var expandedPermissions = new List<string>();

        foreach (var permission in permissions)
        {
            if (permission.EndsWith(".*", StringComparison.Ordinal))
            {
                // Obtener el prefijo del wildcard (ej: "posts" de "posts.*")
                var prefix = permission.Substring(0, permission.Length - 2);

                // Buscar todos los permisos que coincidan con el prefijo
                var matchingPermissions = _context.Permissions
                    .Where(p => p.Key.StartsWith(prefix + ".") && p.IsActive)
                    .Select(p => p.Key)
                    .ToList();

                expandedPermissions.AddRange(matchingPermissions);
            }
            else
            {
                expandedPermissions.Add(permission);
            }
        }

        return expandedPermissions;
    }
}
