using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.RoleQuery;

/// <summary>
/// Servicio para consultas del dominio Roles con parámetros explícitos.
/// Sin dependencias de otros dominios - todas las consultas requieren parámetros claros.
/// </summary>
public interface IRoleQueryService
{
    /// <summary>
    /// Obtiene todos los permisos de un rol específico por su PublicId.
    /// </summary>
    /// <param name="rolePublicId">PublicId del rol a consultar.</param>
    /// <returns>Lista de nombres de permisos del rol especificado.</returns>
    Task<IReadOnlyList<string>> GetRolePermissionsByPublicIdAsync(Guid rolePublicId);

    /// <summary>
    /// Obtiene un rol completo por su PublicId.
    /// </summary>
    /// <param name="rolePublicId">PublicId del rol a buscar.</param>
    /// <returns>El rol encontrado o null si no existe.</returns>
    Task<Role?> GetRoleByPublicIdAsync(Guid rolePublicId);

    /// <summary>
    /// Obtiene un rol completo por su nombre.
    /// </summary>
    /// <param name="roleName">Nombre del rol a buscar.</param>
    /// <returns>El rol encontrado o null si no existe.</returns>
    Task<Role?> GetRoleByNameAsync(string roleName);

    /// <summary>
    /// Obtiene todos los roles activos del sistema.
    /// </summary>
    /// <returns>Lista de todos los roles activos.</returns>
    Task<IReadOnlyList<Role>> GetAllRolesAsync();
}
