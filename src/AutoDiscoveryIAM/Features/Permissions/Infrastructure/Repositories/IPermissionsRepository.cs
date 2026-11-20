// -----------------------------------------------------------------------
// <copyright file="IPermissionsRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Repositories;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Repositories;

/// <summary>
/// Interfaz para operaciones de repositorio específicas de la feature Permissions.
/// </summary>
public interface IPermissionsRepository : IBaseRepository<Permission>
{
    /// <summary>
    /// Obtiene un permiso por su clave única.
    /// </summary>
    /// <param name="key">Clave única del permiso.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Permiso encontrado o null si no existe.</returns>
    Task<Permission?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene todos los permisos activos.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de permisos activos.</returns>
    Task<IList<Permission>> GetActivePermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene permisos por grupo.
    /// </summary>
    /// <param name="group">Grupo de permisos.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de permisos del grupo especificado.</returns>
    Task<IList<Permission>> GetByGroupAsync(string group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene permisos generados por el sistema.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de permisos generados por el sistema.</returns>
    Task<IList<Permission>> GetSystemGeneratedPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca permisos por texto en descripción o clave.
    /// </summary>
    /// <param name="searchText">Texto a buscar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de permisos que coinciden con la búsqueda.</returns>
    Task<IList<Permission>> SearchPermissionsAsync(string searchText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene permisos que tienen el atributo PermissionsNotRequired.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de permisos que no requieren validación de permisos.</returns>
    Task<IList<Permission>> GetPermissionsNotRequiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene permisos que requieren acceso de desarrollo.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de permisos que requieren acceso de desarrollo.</returns>
    Task<IList<Permission>> GetDevelopmentAccessPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene permisos que requieren acceso de SuperAdmin.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de permisos que requieren acceso de SuperAdmin.</returns>
    Task<IList<Permission>> GetSuAdminAccessPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene permisos que tienen validación explícita con RequirePermissions.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de permisos con validación explícita.</returns>
    Task<IList<Permission>> GetExplicitPermissionValidationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene permisos por tipo de autorización combinado.
    /// </summary>
    /// <param name="permissionsNotRequired">Incluir permisos que no requieren validación.</param>
    /// <param name="developmentAccess">Incluir permisos que requieren acceso de desarrollo.</param>
    /// <param name="suAdminAccess">Incluir permisos que requieren acceso de SuperAdmin.</param>
    /// <param name="explicitValidation">Incluir permisos con validación explícita.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de permisos que cumplen los criterios de autorización especificados.</returns>
    Task<IList<Permission>> GetPermissionsByAuthorizationTypeAsync(
        bool permissionsNotRequired = false,
        bool developmentAccess = false,
        bool suAdminAccess = false,
        bool explicitValidation = false,
        CancellationToken cancellationToken = default);
}
