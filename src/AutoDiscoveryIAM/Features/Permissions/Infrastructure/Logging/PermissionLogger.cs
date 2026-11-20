// -----------------------------------------------------------------------
// <copyright file="PermissionLogger.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;
using Microsoft.Extensions.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;

/// <summary>
/// Logger especializado para operaciones sobre permisos RBAC.
/// </summary>
/// <remarks>
/// Permite registrar información, advertencias y errores específicos del dominio de permisos.
/// Utilizado por handlers y servicios relacionados con la gestión de permisos RBAC.
/// </remarks>
public class PermissionLogger(ILogger<PermissionLogger> logger) : LoggerBase<PermissionLogger>(logger), IPermissionLogger
{
  /// <inheritdoc />
  public void LogDiscoveryStarted(bool forceFullScan)
  {
    LogInformation($"Iniciando descubrimiento de permisos. ForceFullScan: {forceFullScan}", null);
  }

  /// <inheritdoc />
  public void LogDiscoveryCompleted(int permissionsFound, long executionTimeMs)
  {
    LogInformation($"Descubrimiento completado. Permisos encontrados: {permissionsFound}, Tiempo: {executionTimeMs}ms", null);
  }

  /// <inheritdoc />
  public void LogPermissionDiscovered(string permissionKey, string feature, string action, bool hasAuthAttributes)
  {
    var authIndicator = hasAuthAttributes ? "✓ Con atributos de autorización" : "○ Sin atributos especiales";
    LogInformation($"Permiso descubierto: {permissionKey} | Feature: {feature} | Action: {action} | {authIndicator}", null);
  }

  /// <inheritdoc />
  public void LogValidationErrors(int errorCount, IList<string> errors)
  {
    var errorList = string.Join("; ", errors);
    LogError($"Errores de validación encontrados ({errorCount}): {errorList}", null);
  }

  /// <inheritdoc />
  public void LogNoPermissionsFound()
  {
    LogWarning("No se encontraron permisos para sincronizar", null);
  }

  /// <inheritdoc />
  public void LogSyncProcessStarted(int permissionCount, bool dryRun)
  {
    LogInformation($"Iniciando proceso de sincronización - Permisos: {permissionCount}, DryRun: {dryRun}", null);
  }

  /// <inheritdoc />
  public void LogSyncProcessCompleted(int created, int updated, int obsoleted, long executionTimeMs, bool success = true)
  {
    if (success)
    {
      LogInformation($"Proceso de sincronización completado - Creados: {created}, Actualizados: {updated}, Obsoletos: {obsoleted}, Tiempo: {executionTimeMs}ms", null);
    }
    else
    {
      LogError($"Proceso de sincronización NO completado - Creados: {created}, Actualizados: {updated}, Obsoletos: {obsoleted}, Tiempo: {executionTimeMs}ms", null);
    }
  }

  /// <inheritdoc />
  public void LogPermissionsSyncStarted(bool forceFullSync, bool dryRun)
  {
    LogInformation($"Iniciando sincronización de permisos - ForceFullSync: {forceFullSync}, DryRun: {dryRun}", null);
  }

  /// <inheritdoc />
  public void LogPermissionsSyncCompleted(int permissionsProcessed, long executionTimeMs, bool dryRun, bool success = true)
  {
    if (success)
    {
      LogInformation($"Sincronización de permisos completada - Permisos: {permissionsProcessed}, Tiempo: {executionTimeMs}ms, DryRun: {dryRun}", null);
    }
    else
    {
      LogError($"Sincronización de permisos NO completada - Permisos: {permissionsProcessed}, Tiempo: {executionTimeMs}ms, DryRun: {dryRun}", null);
    }
  }

  /// <inheritdoc />
  public void LogPermissionsSyncPersisted(int permissionsProcessed, bool success = true)
  {
    if (success)
    {
      LogInformation($"Cambios de permisos persistidos en base de datos - Permisos procesados: {permissionsProcessed}", null);
    }
    else
    {
      LogError($"Cambios de permisos NO persistidos en base de datos - Permisos procesados: {permissionsProcessed}", null);
    }
  }

  /// <inheritdoc />
  public void LogPermissionNotFound(string permissionKey)
  {
    LogWarning($"Permiso no encontrado - Key: {permissionKey}", null);
  }

  /// <inheritdoc />
  public void LogPermissionsRetrieved(int count)
  {
    LogInformation($"Permisos recuperados - Total: {count}", null);
  }

  /// <inheritdoc />
  public void LogPermissionRetrieved(long permissionId, string permissionKey)
  {
    LogInformation($"Permiso recuperado - Id: {permissionId}, Key: {permissionKey}", null);
  }

  /// <inheritdoc />
  public void LogPermissionValidationFailed(string permissionKey, string reason)
  {
    LogWarning($"Validación de permiso falló - Key: {permissionKey}, Razón: {reason}", null);
  }

  /// <inheritdoc />
  public void LogPermissionsSyncFailed(string errorMessage)
  {
    LogError($"Error en la sincronización de permisos - Error: {errorMessage}", null);
  }

  /// <inheritdoc />
  public void LogDiscoveryError(string errorMessage)
  {
    LogError($"Error en el descubrimiento de permisos - Error: {errorMessage}", null);
  }
}
