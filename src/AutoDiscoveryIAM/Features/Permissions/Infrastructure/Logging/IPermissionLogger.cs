// -----------------------------------------------------------------------
// <copyright file="IPermissionLogger.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Logging.Logger;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;

/// <summary>
/// Interfaz para el logger especializado en operaciones sobre permisos RBAC.
/// </summary>
public interface IPermissionLogger : ILoggerBase<IPermissionLogger>
{
  /// <summary>
  /// Registra el inicio del proceso de descubrimiento de permisos.
  /// </summary>
  /// <param name="forceFullScan">Indica si se fuerza un escaneo completo.</param>
  void LogDiscoveryStarted(bool forceFullScan);

  /// <summary>
  /// Registra la finalización del proceso de descubrimiento de permisos.
  /// </summary>
  /// <param name="permissionsFound">Número de permisos descubiertos.</param>
  /// <param name="executionTimeMs">Tiempo de ejecución en milisegundos.</param>
  void LogDiscoveryCompleted(int permissionsFound, long executionTimeMs);

  /// <summary>
  /// Registra un permiso individual descubierto durante el proceso de discovery.
  /// </summary>
  /// <param name="permissionKey">Clave del permiso descubierto.</param>
  /// <param name="feature">Feature a la que pertenece.</param>
  /// <param name="action">Acción del permiso.</param>
  /// <param name="hasAuthAttributes">Indica si tiene atributos de autorización.</param>
  void LogPermissionDiscovered(string permissionKey, string feature, string action, bool hasAuthAttributes);

  /// <summary>
  /// Registra errores de validación encontrados durante el descubrimiento.
  /// </summary>
  /// <param name="errorCount">Número de errores encontrados.</param>
  /// <param name="errors">Lista de errores de validación.</param>
  void LogValidationErrors(int errorCount, IList<string> errors);

  /// <summary>
  /// Registra cuando no se encuentran permisos para sincronizar.
  /// </summary>
  void LogNoPermissionsFound();

  /// <summary>
  /// Registra el inicio del proceso de sincronización de permisos (para servicios internos).
  /// </summary>
  /// <param name="permissionCount">Número de permisos a sincronizar.</param>
  /// <param name="dryRun">Indica si es una ejecución de prueba.</param>
  void LogSyncProcessStarted(int permissionCount, bool dryRun);

  /// <summary>
  /// Registra la finalización del proceso de sincronización de permisos (para servicios internos).
  /// </summary>
  /// <param name="created">Número de permisos creados.</param>
  /// <param name="updated">Número de permisos actualizados.</param>
  /// <param name="obsoleted">Número de permisos marcados como obsoletos.</param>
  /// <param name="executionTimeMs">Tiempo de ejecución en milisegundos.</param>
  /// <param name="success">Indica si la operación se realizó con éxito.</param>
  void LogSyncProcessCompleted(int created, int updated, int obsoleted, long executionTimeMs, bool success = true);  /// <summary>
                                                                                                                     /// Registra el inicio del proceso completo de sincronización de permisos.
                                                                                                                     /// </summary>
                                                                                                                     /// <param name="forceFullSync">Indica si se fuerza un escaneo completo.</param>
                                                                                                                     /// <param name="dryRun">Indica si es una ejecución de prueba.</param>
  void LogPermissionsSyncStarted(bool forceFullSync, bool dryRun);

  /// <summary>
  /// Registra la finalización del proceso completo de sincronización de permisos.
  /// </summary>
  /// <param name="permissionsProcessed">Número total de permisos procesados.</param>
  /// <param name="executionTimeMs">Tiempo total de ejecución en milisegundos.</param>
  /// <param name="dryRun">Indica si fue una ejecución de prueba.</param>
  /// <param name="success">Indica si la operación se realizó con éxito.</param>
  void LogPermissionsSyncCompleted(int permissionsProcessed, long executionTimeMs, bool dryRun, bool success = true);

  /// <summary>
  /// Registra cuando se persisten los cambios en la base de datos.
  /// </summary>
  /// <param name="permissionsProcessed">Número de permisos procesados.</param>
  /// <param name="success">Indica si la operación se realizó con éxito.</param>
  void LogPermissionsSyncPersisted(int permissionsProcessed, bool success = true);

  /// <summary>
  /// Registra cuando no se encuentra un permiso específico.
  /// </summary>
  /// <param name="permissionKey">Clave del permiso no encontrado.</param>
  void LogPermissionNotFound(string permissionKey);

  /// <summary>
  /// Registra el número total de permisos recuperados.
  /// </summary>
  /// <param name="count">Total de permisos recuperados.</param>
  void LogPermissionsRetrieved(int count);

  /// <summary>
  /// Registra un permiso específico recuperado.
  /// </summary>
  /// <param name="permissionId">ID del permiso recuperado.</param>
  /// <param name="permissionKey">Clave del permiso recuperado.</param>
  void LogPermissionRetrieved(long permissionId, string permissionKey);

  /// <summary>
  /// Registra cuando falla la validación de un permiso específico.
  /// </summary>
  /// <param name="permissionKey">Clave del permiso que falló la validación.</param>
  /// <param name="reason">Razón por la cual falló la validación.</param>
  void LogPermissionValidationFailed(string permissionKey, string reason);

  /// <summary>
  /// Registra errores en la sincronización de permisos.
  /// </summary>
  /// <param name="errorMessage">Descripción del error ocurrido en la sincronización.</param>
  void LogPermissionsSyncFailed(string errorMessage);

  /// <summary>
  /// Registra errores durante el descubrimiento de permisos.
  /// </summary>
  /// <param name="errorMessage">Descripción del error ocurrido en el descubrimiento.</param>
  void LogDiscoveryError(string errorMessage);
}
