// -----------------------------------------------------------------------
// <copyright file="EndpointDiscoveryService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Dtos;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Services.PermissionNaming;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;

/// <summary>
/// Implementación del servicio de descubrimiento de endpoints que accede directamente a
/// <see cref="EndpointDataSource"/> en runtime -- único mecanismo de descubrimiento de permisos.
/// Da la ruta/HTTP method reales (sin adivinar por nombre de método) y la descripción vía
/// <c>.WithSummary()</c>/<c>.WithDescription()</c> -- metadatos nativos de ASP.NET Core, no de
/// Swashbuckle/Microsoft.OpenApi.
/// </summary>
public class EndpointDiscoveryService : IEndpointDiscoveryService
{
  private readonly IServiceProvider _serviceProvider;
  private readonly IPermissionNamingService _permissionNamingService;

  public EndpointDiscoveryService(
      IServiceProvider serviceProvider,
      IPermissionNamingService permissionNamingService)
  {
    _serviceProvider = serviceProvider;
    _permissionNamingService = permissionNamingService;
  }

  /// <summary>
  /// Descubre todos los endpoints registrados en la aplicación y extrae sus metadatos de autorización.
  /// </summary>
  /// <returns>Lista de descriptores de permisos extraídos de metadatos de endpoints.</returns>
  public async Task<IList<PermissionDescriptor>> DiscoverEndpointsFromMetadataAsync(CancellationToken cancellationToken)
  {
    await Task.Yield();
    var permissions = new List<PermissionDescriptor>();

    try
    {
      // Obtener acceso al EndpointDataSource para acceder a endpoints registrados
      var endpointDataSource = ResolveEndpointDataSource();
      if (endpointDataSource == null)
      {
        return permissions;
      }

      var endpoints = endpointDataSource.Endpoints;

      foreach (var endpoint in endpoints)
      {
        var permissionDescriptor = await ExtractPermissionFromEndpointAsync(endpoint, cancellationToken);
        if (permissionDescriptor != null)
        {
          permissions.Add(permissionDescriptor);
        }
      }
    }
    catch (Exception)
    {
      // Si no se puede acceder a los endpoints, retornar lista vacía
      // Esto permite que el sistema continue funcionando
    }

    return MergeRoutesSharingKey(permissions);
  }

  /// <summary>
  /// La tabla de rutas del sync: la que fijó el sync desde código para este scope, o la del contenedor.
  /// </summary>
  private EndpointDataSource? ResolveEndpointDataSource()
  {
    return _serviceProvider.GetService<SyncEndpointSource>()?.Resolve() ?? _serviceProvider.GetService<EndpointDataSource>();
  }

  /// <inheritdoc />
  public IList<string> FindEndpointsWithoutPermission()
  {
    var endpointDataSource = ResolveEndpointDataSource();
    if (endpointDataSource == null)
    {
      return [];
    }

    var result = new List<string>();
    foreach (var routeEndpoint in endpointDataSource.Endpoints.OfType<RouteEndpoint>())
    {
      var pattern = routeEndpoint.RoutePattern?.RawText;
      if (string.IsNullOrEmpty(pattern))
      {
        continue;
      }

      var authorizationInfo = ExtractAuthorizationMetadata(routeEndpoint);
      if (authorizationInfo.HasPermissionsNotRequired
          || authorizationInfo.RequiresDevelopmentAccess
          || authorizationInfo.RequiresSuAdminAccess)
      {
        continue;
      }

      var namespaceName = GetEndpointType(routeEndpoint)?.Namespace;
      if (namespaceName is not null && _permissionNamingService.BuildPermissionNaming(namespaceName).IsValid)
      {
        continue;
      }

      var methods = string.Join(",", ExtractHttpMethods(routeEndpoint));
      result.Add($"{methods} {pattern} ({namespaceName ?? "namespace desconocido"})");
    }

    return result;
  }

  /// <summary>
  /// Fusiona descriptores que comparten <see cref="PermissionDescriptor.Key"/> <b>y</b> namespace --
  /// ocurre cuando un mismo slice mapea varias rutas físicas desde un único método público (ej.
  /// <c>TwoFactor</c>, <c>PasswordManagement</c>, <c>GoogleAuth</c>). Estas rutas representan un único
  /// permiso de grano grueso a nivel de slice, no permisos duplicados.
  /// </summary>
  /// <remarks>
  /// Dos namespaces distintos con la misma Key (ej. un endpoint de la app consumidora en
  /// <c>MiApi.Features.Users.Commands.CreateUser</c> frente al <c>users.create_user</c> de la librería)
  /// NO se fusionan: quedan como descriptores separados para que
  /// <c>DiscoveryPermissionsService.ValidateDuplicateKeys</c> rechace el sync con un error claro.
  /// Fusionarlos haría que quien tenga el permiso de uno entre también al otro sin que nadie lo decida.
  /// </remarks>
  private static List<PermissionDescriptor> MergeRoutesSharingKey(IList<PermissionDescriptor> descriptors)
  {
    var merged = new List<PermissionDescriptor>();

    foreach (var group in descriptors.GroupBy(d => (d.Key, d.SourceNamespace)))
    {
      var first = group.First();
      if (group.Count() > 1)
      {
        first.Routes = group.SelectMany(d => d.Routes).Distinct().ToList();
        first.HttpMethods = group.SelectMany(d => d.HttpMethods).Distinct().ToList();
        first.Description ??= group.Select(d => d.Description).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d));
        first.HasPermissionsNotRequired = group.Any(d => d.HasPermissionsNotRequired);
        first.PermissionsNotRequiredReason ??= group.Select(d => d.PermissionsNotRequiredReason).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r));
        first.RequiresDevelopmentAccess = group.Any(d => d.RequiresDevelopmentAccess);
        first.RequiresDevelopmentAccessReason ??= group.Select(d => d.RequiresDevelopmentAccessReason).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r));
        first.RequiresSuAdminAccess = group.Any(d => d.RequiresSuAdminAccess);
        first.RequiresSuAdminAccessReason ??= group.Select(d => d.RequiresSuAdminAccessReason).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r));
        first.HasRequirePermissions = group.Any(d => d.HasRequirePermissions);
        first.RequirePermissionsNote ??= group.Select(d => d.RequirePermissionsNote).FirstOrDefault(r => !string.IsNullOrWhiteSpace(r));
      }

      merged.Add(first);
    }

    return merged;
  }

  /// <summary>
  /// Extrae información de permisos de un endpoint específico usando sus metadatos.
  /// </summary>
  /// <param name="endpoint">Endpoint a analizar.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>Descriptor de permiso extraído o null si no se puede determinar.</returns>
  private async Task<PermissionDescriptor?> ExtractPermissionFromEndpointAsync(
      Microsoft.AspNetCore.Http.Endpoint endpoint,
      CancellationToken cancellationToken)
  {
    await Task.Yield();

    // Obtener información de ruta del endpoint
    if (endpoint is not RouteEndpoint routeEndpoint)
    {
      return null;
    }

    var pattern = routeEndpoint.RoutePattern?.RawText;
    if (string.IsNullOrEmpty(pattern))
    {
      return null;
    }

    // Namespace real del endpoint (para derivar Feature/CqrsType/Action), resuelto desde el
    // MethodInfo del delegate mapeado -- misma técnica que AuthorizationMiddleware.GetEndpointType.
    var endpointType = GetEndpointType(routeEndpoint);
    if (endpointType?.Namespace is not { } namespaceName)
    {
      return null;
    }

    var namingResult = _permissionNamingService.BuildPermissionNaming(namespaceName);
    if (!namingResult.IsValid)
    {
      return null;
    }

    // Extraer metadatos de autorización del endpoint
    var authorizationInfo = ExtractAuthorizationMetadata(endpoint);

    // Determinar HTTP methods
    var httpMethods = ExtractHttpMethods(routeEndpoint);

    return BuildDescriptor(namingResult, endpointType, pattern, httpMethods, authorizationInfo, routeEndpoint);
  }

  /// <summary>
  /// Construye el <see cref="PermissionDescriptor"/> final a partir de las piezas ya resueltas.
  /// </summary>
  /// <summary>
  /// La descripción sale del <c>.WithDescription()</c> del endpoint, que no tiene límite: se recorta al
  /// largo de la columna para que una descripción larga (de la librería o de tu app) nunca haga fallar
  /// el sync al guardar.
  /// </summary>
  private static string? FitDescription(string? description)
    => description is { Length: > PermissionConfiguration.DescriptionMaxLength }
      ? string.Concat(description.AsSpan(0, PermissionConfiguration.DescriptionMaxLength - 1), "…")
      : description;

  private static PermissionDescriptor BuildDescriptor(
      PermissionNamingResultDto namingResult,
      Type endpointType,
      string pattern,
      IList<string> httpMethods,
      AuthorizationInfo authorizationInfo,
      RouteEndpoint routeEndpoint)
  {
    // Descripción real (.WithDescription()/.WithSummary(), metadatos nativos de ASP.NET Core) --
    // no una sintetizada genéricamente.
    var description = routeEndpoint.Metadata.GetMetadata<IEndpointDescriptionMetadata>()?.Description
      ?? routeEndpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>()?.Summary;

    return new PermissionDescriptor
    {
      Key = namingResult.Key,
      Feature = namingResult.Feature,
      CqrsType = namingResult.CqrsType,
      Action = namingResult.ActionSnakeCase,
      Description = FitDescription(description),
      SourceFilePath = string.Empty, // No disponible desde metadatos en runtime
      EndpointMethodName = endpointType.Name,
      SourceNamespace = endpointType.Namespace ?? string.Empty,
      HttpMethods = httpMethods,
      Routes = new[] { pattern },
      HasPermissionsNotRequired = authorizationInfo.HasPermissionsNotRequired,
      PermissionsNotRequiredReason = authorizationInfo.PermissionsNotRequiredReason,
      RequiresDevelopmentAccess = authorizationInfo.RequiresDevelopmentAccess,
      RequiresDevelopmentAccessReason = authorizationInfo.RequiresDevelopmentAccessReason,
      RequiresSuAdminAccess = authorizationInfo.RequiresSuAdminAccess,
      RequiresSuAdminAccessReason = authorizationInfo.RequiresSuAdminAccessReason,
      HasRequirePermissions = authorizationInfo.HasRequirePermissions,
      RequirePermissionsNote = authorizationInfo.RequirePermissionsNote,
    };
  }

  /// <summary>
  /// Obtiene el <see cref="Type"/> declarante del delegate mapeado a este endpoint, buscando el
  /// <see cref="MethodInfo"/> en sus metadatos -- misma técnica que
  /// <c>AuthorizationMiddleware.GetEndpointType</c>.
  /// </summary>
  private static Type? GetEndpointType(RouteEndpoint routeEndpoint)
  {
    foreach (var metadata in routeEndpoint.Metadata)
    {
      if (metadata is MethodInfo methodInfo)
      {
        return methodInfo.DeclaringType;
      }
    }

    return null;
  }

  /// <summary>
  /// Extrae información de autorización de los metadatos del endpoint.
  /// Usa la misma lógica que AuthorizationMiddleware.
  /// </summary>
  private static AuthorizationInfo ExtractAuthorizationMetadata(Microsoft.AspNetCore.Http.Endpoint endpoint)
  {
    var authorizationInfo = new AuthorizationInfo();

    // Buscar PermissionsNotRequiredAttribute en metadatos
    var permissionsNotRequired = endpoint.Metadata.GetMetadata<PermissionsNotRequiredAttribute>();
    if (permissionsNotRequired != null)
    {
      authorizationInfo.HasPermissionsNotRequired = true;
      authorizationInfo.PermissionsNotRequiredReason = permissionsNotRequired.Reason;
    }

    // Buscar RequiresDevelopmentAccessAttribute en metadatos
    var requiresDevelopmentAccess = endpoint.Metadata.GetMetadata<RequiresDevelopmentAccessAttribute>();
    if (requiresDevelopmentAccess != null)
    {
      authorizationInfo.RequiresDevelopmentAccess = true;
      authorizationInfo.RequiresDevelopmentAccessReason = requiresDevelopmentAccess.Reason;
    }

    // Buscar RequiresSuAdminAccessAttribute en metadatos
    var requiresSuAdminAccess = endpoint.Metadata.GetMetadata<RequiresSuAdminAccessAttribute>();
    if (requiresSuAdminAccess != null)
    {
      authorizationInfo.RequiresSuAdminAccess = true;
      authorizationInfo.RequiresSuAdminAccessReason = requiresSuAdminAccess.Reason;
    }

    // Buscar RequirePermissionsAttribute en metadatos
    var requirePermissions = endpoint.Metadata.GetMetadata<RequirePermissionsAttribute>();
    if (requirePermissions != null)
    {
      authorizationInfo.HasRequirePermissions = true;
      authorizationInfo.RequirePermissionsNote = requirePermissions.Note;
    }

    return authorizationInfo;
  }

  /// <summary>
  /// Extrae los HTTP methods del endpoint.
  /// </summary>
  private static IList<string> ExtractHttpMethods(RouteEndpoint routeEndpoint)
  {
    var httpMethods = new List<string>();

    // Buscar HttpMethodMetadata en los metadatos
    var httpMethodMetadata = routeEndpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>();
    if (httpMethodMetadata?.HttpMethods != null)
    {
      httpMethods.AddRange(httpMethodMetadata.HttpMethods);
    }

    return httpMethods.Count > 0 ? httpMethods : new[] { "GET" }; // Default to GET
  }
}