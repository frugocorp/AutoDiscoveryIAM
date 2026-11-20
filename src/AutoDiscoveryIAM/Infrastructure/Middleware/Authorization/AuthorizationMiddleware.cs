// -----------------------------------------------------------------------
// <copyright file="AuthorizationMiddleware.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable CA1848 // For improved performance, use LoggerMessage delegates - TODO: Optimize logging in future iteration
#pragma warning disable CA1873 // Avoid potentially expensive logging argument evaluation - same deferred optimization as CA1848 above

using System.Reflection;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Services.UserQuery;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

/// <summary>
/// Middleware de autorización que valida permisos de usuarios automáticamente.
/// Implementa el principio "Secure by Default" donde todos los endpoints requieren
/// autorización a menos que estén marcados con [PermissionsNotRequired].
/// </summary>
/// <remarks>
/// Flujo de autorización:
/// 1. Verificar si endpoint tiene [PermissionsNotRequired] → continuar sin validación
/// 2. Verificar autenticación del usuario → 401 si no autenticado
/// 3. Verificar si usuario es Super User → continuar sin validación (bypass)
/// 4. Extraer permiso del namespace del endpoint
/// 5. Validar que usuario tiene el permiso activo → 403 si no autorizado
/// 6. Continuar con ejecución del endpoint
///
/// Beneficios:
/// - Autorización automática sin código duplicado
/// - Seguridad por defecto con opt-out explícito
/// - Bypass para super users
/// - Logging de eventos de autorización
/// - Performance optimizada con cache
/// </remarks>
public class AuthorizationMiddleware
{

  private readonly RequestDelegate _next;
  private readonly ILogger<AuthorizationMiddleware> _logger;
  private readonly IServiceProvider _serviceProvider;
  private readonly ISecurityEventLogger _securityEventLogger;
  private readonly IEndpointPermissionKeyResolver _keyResolver;

  /// <summary>
  /// Estructura que contiene la información de atributos de autorización de un endpoint.
  /// </summary>
  private readonly record struct EndpointAuthorizationInfo(
      bool HasPermissionsNotRequired,
      string? RequireNote,
      RequiresDevelopmentAccessAttribute? DevelopmentAccess,
      RequiresSuAdminAccessAttribute? SuAdminAccess);

  /// <summary>
  /// Cache de atributos por tipo de endpoint para optimizar performance.
  /// Key: Type.FullName, Value: EndpointAuthorizationInfo
  /// </summary>
  private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, EndpointAuthorizationInfo> AttributeCache = new(StringComparer.Ordinal);

  public AuthorizationMiddleware(
      RequestDelegate next,
      ILogger<AuthorizationMiddleware> logger,
      IServiceProvider serviceProvider,
      ISecurityEventLogger securityEventLogger,
      IEndpointPermissionKeyResolver keyResolver)
  {
    _next = next ?? throw new ArgumentNullException(nameof(next));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    _securityEventLogger = securityEventLogger ?? throw new ArgumentNullException(nameof(securityEventLogger));
    _keyResolver = keyResolver ?? throw new ArgumentNullException(nameof(keyResolver));
  }

  /// <summary>
  /// Invoca el middleware de autorización para validar permisos del usuario.
  /// </summary>
  /// <param name="context">Contexto HTTP actual.</param>
  public async Task InvokeAsync(HttpContext context)
  {
    try
    {
      // 1. Extraer información del endpoint actual
      var endpoint = context.GetEndpoint();
      if (endpoint == null)
      {
        _logger.LogDebug("No endpoint found, continuing without authorization check");
        await _next(context);
        return;
      }

      // 2. Verificar atributos de autorización del endpoint
      var authInfo = await CheckPermissionAttributesAsync(endpoint);

      if (authInfo.HasPermissionsNotRequired)
      {
        _logger.LogDebug("Endpoint marked with [PermissionsNotRequired], skipping authorization");
        await _next(context);
        return;
      }

      // 3. Verificar autenticación del usuario
      if (!context.User.Identity?.IsAuthenticated == true)
      {
        _logger.LogWarning("Unauthenticated request to protected endpoint: {Path}", context.Request.Path);
        _securityEventLogger.LogSecurityEvent(SecurityEventType.PermissionDenied, null, context.Connection.RemoteIpAddress?.ToString(), context.Request.Path, "Petición no autenticada a endpoint protegido.");
        throw new AuthenticationRequiredException(AuthorizationValidationTypes.Messages.AuthenticationRequired);
      }

      // 4-5. Obtener información del usuario autenticado y validar que esté activo
      using var scope = _serviceProvider.CreateScope();
      var (currentUser, userPublicId) = await ResolveAuthenticatedUserAsync(context, scope);

      var specialUserResult = await HandleSpecialUserAccessAsync(context, authInfo, currentUser, userPublicId);
      if (specialUserResult.HasValue)
      {
        if (specialUserResult.Value)
        {
          await _next(context);
        }
        return;
      }

      // 6. Clave de permiso del endpoint que el router ya eligió, con la misma regla que el sync
      // (todas las rutas de un slice comparten su permiso; un choque de claves niega el acceso).
      var permissionKey = _keyResolver.ResolveKey(endpoint);
      if (string.IsNullOrEmpty(permissionKey))
      {
        _logger.LogWarning("Could not extract permission key from endpoint {Path}", context.Request.Path);
        throw new InsufficientPermissionsException(AuthorizationValidationTypes.Messages.PermissionValidationFailed);
      }

      // 7. Validar que el usuario tiene el permiso activo
      var userQueryService = scope.ServiceProvider.GetRequiredService<IUserQueryService>();
      var hasPermission = await userQueryService.HasPermissionByPublicIdAsync(userPublicId, permissionKey);

      if (!hasPermission)
      {
        _logger.LogWarning("User {UserId} denied access to {Path} - missing permission: {Permission}",
            userPublicId, context.Request.Path, permissionKey);
        _securityEventLogger.LogSecurityEvent(SecurityEventType.PermissionDenied, userPublicId.ToString(), context.Connection.RemoteIpAddress?.ToString(), context.Request.Path, $"Permiso requerido: {permissionKey}");
        throw new InsufficientPermissionsException($"{AuthorizationValidationTypes.Messages.InsufficientPermissions}. Requerido: {permissionKey}", permissionKey, userPublicId);
      }

      // 8. Usuario autorizado - continuar con la ejecución
      _logger.LogDebug("User {UserId} authorized for {Path} with permission {Permission}",
          userPublicId, context.Request.Path, permissionKey);

      await _next(context);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error in authorization middleware for path {Path}", context.Request.Path);

      // Re-throw the exception so that ExceptionHandlingMiddleware can handle it properly
      // We only want to handle authorization-specific errors here
      throw;
    }
  }

  /// <summary>
  /// Resuelve el usuario autenticado (PublicId + entidad) y valida que exista y esté activo.
  /// </summary>
  private async Task<(Features.Users.Models.Entities.User CurrentUser, Guid UserPublicId)> ResolveAuthenticatedUserAsync(HttpContext context, IServiceScope scope)
  {
    var authContextService = scope.ServiceProvider.GetRequiredService<IAuthContextService>();
    var userPublicId = authContextService.GetCurrentUserPublicId();

    if (userPublicId == null)
    {
      _logger.LogWarning("Could not retrieve user PublicId for authenticated user");
      throw new AuthenticationRequiredException(AuthorizationValidationTypes.Messages.InvalidUserContext);
    }

    var currentUser = await authContextService.GetCurrentUserAsync();
    if (currentUser == null)
    {
      _logger.LogWarning("Could not retrieve current user entity for authenticated user {UserId}", userPublicId);
      throw new AuthenticationRequiredException(AuthorizationValidationTypes.Messages.InvalidUserContext);
    }

    if (!currentUser.IsActive)
    {
      _logger.LogWarning("User {UserId} denied access to {Path} - user is inactive", userPublicId, context.Request.Path);
      throw new InsufficientPermissionsException(AuthorizationValidationTypes.Messages.UserInactive);
    }

    return (currentUser, userPublicId.Value);
  }

  /// <summary>
  /// Verifica los atributos de permisos del endpoint usando cache para optimizar performance.
  /// Busca primero en los metadatos del endpoint (Minimal API) y luego en atributos de clase.
  /// </summary>
  private static async Task<EndpointAuthorizationInfo> CheckPermissionAttributesAsync(Microsoft.AspNetCore.Http.Endpoint endpoint)
  {
    await Task.Yield(); // Simular operación asíncrona

    // PRIMERO: Buscar en metadatos del endpoint (Minimal API .WithMetadata())
    var permissionsNotRequiredInMetadata = endpoint.Metadata.GetMetadata<PermissionsNotRequiredAttribute>();
    if (permissionsNotRequiredInMetadata != null)
    {
      return new EndpointAuthorizationInfo(
          HasPermissionsNotRequired: true,
          RequireNote: permissionsNotRequiredInMetadata.Reason,
          DevelopmentAccess: null,
          SuAdminAccess: null);
    }

    var requirePermissionsInMetadata = endpoint.Metadata.GetMetadata<RequirePermissionsAttribute>();
    var developmentAccessInMetadata = endpoint.Metadata.GetMetadata<RequiresDevelopmentAccessAttribute>();
    var suAdminAccessInMetadata = endpoint.Metadata.GetMetadata<RequiresSuAdminAccessAttribute>();

    if (requirePermissionsInMetadata != null || developmentAccessInMetadata != null || suAdminAccessInMetadata != null)
    {
      return new EndpointAuthorizationInfo(
          HasPermissionsNotRequired: false,
          RequireNote: requirePermissionsInMetadata?.Note,
          DevelopmentAccess: developmentAccessInMetadata,
          SuAdminAccess: suAdminAccessInMetadata);
    }

    // SEGUNDO: Intentar obtener el tipo del endpoint para atributos de clase (fallback)
    var endpointType = GetEndpointType(endpoint);
    if (endpointType == null)
    {
      return new EndpointAuthorizationInfo(false, null, null, null);
    }

    var typeKey = endpointType.FullName ?? string.Empty;

    // Verificar cache primero
    if (AttributeCache.TryGetValue(typeKey, out var cachedResult))
    {
      return cachedResult;
    }

    // Buscar atributos en el tipo
    var hasPermissionsNotRequired = endpointType.GetCustomAttribute<PermissionsNotRequiredAttribute>() != null;
    var requirePermissionsAttr = endpointType.GetCustomAttribute<RequirePermissionsAttribute>();
    var developmentAccessAttr = endpointType.GetCustomAttribute<RequiresDevelopmentAccessAttribute>();
    var suAdminAccessAttr = endpointType.GetCustomAttribute<RequiresSuAdminAccessAttribute>();

    // Crear resultado
    var result = new EndpointAuthorizationInfo(
        HasPermissionsNotRequired: hasPermissionsNotRequired,
        RequireNote: requirePermissionsAttr?.Note,
        DevelopmentAccess: developmentAccessAttr,
        SuAdminAccess: suAdminAccessAttr);

    // Guardar en cache
    AttributeCache[typeKey] = result;

    return result;
  }

  /// <summary>
  /// Obtiene el Type del endpoint desde el RouteEndpoint.
  /// </summary>
  private static Type? GetEndpointType(Microsoft.AspNetCore.Http.Endpoint endpoint)
  {
    // Para Minimal APIs, el tipo suele estar en los metadatos
    var routeEndpoint = endpoint as RouteEndpoint;
    if (routeEndpoint?.Metadata == null)
    {
      return null;
    }

    // Buscar en los metadatos información del tipo
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
  /// Maneja la validación de acceso para usuarios especiales y atributos granulares.
  /// </summary>
  /// <returns>null si debe continuar con validación normal, true si debe proceder, false si debe denegar acceso</returns>
  private Task<bool?> HandleSpecialUserAccessAsync(
      HttpContext context,
      EndpointAuthorizationInfo authInfo,
      Features.Users.Models.Entities.User? currentUser,
      Guid userPublicId)
  {
    // Verificar restricciones granulares específicas
    if (authInfo.DevelopmentAccess != null)
    {
      // Endpoint requiere acceso de Development - SOLO Development
      var hasDevAccess = currentUser?.IsDevelopment == true;

      if (!hasDevAccess)
      {
        _logger.LogWarning("User {UserId} denied access to {Path} - requires development access: {Reason}",
            userPublicId, context.Request.Path, authInfo.DevelopmentAccess.Reason);
        var message = string.IsNullOrEmpty(authInfo.DevelopmentAccess.Reason)
            ? AuthorizationValidationTypes.Messages.DevelopmentAccessRequired
            : $"{AuthorizationValidationTypes.Messages.DevelopmentAccessRequired}: {authInfo.DevelopmentAccess.Reason}";
        throw new DevelopmentAccessRequiredException(message, authInfo.DevelopmentAccess.Reason);
      }

      _logger.LogInformation("Development access granted for user {UserId} ({UserType}) to endpoint {Path}",
          userPublicId, GetUserType(currentUser), context.Request.Path);
      return Task.FromResult<bool?>(true);
    }

    if (authInfo.SuAdminAccess != null)
    {
      // Endpoint requiere acceso de SuAdmin - SOLO Development y SuAdmin
      var hasSuAdminAccess = currentUser?.IsDevelopment == true ||
                            currentUser?.IsSuAdmin == true;

      if (!hasSuAdminAccess)
      {
        _logger.LogWarning("User {UserId} denied access to {Path} - requires super admin access: {Reason}",
            userPublicId, context.Request.Path, authInfo.SuAdminAccess.Reason);
        var message = string.IsNullOrEmpty(authInfo.SuAdminAccess.Reason)
            ? AuthorizationValidationTypes.Messages.SuperAdminAccessRequired
            : $"{AuthorizationValidationTypes.Messages.SuperAdminAccessRequired}: {authInfo.SuAdminAccess.Reason}";
        throw new SuperAdminAccessRequiredException(message, authInfo.SuAdminAccess.Reason);
      }

      _logger.LogInformation("Super admin access granted for user {UserId} ({UserType}) to endpoint {Path}",
          userPublicId, GetUserType(currentUser), context.Request.Path);
      return Task.FromResult<bool?>(true);
    }

    // Bypass general para usuarios especiales (solo si no hay restricciones granulares)
    var isSpecialUser = currentUser?.IsAdmin == true ||
                       currentUser?.IsDevelopment == true ||
                       currentUser?.IsSuAdmin == true;

    if (isSpecialUser)
    {
      _logger.LogInformation("Special user bypass for user {UserId} ({UserType}) to endpoint {Path}",
          userPublicId, GetUserType(currentUser), context.Request.Path);
      return Task.FromResult<bool?>(true);
    }

    // Continuar con validación normal
    return Task.FromResult<bool?>(null);
  }

  /// <summary>
  /// Obtiene el tipo de usuario especial para logging.
  /// </summary>
  private static string GetUserType(Features.Users.Models.Entities.User? user)
  {
    if (user == null) return "Unknown";

    if (user.IsDevelopment) return "Development";
    if (user.IsSuAdmin) return "SuAdmin";
    if (user.IsAdmin) return "Admin";

    return "Regular";
  }
}
