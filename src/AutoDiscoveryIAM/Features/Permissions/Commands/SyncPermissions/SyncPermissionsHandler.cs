// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.AnalyzeUpdatedPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;

/// <summary>
/// Handler para sincronizar permisos automáticamente a partir de un comando <see cref="SyncPermissionsCommand"/>.
/// </summary>
/// <param name="analyzeService">Servicio orquestador de análisis y actualización de permisos (<see cref="IAnalyzeUpdatedPermissionsService"/>).</param>
/// <param name="logger">Instancia de <see cref="IPermissionLogger"/> para registrar eventos y errores relacionados con permisos.</param>
/// <param name="unitOfWork">Unidad de trabajo para gestionar la transacción.</param>
/// <remarks>
/// Utiliza el servicio orquestador que coordina el descubrimiento y sincronización de permisos.
/// Gestiona transacciones de base de datos y registra eventos del proceso.
/// </remarks>
public class SyncPermissionsHandler(
    IAnalyzeUpdatedPermissionsService analyzeService,
    IPermissionLogger logger,
    IAutoDiscoveryIAMUnitOfWork unitOfWork)
{
  private readonly IAnalyzeUpdatedPermissionsService _analyzeService = analyzeService ?? throw new ArgumentNullException(nameof(analyzeService));
  private readonly IPermissionLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

  /// <summary>
  /// Maneja la sincronización de permisos.
  /// </summary>
  /// <param name="command">Comando con los parámetros de sincronización.</param>
  /// <param name="cancellationToken">Token para cancelar la operación.</param>
  /// <returns>Resultado de la operación y los datos de sincronización en caso de éxito.</returns>
  /// <example>
  /// var handler = new SyncPermissionsHandler(analyzeService, logger, unitOfWork);
  /// var response = await handler.Handle(command, CancellationToken.None);
  /// </example>
  public async Task<OperationResult<SyncPermissionsCommandResponseDto>> Handle(SyncPermissionsCommand command, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(command);

    _logger.LogPermissionsSyncStarted(command.Request.ForceFullSync, command.Request.DryRun);

    // Ejecutar proceso completo de análisis y actualización de permisos
    var syncPermissionsResult = await _analyzeService.SyncPermissionsAsync(
        command.Request.ForceFullSync,
        command.Request.DryRun,
        command.Request.EnableDetailedConsoleOutput,
        cancellationToken);

    if (!syncPermissionsResult.Success)
    {
      _logger.LogPermissionsSyncFailed(syncPermissionsResult.Error ?? "Error desconocido");
      return OperationResult.Fail<SyncPermissionsCommandResponseDto>(
          syncPermissionsResult.ErrorInfo ?? ErrorInfo.Status500Unknown,
          syncPermissionsResult.Error ?? "Error desconocido en la orquestación");
    }

    // Si no es DryRun, persistir cambios usando AutoDiscoveryIAMUnitOfWork
    if (!command.Request.DryRun)
    {
      await _unitOfWork.SaveChangesAsync(cancellationToken);
      _logger.LogPermissionsSyncPersisted(syncPermissionsResult.Data!.PermissionsDiscovered);
    }

    // Envolver el resultado del orquestador en el DTO del handler
    var responseDto = new SyncPermissionsCommandResponseDto(syncPermissionsResult.Data!);

    _logger.LogPermissionsSyncCompleted(
        syncPermissionsResult.Data!.PermissionsDiscovered,
        syncPermissionsResult.Data!.TotalExecutionTimeMs,
        command.Request.DryRun);

    return OperationResult.Ok(responseDto);
  }
}
