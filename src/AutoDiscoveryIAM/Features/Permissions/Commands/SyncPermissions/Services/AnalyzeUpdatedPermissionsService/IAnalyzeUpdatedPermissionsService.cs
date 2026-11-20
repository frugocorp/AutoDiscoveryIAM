// -----------------------------------------------------------------------
// <copyright file="IAnalyzeUpdatedPermissionsService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.AnalyzeUpdatedPermissionsService;

/// <summary>
/// Interfaz para el servicio orquestador de sincronización de permisos.
/// </summary>
/// <remarks>
/// Orquesta los servicios de descubrimiento y sincronización, proporcionando una interfaz
/// unificada y simplificada para el handler.
/// </remarks>
public interface IAnalyzeUpdatedPermissionsService
{
    /// <summary>
    /// Ejecuta el proceso completo de sincronización de permisos.
    /// </summary>
    /// <param name="forceFullScan">Si es true, ignora cache y escanea todos los archivos.</param>
    /// <param name="dryRun">Si es true, no persiste cambios, solo retorna reporte.</param>
    /// <param name="enableDetailedConsoleOutput">Si es true, imprime en consola cada permiso descubierto.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado consolidado del proceso completo de sincronización.</returns>
    Task<OperationResult<AnalyzeUpdatedPermissionsResponseDto>> SyncPermissionsAsync(
        bool forceFullScan = false,
        bool dryRun = false,
        bool enableDetailedConsoleOutput = false,
        CancellationToken cancellationToken = default);
}
