using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.UserQuery;

/// <summary>
/// Servicio para consultas del dominio Users con parámetros explícitos.
/// Sin dependencias de otros dominios - todas las consultas requieren parámetros claros.
/// </summary>
public interface IUserQueryService
{
    /// <summary>
    /// Obtiene los roles de un usuario específico por su PublicId.
    /// </summary>
    /// <param name="userPublicId">PublicId del usuario a consultar.</param>
    /// <returns>Lista de roles del usuario especificado.</returns>
    Task<IReadOnlyList<Role>> GetUserRolesByPublicIdAsync(Guid userPublicId);

    /// <summary>
    /// Obtiene todos los permisos efectivos de un usuario por su PublicId.
    /// Incluye resolución de wildcards (posts.* → posts.read, posts.write, etc.).
    /// </summary>
    /// <param name="userPublicId">PublicId del usuario a consultar.</param>
    /// <returns>Lista de nombres de permisos efectivos del usuario.</returns>
    Task<IReadOnlyList<string>> GetUserPermissionsByPublicIdAsync(Guid userPublicId);

    /// <summary>
    /// Verifica si un usuario tiene un permiso específico por su PublicId.
    /// Incluye verificación de wildcards.
    /// </summary>
    /// <param name="userPublicId">PublicId del usuario a verificar.</param>
    /// <param name="permission">Nombre del permiso a verificar.</param>
    /// <returns>true si el usuario tiene el permiso, false en caso contrario.</returns>
    Task<bool> HasPermissionByPublicIdAsync(Guid userPublicId, string permission);

    /// <summary>
    /// Obtiene un usuario completo por su PublicId.
    /// </summary>
    /// <param name="userPublicId">PublicId del usuario a buscar.</param>
    /// <returns>El usuario encontrado o null si no existe.</returns>
    Task<User?> GetUserByPublicIdAsync(Guid userPublicId);

    /// <summary>
    /// Obtiene un usuario completo por su email.
    /// </summary>
    /// <param name="email">Email del usuario a buscar.</param>
    /// <returns>El usuario encontrado o null si no existe.</returns>
    Task<User?> GetUserByEmailAsync(string email);
}
