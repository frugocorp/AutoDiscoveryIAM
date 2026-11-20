// -----------------------------------------------------------------------
// <copyright file="AnalyzeUpdatedPermissionsService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.SyncPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Catalogs;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.AnalyzeUpdatedPermissionsService;

/// <summary>
/// Servicio orquestador para la sincronización de permisos.
/// </summary>
/// <remarks>
/// Coordina los servicios de descubrimiento y sincronización, manejando errores y
/// proporcionando una respuesta consolidada y simplificada.
/// </remarks>
public class AnalyzeUpdatedPermissionsService(
    IDiscoveryPermissionsService discoveryService,
    ISyncPermissionsService syncService,
    IPermissionLogger logger) : IAnalyzeUpdatedPermissionsService
{
  private readonly IDiscoveryPermissionsService _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
  private readonly ISyncPermissionsService _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
  private readonly IPermissionLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

  /// <inheritdoc/>
  public async Task<OperationResult<AnalyzeUpdatedPermissionsResponseDto>> SyncPermissionsAsync(
      bool forceFullScan = false,
      bool dryRun = false,
      bool enableDetailedConsoleOutput = false,
      CancellationToken cancellationToken = default)
  {
    var totalStopwatch = Stopwatch.StartNew();
    var discoveryStopwatch = Stopwatch.StartNew();

    // 1. Ejecutar descubrimiento de permisos
    var discoveryResult = await _discoveryService.DiscoverPermissionsAsync(forceFullScan, enableDetailedConsoleOutput, cancellationToken);
    discoveryStopwatch.Stop();

    if (!discoveryResult.Success || discoveryResult.Data == null || discoveryResult.Data.Count == 0)
    {
      _logger.LogNoPermissionsFound();
      return OperationResult.Fail<AnalyzeUpdatedPermissionsResponseDto>(
          discoveryResult.ErrorInfo ?? ErrorInfo.Status404NotFound,
          discoveryResult.Error ?? PermissionErrorMessagesCatalog.NoPermissionsDiscovered);
    }

    // 2. Validar consistencia de permisos descubiertos
    var validationResult = _discoveryService.ValidateConsistency(discoveryResult.Data);
    if (!validationResult.Success)
    {
      return OperationResult.Fail<AnalyzeUpdatedPermissionsResponseDto>(
          validationResult.ErrorInfo ?? ErrorInfo.Status400Validation,
          validationResult.Error ?? PermissionErrorMessagesCatalog.ValidationConsistencyError);
    }

    // 3. Ejecutar sincronización con BD
    var syncResult = await _syncService.SyncPermissionsAsync(discoveryResult.Data, dryRun, cancellationToken);
    if (!syncResult.Success || syncResult.Data == null)
    {
      return OperationResult.Fail<AnalyzeUpdatedPermissionsResponseDto>(
          syncResult.ErrorInfo ?? ErrorInfo.Status500Unknown,
          syncResult.Error ?? PermissionErrorMessagesCatalog.SyncDatabaseError);
    }

    totalStopwatch.Stop();

    // Endpoints que exigen permiso pero no pudieron nombrarse: hoy responden 403 a casi todos.
    // Se reportan (no fallan el sync) para que quien consume el paquete los corrija o los marque.
    var endpointsWithoutPermission = _discoveryService.FindEndpointsWithoutPermission() ?? [];

    // 4. Construir respuesta consolidada
    var orchestrationResponse = BuildResponse(discoveryResult.Data, syncResult.Data, totalStopwatch.ElapsedMilliseconds, discoveryStopwatch.ElapsedMilliseconds, dryRun)
        with { EndpointsWithoutPermission = endpointsWithoutPermission };

    return OperationResult.Ok(orchestrationResponse);
  }

  private static AnalyzeUpdatedPermissionsResponseDto BuildResponse(
      IList<PermissionDescriptor> discoveryResult,
      SyncPermissionsResponseDto syncResult,
      long totalTimeMs,
      long discoveryTimeMs,
      bool dryRun)
  {
    return new AnalyzeUpdatedPermissionsResponseDto(
        PermissionsDiscovered: discoveryResult.Count,
        PermissionsCreated: syncResult.PermissionsCreated,
        PermissionsUpdated: syncResult.PermissionsUpdated,
        PermissionsObsoleted: syncResult.PermissionsObsoleted,
        PermissionsRecovered: syncResult.PermissionsRecovered,
        TotalExecutionTimeMs: totalTimeMs,
        DiscoveryExecutionTimeMs: discoveryTimeMs, // Tiempo real del discovery
        SyncExecutionTimeMs: syncResult.ExecutionTimeMs,
        FilesScanned: 0, // TODO: Obtener estadísticas del discovery cuando esté implementado
        FeaturesAnalyzed: discoveryResult.Select(p => p.Feature).Distinct().ToList(),
        DryRunMode: dryRun);
  }
}
