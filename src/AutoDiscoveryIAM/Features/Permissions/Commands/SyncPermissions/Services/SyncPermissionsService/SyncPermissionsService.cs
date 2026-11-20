// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Repositories;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.SyncPermissionsService;

/// <summary>
/// Resultado de las operaciones de sincronización.
/// </summary>
/// <param name="PermissionsCreated">Número de permisos creados.</param>
/// <param name="PermissionsUpdated">Número de permisos actualizados.</param>
/// <param name="PermissionsObsoleted">Número de permisos marcados como obsoletos.</param>
/// <param name="TotalOperations">Total de operaciones realizadas.</param>
internal sealed record SyncOperationResult(
    int PermissionsCreated,
    int PermissionsUpdated,
    int PermissionsObsoleted,
    int TotalOperations);

/// <summary>
/// Planes de operación para la sincronización.
/// </summary>
/// <param name="NewPermissions">Permisos nuevos a crear.</param>
/// <param name="UpdatedPermissions">Permisos existentes a actualizar.</param>
/// <param name="ObsoleteKeys">Claves de permisos a marcar como obsoletos.</param>
internal sealed record OperationPlans(
    IList<PermissionDescriptor> NewPermissions,
    IList<PermissionDescriptor> UpdatedPermissions,
    IList<string> ObsoleteKeys);

/// <summary>
/// Servicio de sincronización de permisos con la base de datos.
/// </summary>
/// <remarks>
/// Implementa el algoritmo de 3 niveles para sincronizar permisos descubiertos con la base de datos.
/// </remarks>
public class SyncPermissionsService(
    ISyncPermissionsRepository permissionRepository,
    IPermissionLogger logger) : ISyncPermissionsService
{
  private readonly ISyncPermissionsRepository _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
  //private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
  private readonly IPermissionLogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

  /// <summary>
  /// Sincroniza los permisos descubiertos con la base de datos.
  /// </summary>
  /// <param name="discoveredPermissions">Lista de permisos descubiertos.</param>
  /// <param name="dryRun">Si es true, no persiste cambios, solo retorna reporte.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>Resultado de la operación con detalles de la sincronización.</returns>
  public async Task<OperationResult<SyncPermissionsResponseDto>> SyncPermissionsAsync(
      IList<PermissionDescriptor> discoveredPermissions,
      bool dryRun = false,
      CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(discoveredPermissions);
    var stopwatch = Stopwatch.StartNew();
    _logger.LogSyncProcessStarted(discoveredPermissions.Count, dryRun);

    try
    {
      // Ejecutar sincronización
      var syncResult = await ExecuteSynchronizationAsync(discoveredPermissions, dryRun, cancellationToken);
      stopwatch.Stop();
      // Construir respuesta
      var response = BuildSyncResponse(syncResult, stopwatch.ElapsedMilliseconds, dryRun);
      LogSyncCompletion(response);
      return OperationResult.Ok(response);
    }
    catch (OperationCanceledException)
    {
      // La operación fue cancelada por un cancellationToken.ThrowIfCancellationRequested(); o similar
      // Se envió un cancellationToken previamente cancelado
      // Volvemos a lanzar la misma exception que fue capturada en este caso OperationCanceledException
      throw;
    }
    catch (Exception ex)
    {
      return HandleSyncErrorAsync(ex, dryRun);
    }
  }

  /// <summary>
  /// Ejecuta la lógica de sincronización completa.
  /// </summary>
  private async Task<SyncOperationResult> ExecuteSynchronizationAsync(
      IList<PermissionDescriptor> discoveredPermissions,
      bool dryRun,
      CancellationToken cancellationToken)
  {
    // Obtener permisos existentes
    var existingPermissions = await GetExistingPermissionsAsync(cancellationToken);

    // Calcular operaciones necesarias
    var operationPlans = CalculateOperationPlans(discoveredPermissions, existingPermissions, cancellationToken);

    // Ejecutar operaciones
    var createCount = await ProcessNewPermissionsAsync(operationPlans.NewPermissions, dryRun, cancellationToken);
    var updateCount = await ProcessUpdatedPermissionsAsync(operationPlans.UpdatedPermissions, existingPermissions, dryRun, cancellationToken);
    var obsoleteCount = await ProcessObsoletePermissionsAsync(operationPlans.ObsoleteKeys, dryRun, cancellationToken);

    return new SyncOperationResult(createCount, updateCount, obsoleteCount, createCount + updateCount + obsoleteCount);
  }

  /// <summary>
  /// Obtiene todos los permisos existentes de la base de datos (activos e inactivos).
  /// </summary>
  private async Task<Dictionary<string, Permission>> GetExistingPermissionsAsync(CancellationToken cancellationToken)
  {
    // Obtener TODOS los permisos (activos e inactivos) para evitar violaciones de clave única
    var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
    return permissions.ToDictionary(p => p.Key, p => p);
  }

  /// <summary>
  /// Calcula qué operaciones son necesarias basándose en los permisos descubiertos vs existentes.
  /// </summary>
  private static OperationPlans CalculateOperationPlans(
      IList<PermissionDescriptor> discoveredPermissions,
      Dictionary<string, Permission> existingPermissions,
      CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    var discoveredKeys = discoveredPermissions.Select(p => p.Key).ToHashSet();
    var existingKeys = existingPermissions.Keys.ToHashSet();

    var newKeys = discoveredKeys.Except(existingKeys).ToList();
    var obsoleteKeys = existingKeys.Except(discoveredKeys).ToList();

    var updatedPermissions = discoveredPermissions
        .Where(dp => existingKeys.Contains(dp.Key) && HasChanges(dp, existingPermissions[dp.Key]))
        .ToList();

    return new OperationPlans(
        NewPermissions: discoveredPermissions.Where(p => newKeys.Contains(p.Key)).ToList(),
        UpdatedPermissions: updatedPermissions,
        ObsoleteKeys: obsoleteKeys);
  }

  /// <summary>
  /// Procesa la creación de nuevos permisos usando UPSERT para manejar duplicados.
  /// </summary>
  private async Task<int> ProcessNewPermissionsAsync(
      IList<PermissionDescriptor> newPermissions,
      bool dryRun,
      CancellationToken cancellationToken)
  {
    var count = 0;
    foreach (var descriptor in newPermissions)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (!dryRun)
      {
        // Verificar si ya existe un permiso con esta clave (incluyendo inactivos)
        var existingPermission = await _permissionRepository.GetByKeyAsync(descriptor.Key, cancellationToken);

        if (existingPermission != null)
        {
          // Si existe, actualizarlo (equivale a reactivación + actualización)
          UpdatePermissionFromDescriptor(existingPermission, descriptor, 1);

          await _permissionRepository.UpdateAsync(existingPermission, cancellationToken);
        }
        else
        {
          // Si no existe, crearlo
          var entity = MapToEntity(descriptor);
          await _permissionRepository.AddAsync(entity, cancellationToken);
        }
      }
      count++;
    }
    return count;
  }

  /// <summary>
  /// Procesa la actualización de permisos existentes.
  /// </summary>
  private async Task<int> ProcessUpdatedPermissionsAsync(
      IList<PermissionDescriptor> updatedPermissions,
      Dictionary<string, Permission> existingPermissions,
      bool dryRun,
      CancellationToken cancellationToken)
  {
    var count = 0;
    foreach (var descriptor in updatedPermissions)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (!dryRun)
      {
        var existing = existingPermissions[descriptor.Key];

        // Preservar campos de auditoría y actualizar desde descriptor
        UpdatePermissionFromDescriptor(existing, descriptor, 1);
        existing.CreatedAt = existing.CreatedAt;
        existing.CreatedBy = existing.CreatedBy;
        existing.PreviousKey = existing.PreviousKey ?? existing.Key;
        existing.PreviousFilePath = existing.PreviousFilePath ?? existing.SourceFilePath;
        existing.PreviousMethodName = existing.PreviousMethodName ?? existing.EndpointMethodName;
        //_context.Entry(existing).State = EntityState.Detached;
        await _permissionRepository.UpdateAsync(existing, cancellationToken);
      }
      count++;
    }
    return count;
  }

  /// <summary>
  /// Procesa la obsolescencia de permisos que ya no existen.
  /// </summary>
  private async Task<int> ProcessObsoletePermissionsAsync(
      IList<string> obsoleteKeys,
      bool dryRun,
      CancellationToken cancellationToken)
  {
    var count = 0;
    foreach (var key in obsoleteKeys)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (!dryRun)
      {
        await _permissionRepository.MarkAsObsoleteAsync(key, cancellationToken);
        count++;
      }
      else
      {
        count++;
      }
    }
    return count;
  }

  /// <summary>
  /// Construye la respuesta de sincronización con estadísticas.
  /// </summary>
  private SyncPermissionsResponseDto BuildSyncResponse(
      SyncOperationResult syncResult,
      long executionTimeMs,
      bool dryRun)
  {
    var stats = _permissionRepository.Stats;
    var databaseOperations = new DatabaseOperationStatistics(
        TotalQueriesExecuted: stats.QueriesExecuted,
        InsertOperations: stats.Inserted,
        UpdateOperations: stats.Updated,
        SoftDeleteOperations: stats.Blocked, // Obsoletos considerados como bloqueados
        RecoveryOperations: stats.Restored,
        TransactionDurationMs: executionTimeMs);

    return new SyncPermissionsResponseDto(
        PermissionsCreated: syncResult.PermissionsCreated,
        PermissionsUpdated: syncResult.PermissionsUpdated,
        PermissionsObsoleted: syncResult.PermissionsObsoleted,
        PermissionsRecovered: 0,
        ExecutionTimeMs: executionTimeMs,
        DatabaseOperations: databaseOperations,
        ProcessingErrors: new List<string>(),
        ProcessingWarnings: new List<string>(),
        DryRunMode: dryRun);
  }

  /// <summary>
  /// Registra la finalización exitosa de la sincronización.
  /// </summary>
  private void LogSyncCompletion(SyncPermissionsResponseDto response)
  {
    _logger.LogSyncProcessCompleted(
        response.PermissionsCreated,
        response.PermissionsUpdated,
        response.PermissionsObsoleted,
        response.ExecutionTimeMs);
  }

  /// <summary>
  /// Maneja errores durante la sincronización.
  /// </summary>
  private OperationResult<SyncPermissionsResponseDto> HandleSyncErrorAsync(
      Exception ex,
      bool dryRun)
  {
    var errorMessage = $"Error durante la sincronización: {ex.Message}";
    _logger.LogPermissionsSyncFailed(errorMessage);

    return OperationResult.Fail<SyncPermissionsResponseDto>(
        ErrorInfo.Status500Unknown,
        errorMessage);
  }

  /// <summary>
  /// Mapea un PermissionDescriptor a una entidad Permission.
  /// </summary>
  private static Permission MapToEntity(PermissionDescriptor descriptor)
  {
    return new Permission
    {
      Key = descriptor.Key,
      Description = descriptor.Description,
      Group = descriptor.Feature,
      IsActive = true,
      IsSystemGenerated = true,
      Route = descriptor.Routes?.FirstOrDefault(),
      HttpMethod = descriptor.HttpMethods?.FirstOrDefault(),
      FullNamespace = ExtractNamespaceFromSourcePath(descriptor.SourceFilePath),
      SourceFilePath = descriptor.SourceFilePath,
      EndpointMethodName = descriptor.EndpointMethodName,
      CqrsType = descriptor.CqrsType,
      HasPermissionsNotRequired = descriptor.HasPermissionsNotRequired,
      PermissionsNotRequiredReason = descriptor.PermissionsNotRequiredReason,
      RequiresDevelopmentAccess = descriptor.RequiresDevelopmentAccess,
      RequiresDevelopmentAccessReason = descriptor.RequiresDevelopmentAccessReason,
      RequiresSuAdminAccess = descriptor.RequiresSuAdminAccess,
      RequiresSuAdminAccessReason = descriptor.RequiresSuAdminAccessReason,
      HasRequirePermissions = descriptor.HasRequirePermissions,
      RequirePermissionsNote = descriptor.RequirePermissionsNote,
      LastSyncAt = DateTime.UtcNow,
      CreatedAt = DateTime.UtcNow,
      CreatedBy = 1,
      UpdatedAt = DateTime.UtcNow,
      UpdatedBy = 1,
    };
  }

  /// <summary>
  /// Determina si hay cambios entre un descriptor descubierto y un permiso existente.
  /// </summary>
  private static bool HasChanges(PermissionDescriptor discovered, Permission existing)
  {
    return discovered.Description != existing.Description ||
           discovered.Routes?.FirstOrDefault() != existing.Route ||
           discovered.HttpMethods?.FirstOrDefault() != existing.HttpMethod ||
           discovered.SourceFilePath != existing.SourceFilePath ||
           discovered.EndpointMethodName != existing.EndpointMethodName ||
           discovered.CqrsType != existing.CqrsType ||
           discovered.HasPermissionsNotRequired != existing.HasPermissionsNotRequired ||
           discovered.PermissionsNotRequiredReason != existing.PermissionsNotRequiredReason ||
           discovered.RequiresDevelopmentAccess != existing.RequiresDevelopmentAccess ||
           discovered.RequiresDevelopmentAccessReason != existing.RequiresDevelopmentAccessReason ||
           discovered.RequiresSuAdminAccess != existing.RequiresSuAdminAccess ||
           discovered.RequiresSuAdminAccessReason != existing.RequiresSuAdminAccessReason ||
           discovered.HasRequirePermissions != existing.HasRequirePermissions ||
           discovered.RequirePermissionsNote != existing.RequirePermissionsNote;
  }

  /// <summary>
  /// Extrae el namespace completo desde el path del archivo fuente.
  /// </summary>
  private static string ExtractNamespaceFromSourcePath(string? sourceFilePath)
  {
    if (string.IsNullOrEmpty(sourceFilePath))
      return string.Empty;

    // Convertir path a namespace: Features\Users\Commands\CreateUser -> Features.Users.Commands.CreateUser
    var namespacePart = sourceFilePath.Replace('\\', '.').Replace('/', '.');

    // Remover la extensión del archivo
    var lastDotIndex = namespacePart.LastIndexOf('.');
    if (lastDotIndex > 0)
    {
      namespacePart = namespacePart.Substring(0, lastDotIndex);
    }

    return $"FrugoCorp.AspNetCore.AutoDiscoveryIAM.{namespacePart}";
  }

  /// <summary>
  /// Actualiza los campos de un Permission existente basándose en un PermissionDescriptor.
  /// </summary>
  /// <param name="permission">Permission a actualizar.</param>
  /// <param name="descriptor">Descriptor con los datos nuevos.</param>
  /// <param name="updatedBy">Usuario/sistema que realiza la actualización.</param>
  private static void UpdatePermissionFromDescriptor(Permission permission, PermissionDescriptor descriptor, long updatedBy)
  {
    permission.Key = descriptor.Key;
    permission.Description = descriptor.Description;
    permission.Group = descriptor.Feature;
    permission.IsActive = true;
    permission.IsSystemGenerated = true;

    // Si el permiso venía soft-deleted (ej. GetByKeyAsync lo encontró vía IgnoreQueryFilters porque
    // se había marcado obsoleto en un sync anterior y ahora reaparece), revertir el soft-delete —
    // de lo contrario el filtro global (!IsDeleted) lo seguiría ocultando pese a IsActive=true.
    if (permission.IsDeleted)
    {
      permission.IsDeleted = false;
      permission.DeletedAt = null;
      permission.DeletedBy = null;
      permission.RestoredAt = DateTime.UtcNow;
      permission.RestoredBy = updatedBy;
    }
    permission.Route = descriptor.Routes?.FirstOrDefault();
    permission.HttpMethod = descriptor.HttpMethods?.FirstOrDefault();
    permission.FullNamespace = ExtractNamespaceFromSourcePath(descriptor.SourceFilePath);
    permission.SourceFilePath = descriptor.SourceFilePath;
    permission.EndpointMethodName = descriptor.EndpointMethodName;
    permission.CqrsType = descriptor.CqrsType;
    permission.HasPermissionsNotRequired = descriptor.HasPermissionsNotRequired;
    permission.PermissionsNotRequiredReason = descriptor.PermissionsNotRequiredReason;
    permission.RequiresDevelopmentAccess = descriptor.RequiresDevelopmentAccess;
    permission.RequiresDevelopmentAccessReason = descriptor.RequiresDevelopmentAccessReason;
    permission.RequiresSuAdminAccess = descriptor.RequiresSuAdminAccess;
    permission.RequiresSuAdminAccessReason = descriptor.RequiresSuAdminAccessReason;
    permission.HasRequirePermissions = descriptor.HasRequirePermissions;
    permission.RequirePermissionsNote = descriptor.RequirePermissionsNote;
    permission.LastSyncAt = DateTime.UtcNow;
    permission.UpdatedAt = DateTime.UtcNow;
    permission.UpdatedBy = updatedBy;
  }

  /// <summary>
  /// Ejecuta el algoritmo de 3 niveles para recuperar permisos existentes.
  /// </summary>
  /// <param name="existingPermissionId">ID del permiso existente a recuperar.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>Resultado de la operación con descriptor actualizado del permiso o null si no se pudo recuperar.</returns>
  public async Task<OperationResult<PermissionDescriptor?>> RecoverPermissionAsync(
    long existingPermissionId,
    CancellationToken cancellationToken = default)
  {
    if (existingPermissionId <= 0)
    {
      return OperationResult.Fail<PermissionDescriptor?>(ErrorInfo.Status400Validation, PermissionValidationTypes.IdGreaterThanZero.Message);
    }
    try
    {
      var permission = await FindPermissionByIdAsync(existingPermissionId, cancellationToken);
      if (permission == null)
      {
        return OperationResult.Ok<PermissionDescriptor?>(null);
      }
      var recovered = await ApplyRecoveryAlgorithm(permission, existingPermissionId);
      cancellationToken.ThrowIfCancellationRequested();
      return OperationResult.Ok(recovered);
    }
    catch (OperationCanceledException)
    {
      // La operación fue cancelada por un cancellationToken.ThrowIfCancellationRequested(); o similar
      // Se envió un cancellationToken previamente cancelado
      // Volvemos a lanzar la misma exception que fue capturada en este caso OperationCanceledException
      throw;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error al recuperar permiso {existingPermissionId}: {ex.Message}");
      return await HandleRecoveryErrorAsync(ex, existingPermissionId);
    }
  }

  /// <summary>
  /// Busca un permiso por su ID.
  /// </summary>
  private async Task<Permission?> FindPermissionByIdAsync(long permissionId, CancellationToken cancellationToken)
  {
    return await _permissionRepository.GetByIdAsync(permissionId, cancellationToken);
  }

  /// <summary>
  /// Aplica el algoritmo de 3 niveles para recuperar un permiso.
  /// </summary>
  private Task<PermissionDescriptor?> ApplyRecoveryAlgorithm(Permission permission, long permissionId)
  {
    if (permission == null) return Task.FromResult<PermissionDescriptor?>(null);
    // Nivel 1: Permiso activo y válido
    if (permission.IsActive && !string.IsNullOrEmpty(permission.Key))
    {
      var recovered = MapToDescriptor(permission);
      recovered.ValidationWarnings.Add("Permiso estaba activo y es válido");
      _logger.LogPermissionRetrieved(permissionId, permission.Key);
      return Task.FromResult<PermissionDescriptor?>(recovered);
    }

    // Nivel 2: Permiso inactivo pero recuperable
    if (!permission.IsActive && !string.IsNullOrEmpty(permission.Key))
    {
      var recovered = MapToDescriptor(permission);
      recovered.ValidationWarnings.Add("Permiso estaba marcado como inactivo");
      _logger.LogPermissionRetrieved(permissionId, permission.Key);
      return Task.FromResult<PermissionDescriptor?>(recovered);
    }

    // Nivel 3: Intentar recuperar desde campos de auditoría
    if (!string.IsNullOrEmpty(permission.PreviousKey))
    {
      var recovered = RecoverFromAuditFields(permission, permissionId);
      recovered.ValidationWarnings.Add("Permiso recuperado desde campos de auditoría");
      _logger.LogPermissionRetrieved(permissionId, recovered.Key);
      return Task.FromResult<PermissionDescriptor?>(recovered);
    }
    return Task.FromResult<PermissionDescriptor?>(null);
  }

  /// <summary>
  /// Recupera un permiso desde los campos de auditoría.
  /// </summary>
  private static PermissionDescriptor RecoverFromAuditFields(Permission permission, long permissionId)
  {
    return new PermissionDescriptor
    {
      Key = permission.PreviousKey!,
      Feature = permission.Group ?? "unknown",
      Action = ExtractActionFromKey(permission.PreviousKey!),
      Description = permission.Description ?? $"Permiso recuperado desde auditoría (ID: {permissionId})",
      CqrsType = permission.CqrsType ?? "Unknown",
      SourceFilePath = permission.PreviousFilePath ?? permission.SourceFilePath ?? "Unknown",
      EndpointMethodName = permission.PreviousMethodName ?? permission.EndpointMethodName ?? "Unknown",
      HttpMethods = !string.IsNullOrEmpty(permission.HttpMethod)
          ? new List<string> { permission.HttpMethod }
          : new List<string> { "POST" },
      Routes = !string.IsNullOrEmpty(permission.Route)
          ? new List<string> { permission.Route }
          : new List<string> { $"/api/{permission.Group ?? "unknown"}" },
      IsObsolete = true,
      DiscoveredAt = permission.LastSyncAt ?? permission.UpdatedAt ?? permission.CreatedAt,
      ContentHash = $"recovered_{permissionId}",
      ValidationErrors = new List<string>(),
      ValidationWarnings = new List<string> { "Permiso recuperado desde campos de auditoría" },
    };
  }

  /// <summary>
  /// Maneja errores durante la recuperación de permisos.
  /// </summary>
  private Task<OperationResult<PermissionDescriptor?>> HandleRecoveryErrorAsync(Exception ex, long permissionId)
  {
    var errorMessage = $"Error al recuperar permiso {permissionId}: {ex.Message}";
    _logger.LogPermissionsSyncFailed(errorMessage);
    return Task.FromResult(OperationResult.Fail<PermissionDescriptor?>(ErrorInfo.Status500Unknown, errorMessage));
  }  /// <summary>
     /// Mapea una entidad Permission a un PermissionDescriptor.
     /// </summary>
  private static PermissionDescriptor MapToDescriptor(Permission permission)
  {
    return new PermissionDescriptor
    {
      Key = permission.Key,
      Feature = permission.Group ?? ExtractFeatureFromKey(permission.Key),
      Action = ExtractActionFromKey(permission.Key),
      Description = permission.Description ?? $"Permiso {permission.Key}",
      CqrsType = permission.CqrsType ?? "Unknown",
      SourceFilePath = permission.SourceFilePath ?? "Unknown",
      EndpointMethodName = permission.EndpointMethodName ?? "Unknown",
      HttpMethods = !string.IsNullOrEmpty(permission.HttpMethod)
            ? new List<string> { permission.HttpMethod }
            : new List<string> { "POST" },
      Routes = !string.IsNullOrEmpty(permission.Route)
            ? new List<string> { permission.Route }
            : new List<string> { $"/api/{permission.Group ?? "unknown"}" },
      IsObsolete = !permission.IsActive,
      DiscoveredAt = permission.LastSyncAt ?? permission.UpdatedAt ?? permission.CreatedAt,
      ContentHash = $"{permission.Key}_{permission.UpdatedAt:yyyyMMddHHmmss}",
      ValidationErrors = new List<string>(),
      ValidationWarnings = new List<string>(),
    };
  }

  /// <summary>
  /// Extrae la feature de una key de permiso.
  /// </summary>
  private static string ExtractFeatureFromKey(string key)
  {
    var parts = key?.Split('.') ?? Array.Empty<string>();
    return parts.Length > 0 ? parts[0] : "unknown";
  }

  /// <summary>
  /// Extrae la action de una key de permiso.
  /// </summary>
  private static string ExtractActionFromKey(string key)
  {
    var parts = key?.Split('.') ?? Array.Empty<string>();
    return parts.Length > 1 ? parts[1] : "unknown";
  }
}
