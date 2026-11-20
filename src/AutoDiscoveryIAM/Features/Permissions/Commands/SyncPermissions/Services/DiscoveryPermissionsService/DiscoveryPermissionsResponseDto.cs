// -----------------------------------------------------------------------
// <copyright file="DiscoveryPermissionsResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;

/// <summary>
/// DTO de respuesta del servicio de descubrimiento de permisos.
/// </summary>
/// <param name="PermissionsDiscovered">Número total de permisos descubiertos.</param>
/// <param name="ExecutionTimeMs">Tiempo de ejecución del descubrimiento en milisegundos.</param>
/// <param name="DiscoveredPermissions">Lista de permisos descubiertos con todos sus detalles.</param>
/// <param name="ValidationErrors">Lista de errores de validación encontrados.</param>
/// <param name="ValidationWarnings">Lista de advertencias de validación encontradas.</param>
/// <param name="ScanStatistics">Estadísticas del proceso de escaneo.</param>
public record DiscoveryPermissionsResponseDto(
    int PermissionsDiscovered,
    long ExecutionTimeMs,
    IList<PermissionDescriptor> DiscoveredPermissions,
    IList<string> ValidationErrors,
    IList<string> ValidationWarnings,
    DiscoveryScanStatistics ScanStatistics);

/// <summary>
/// Estadísticas del proceso de escaneo de permisos.
/// </summary>
/// <param name="FilesScanned">Número de archivos analizados.</param>
/// <param name="EndpointsFound">Número total de endpoints encontrados.</param>
/// <param name="ValidEndpoints">Número de endpoints válidos para generar permisos.</param>
/// <param name="SkippedEndpoints">Número de endpoints omitidos por no cumplir criterios.</param>
/// <param name="FeaturesAnalyzed">Lista de features analizadas.</param>
public record DiscoveryScanStatistics(
    int FilesScanned,
    int EndpointsFound,
    int ValidEndpoints,
    int SkippedEndpoints,
    IList<string> FeaturesAnalyzed);
