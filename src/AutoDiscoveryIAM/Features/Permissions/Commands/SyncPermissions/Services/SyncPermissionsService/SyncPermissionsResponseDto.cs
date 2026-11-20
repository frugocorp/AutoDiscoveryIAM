// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.SyncPermissionsService;

/// <summary>
/// DTO de respuesta del servicio de sincronización de permisos.
/// </summary>
/// <param name="PermissionsCreated">Número de permisos nuevos creados en la base de datos.</param>
/// <param name="PermissionsUpdated">Número de permisos existentes que fueron actualizados.</param>
/// <param name="PermissionsObsoleted">Número de permisos marcados como obsoletos (soft delete).</param>
/// <param name="PermissionsRecovered">Número de permisos obsoletos que fueron recuperados.</param>
/// <param name="ExecutionTimeMs">Tiempo de ejecución de la sincronización en milisegundos.</param>
/// <param name="DatabaseOperations">Estadísticas detalladas de operaciones en base de datos.</param>
/// <param name="ProcessingErrors">Lista de errores ocurridos durante la sincronización.</param>
/// <param name="ProcessingWarnings">Lista de advertencias generadas durante la sincronización.</param>
/// <param name="DryRunMode">Indica si se ejecutó en modo de prueba sin persistir cambios.</param>
public record SyncPermissionsResponseDto(
    int PermissionsCreated,
    int PermissionsUpdated,
    int PermissionsObsoleted,
    int PermissionsRecovered,
    long ExecutionTimeMs,
    DatabaseOperationStatistics DatabaseOperations,
    IList<string> ProcessingErrors,
    IList<string> ProcessingWarnings,
    bool DryRunMode);

/// <summary>
/// Estadísticas de operaciones en base de datos durante la sincronización.
/// </summary>
/// <param name="TotalQueriesExecuted">Número total de consultas ejecutadas.</param>
/// <param name="InsertOperations">Número de operaciones INSERT realizadas.</param>
/// <param name="UpdateOperations">Número de operaciones UPDATE realizadas.</param>
/// <param name="SoftDeleteOperations">Número de operaciones de soft delete realizadas.</param>
/// <param name="RecoveryOperations">Número de operaciones de recuperación realizadas.</param>
/// <param name="TransactionDurationMs">Duración total de transacciones en milisegundos.</param>
public record DatabaseOperationStatistics(
    int TotalQueriesExecuted,
    int InsertOperations,
    int UpdateOperations,
    int SoftDeleteOperations,
    int RecoveryOperations,
    long TransactionDurationMs);
