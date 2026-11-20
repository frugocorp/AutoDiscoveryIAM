// -----------------------------------------------------------------------
// <copyright file="PermissionErrorMessagesCatalog.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Catalogs;

/// <summary>
/// Catálogo de mensajes de error para operaciones relacionadas con permisos.
/// Centraliza todos los mensajes de error para mantener consistencia entre servicios y pruebas.
/// </summary>
public static class PermissionErrorMessagesCatalog
{
  /// <summary>
  /// Error cuando no se pueden descubrir permisos en el sistema.
  /// </summary>
  public const string NoPermissionsDiscovered = "No se pudieron descubrir permisos en el sistema";

  /// <summary>
  /// Error durante la validación de consistencia de permisos.
  /// </summary>
  public const string ValidationConsistencyError = "Error validando consistencia de permisos";

  /// <summary>
  /// Error durante la sincronización con la base de datos.
  /// </summary>
  public const string SyncDatabaseError = "Error durante la sincronización con la base de datos";

  /// <summary>
  /// Error genérico en el servicio de descubrimiento de permisos.
  /// </summary>
  public const string DiscoveryServiceError = "Error en discovery";

  /// <summary>
  /// Error genérico en el servicio de sincronización de permisos.
  /// </summary>
  public const string SyncServiceError = "Error en sync";

  /// <summary>
  /// Error genérico en el análisis de permisos actualizados.
  /// </summary>
  public const string AnalysisServiceError = "Error en análisis";

  /// <summary>
  /// Error de validación con errores específicos.
  /// </summary>
  public const string ValidationErrors = "Errores de validación";
}
