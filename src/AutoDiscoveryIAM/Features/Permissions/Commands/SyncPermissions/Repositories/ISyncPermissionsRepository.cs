// -----------------------------------------------------------------------
// <copyright file="ISyncPermissionsRepository.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Repositories;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Repositories;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Repositories;

/// <summary>
/// Interfaz para operaciones específicas del slice de sincronización de permisos.
/// </summary>
public interface ISyncPermissionsRepository : IPermissionsRepository
{
    /// <summary>
    /// Estadísticas de operaciones realizadas por el repositorio.
    /// </summary>
    RepositoryStats Stats { get; }

    /// <summary>
    /// Marca un permiso como obsoleto (soft delete).
    /// </summary>
    /// <param name="key">Clave del permiso a marcar como obsoleto.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Task que representa la operación asíncrona.</returns>
    Task MarkAsObsoleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupera un permiso marcado como obsoleto.
    /// </summary>
    /// <param name="permission">Permiso a recuperar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Task que representa la operación asíncrona.</returns>
    Task RecoverPermissionAsync(Permission permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza la fecha de última sincronización de un permiso.
    /// </summary>
    /// <param name="key">Clave del permiso.</param>
    /// <param name="syncDate">Fecha de sincronización.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Task que representa la operación asíncrona.</returns>
    Task UpdateLastSyncAsync(string key, DateTime syncDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene permisos que no han sido sincronizados desde una fecha específica.
    /// </summary>
    /// <param name="cutoffDate">Fecha límite.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Lista de permisos no sincronizados.</returns>
    Task<IList<Permission>> GetOutOfSyncPermissionsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}
