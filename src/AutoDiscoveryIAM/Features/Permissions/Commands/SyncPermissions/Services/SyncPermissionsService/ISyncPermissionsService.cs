// -----------------------------------------------------------------------
// <copyright file="ISyncPermissionsService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.SyncPermissionsService;

/// <summary>
/// Interfaz para el servicio de sincronización de permisos con la base de datos.
/// </summary>
/// <remarks>
/// Define el contrato para sincronizar permisos descubiertos con la base de datos,
/// implementando el algoritmo de 3 niveles de recuperación.
/// </remarks>
public interface ISyncPermissionsService
{
    /// <summary>
    /// Sincroniza los permisos descubiertos con la base de datos.
    /// </summary>
    /// <param name="discoveredPermissions">Lista de permisos descubiertos.</param>
    /// <param name="dryRun">Si es true, no persiste cambios, solo retorna reporte.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado de la operación con detalles de la sincronización.</returns>
    Task<OperationResult<SyncPermissionsResponseDto>> SyncPermissionsAsync(
        IList<PermissionDescriptor> discoveredPermissions,
        bool dryRun = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta el algoritmo de 3 niveles para recuperar permisos existentes.
    /// </summary>
    /// <param name="existingPermissionId">ID del permiso existente a recuperar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado de la operación con descriptor actualizado del permiso o null si no se pudo recuperar.</returns>
    Task<OperationResult<PermissionDescriptor?>> RecoverPermissionAsync(long existingPermissionId, CancellationToken cancellationToken = default);
}
