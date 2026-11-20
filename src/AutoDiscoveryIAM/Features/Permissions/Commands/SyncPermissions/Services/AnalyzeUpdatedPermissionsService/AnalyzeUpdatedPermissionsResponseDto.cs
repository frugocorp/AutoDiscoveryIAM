// -----------------------------------------------------------------------
// <copyright file="AnalyzeUpdatedPermissionsResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.AnalyzeUpdatedPermissionsService;

/// <summary>
/// DTO de respuesta del servicio orquestador de sincronización de permisos.
/// </summary>
/// <remarks>
/// Contiene un resumen consolidado y no sensible de los resultados de discovery y sincronización.
/// </remarks>
/// <param name="PermissionsDiscovered">Número total de permisos descubiertos.</param>
/// <param name="PermissionsCreated">Número de permisos nuevos creados en la base de datos.</param>
/// <param name="PermissionsUpdated">Número de permisos existentes que fueron actualizados.</param>
/// <param name="PermissionsObsoleted">Número de permisos marcados como obsoletos.</param>
/// <param name="PermissionsRecovered">Número de permisos obsoletos que fueron recuperados.</param>
/// <param name="TotalExecutionTimeMs">Tiempo total de ejecución del proceso completo en milisegundos.</param>
/// <param name="DiscoveryExecutionTimeMs">Tiempo de ejecución solo del descubrimiento en milisegundos.</param>
/// <param name="SyncExecutionTimeMs">Tiempo de ejecución solo de la sincronización en milisegundos.</param>
/// <param name="FilesScanned">Número de archivos analizados durante el discovery.</param>
/// <param name="FeaturesAnalyzed">Lista de features que fueron analizadas.</param>
/// <param name="DryRunMode">Indica si se ejecutó en modo de prueba sin persistir cambios.</param>
/// <param name="EndpointsWithoutPermission">Endpoints que exigen permiso pero cuyo namespace no permite derivar una clave (responden 403 salvo a usuarios con flags especiales); vacía si no hay ninguno.</param>
public record AnalyzeUpdatedPermissionsResponseDto(
    int PermissionsDiscovered,
    int PermissionsCreated,
    int PermissionsUpdated,
    int PermissionsObsoleted,
    int PermissionsRecovered,
    long TotalExecutionTimeMs,
    long DiscoveryExecutionTimeMs,
    long SyncExecutionTimeMs,
    int FilesScanned,
    IList<string> FeaturesAnalyzed,
    bool DryRunMode,
    IList<string>? EndpointsWithoutPermission = null);
