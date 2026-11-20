// -----------------------------------------------------------------------
// <copyright file="DiscoveryPermissionsService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Repositories;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;

/// <summary>
/// Servicio de descubrimiento de permisos vía <see cref="EndpointDiscoveryService"/>.
/// </summary>
/// <remarks>
/// Único mecanismo de descubrimiento: lee <c>EndpointDataSource</c> (la tabla de ruteo nativa de
/// ASP.NET Core) en runtime, sin adivinar HTTP method/ruta y sin reflection sobre archivos fuente.
/// Si el descubrimiento por metadatos no encuentra nada o falla, esto se reporta como un error --
/// no existe un mecanismo de respaldo que oculte una app mal configurada (ej. sin endpoints mapeados
/// todavía, o llamado antes de que el pipeline de ASP.NET Core esté listo).
/// </remarks>
public class DiscoveryPermissionsService : IDiscoveryPermissionsService
{
  private readonly IPermissionLogger _logger;
  private readonly IEndpointDiscoveryService _endpointDiscoveryService;
  private readonly ISyncPermissionsRepository _permissionRepository;

  public DiscoveryPermissionsService(
      IPermissionLogger logger,
      IEndpointDiscoveryService endpointDiscoveryService,
      ISyncPermissionsRepository permissionRepository)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _endpointDiscoveryService = endpointDiscoveryService ?? throw new ArgumentNullException(nameof(endpointDiscoveryService));
    _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
  }

  /// <inheritdoc />
  public async Task<OperationResult<IList<PermissionDescriptor>>> DiscoverPermissionsAsync(
      bool forceFullScan = false,
      bool enableDetailedConsoleOutput = false,
      CancellationToken cancellationToken = default)
  {
    // Verificar cancelación al inicio - lanzar TaskCanceledException inmediatamente si está cancelado
    if (cancellationToken.IsCancellationRequested)
    {
      throw new TaskCanceledException();
    }

    try
    {
      _logger.LogDiscoveryStarted(forceFullScan);
      var startTime = DateTime.UtcNow;

      var permissions = await _endpointDiscoveryService.DiscoverEndpointsFromMetadataAsync(cancellationToken);

      if (permissions.Count == 0)
      {
        return OperationResult.Fail<IList<PermissionDescriptor>>(ErrorInfo.Status404NotFound, PermissionErrorMessagesCatalog.NoPermissionsDiscovered);
      }

      // Obtener permisos existentes de la BD para comparación
      var existingPermissions = await GetExistingPermissionsAsync(cancellationToken);

      // Loggear permisos descubiertos con su estado
      LogDiscoveredPermissions(permissions, existingPermissions, enableDetailedConsoleOutput);

      var executionTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
      _logger.LogDiscoveryCompleted(permissions.Count, executionTime);

      return OperationResult.Ok<IList<PermissionDescriptor>>(permissions);
    }
    catch (OperationCanceledException)
    {
      // Re-lanzar como TaskCanceledException para las pruebas
      throw new TaskCanceledException();
    }
    catch (Exception ex)
    {
      _logger.LogDiscoveryError($"Error en el descubrimiento de permisos: {ex.Message}");
      return OperationResult.Fail<IList<PermissionDescriptor>>(ErrorInfo.Status500Unknown, PermissionErrorMessagesCatalog.DiscoveryServiceError);
    }
  }

  /// <inheritdoc />
  public OperationResult<IList<string>> ValidateConsistency(IList<PermissionDescriptor> descriptors)
  {
    // Validar null directamente sin try-catch para permitir que la excepción se propague
    ArgumentNullException.ThrowIfNull(descriptors);

    try
    {
      var errors = new List<string>();

      errors.AddRange(ValidateDuplicateKeys(descriptors));
      errors.AddRange(ValidateRequiredFields(descriptors));
      errors.AddRange(ValidateRoutes(descriptors));
      errors.AddRange(ValidateHttpMethods(descriptors));

      if (errors.Count > 0)
      {
        _logger.LogValidationErrors(errors.Count, errors);
        // Si hay errores de validación, la operación falla -- con el detalle en el mensaje: quien
        // llama al sync necesita saber QUÉ clave/namespace lo rompió, no solo que algo falló.
        return OperationResult.Fail<IList<string>>(
            ErrorInfo.Status400Validation,
            $"{PermissionErrorMessagesCatalog.ValidationErrors}: {string.Join("; ", errors)}");
      }

      // LÓGICA CORRECTA: Si no hay errores, la operación es exitosa con lista vacía
      return OperationResult.Ok<IList<string>>(errors);
    }
    catch (Exception ex)
    {
      var errorMsg = $"{PermissionErrorMessagesCatalog.ValidationConsistencyError}: {ex.Message}";
      return OperationResult.Fail<IList<string>>(ErrorInfo.Status500Unknown, errorMsg);
    }
  }

  /// <inheritdoc />
  public IList<string> FindEndpointsWithoutPermission()
  {
    return _endpointDiscoveryService.FindEndpointsWithoutPermission() ?? [];
  }

  /// <summary>
  /// Dos slices distintos (namespaces distintos) que producen la misma clave: cada uno quedaría
  /// autorizado por el permiso del otro. Se rechaza nombrando ambos namespaces, para que quien
  /// consume el paquete sepa qué carpeta renombrar.
  /// </summary>
  private static IEnumerable<string> ValidateDuplicateKeys(IList<PermissionDescriptor> descriptors)
  {
    return descriptors
        .GroupBy(p => p.Key)
        .Where(g => g.Count() > 1)
        .Select(g =>
        {
          var namespaces = g.Select(p => p.SourceNamespace).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
          return namespaces.Count > 1
              ? $"Clave duplicada: {g.Key} (la producen {string.Join(" y ", namespaces)}; renombra el slice o su feature para que no compartan permiso)"
              : $"Clave duplicada: {g.Key}";
        });
  }

  private static IEnumerable<string> ValidateRequiredFields(IList<PermissionDescriptor> descriptors)
  {
    return descriptors
        .Where(p => string.IsNullOrWhiteSpace(p.Key) ||
                   string.IsNullOrWhiteSpace(p.Feature) ||
                   string.IsNullOrWhiteSpace(p.Action))
        .Select(p => $"Permiso inválido con clave '{p.Key}': campos requeridos faltantes");
  }

  private static IEnumerable<string> ValidateRoutes(IList<PermissionDescriptor> descriptors)
  {
    return descriptors
        .Where(p => p.Routes == null || p.Routes.Count == 0)
        .Select(p => $"Permiso '{p.Key}': sin rutas definidas");
  }

  private static IEnumerable<string> ValidateHttpMethods(IList<PermissionDescriptor> descriptors)
  {
    return descriptors
        .Where(p => p.HttpMethods == null || p.HttpMethods.Count == 0)
        .Select(p => $"Permiso '{p.Key}': sin métodos HTTP definidos");
  }

  /// <summary>
  /// Obtiene los permisos existentes de la base de datos como diccionario.
  /// </summary>
  private async Task<Dictionary<string, Permission>> GetExistingPermissionsAsync(CancellationToken cancellationToken)
  {
    try
    {
      var existing = await _permissionRepository.GetAllAsync(cancellationToken);
      return existing.ToDictionary(p => p.Key, p => p);
    }
    catch
    {
      // Si falla, retornar diccionario vacío (todos serán nuevos)
      return new Dictionary<string, Permission>();
    }
  }

  /// <summary>
  /// Registra en log y consola cada permiso descubierto con su estado (Nuevo/Modificado/Sin cambios).
  /// </summary>
  private void LogDiscoveredPermissions(
      IList<PermissionDescriptor> permissions,
      Dictionary<string, Permission>? existingPermissions = null,
      bool enableDetailedConsoleOutput = false)
  {
    foreach (var permission in permissions)
    {
      var hasAuthAttrs = permission.HasPermissionsNotRequired ||
                        permission.RequiresDevelopmentAccess ||
                        permission.RequiresSuAdminAccess ||
                        permission.HasRequirePermissions;
      _logger.LogPermissionDiscovered(permission.Key, permission.Feature, permission.Action, hasAuthAttrs);

      // Solo imprimir en consola si está habilitado
      if (enableDetailedConsoleOutput)
      {
        // Determinar estado del permiso
        var status = "Nuevo       ";
        if (existingPermissions != null && existingPermissions.TryGetValue(permission.Key, out var existing))
        {
          // Comparar si hay cambios significativos (incluyendo TODOS los campos de autorización)
          var hasChanges = existing.Description != permission.Description ||
                          existing.HasPermissionsNotRequired != permission.HasPermissionsNotRequired ||
                          existing.PermissionsNotRequiredReason != permission.PermissionsNotRequiredReason ||
                          existing.RequiresDevelopmentAccess != permission.RequiresDevelopmentAccess ||
                          existing.RequiresDevelopmentAccessReason != permission.RequiresDevelopmentAccessReason ||
                          existing.RequiresSuAdminAccess != permission.RequiresSuAdminAccess ||
                          existing.RequiresSuAdminAccessReason != permission.RequiresSuAdminAccessReason ||
                          existing.HasRequirePermissions != permission.HasRequirePermissions ||
                          existing.RequirePermissionsNote != permission.RequirePermissionsNote;

          status = hasChanges ? "Modificado  " : "Sin cambios ";
        }

        // Imprimir en consola para tests
        var authIndicator = hasAuthAttrs ? "✓ Con autorización" : "○ Sin atributos";
        Console.WriteLine($"  → {permission.Key,-50} | {permission.Feature,-20} | {status} | {authIndicator}");
      }
    }
  }
}
