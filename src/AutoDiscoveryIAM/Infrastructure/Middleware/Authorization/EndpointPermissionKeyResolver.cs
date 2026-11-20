// -----------------------------------------------------------------------
// <copyright file="EndpointPermissionKeyResolver.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Services.PermissionNaming;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

/// <inheritdoc cref="IEndpointPermissionKeyResolver" />
/// <remarks>
/// La tabla <c>namespace → clave</c> se arma una sola vez, en la primera petición protegida (cuando
/// todos los endpoints ya están mapeados), leyendo <see cref="EndpointDataSource"/>. Las claves que
/// producen dos namespaces distintos quedan en <c>null</c>.
/// </remarks>
public sealed class EndpointPermissionKeyResolver : IEndpointPermissionKeyResolver
{
  private static readonly PermissionNamingService NamingService = new();

  private readonly Lazy<Dictionary<string, string?>> _keysByNamespace;

  /// <summary>
  /// Initializes a new instance of the <see cref="EndpointPermissionKeyResolver"/> class.
  /// </summary>
  /// <param name="serviceProvider">Proveedor raíz, para leer <see cref="EndpointDataSource"/> de forma diferida.</param>
  public EndpointPermissionKeyResolver(IServiceProvider serviceProvider)
  {
    ArgumentNullException.ThrowIfNull(serviceProvider);
    _keysByNamespace = new Lazy<Dictionary<string, string?>>(() => BuildKeysByNamespace(serviceProvider));
  }

  /// <inheritdoc />
  public string? ResolveKey(Endpoint endpoint)
  {
    ArgumentNullException.ThrowIfNull(endpoint);

    var namespaceName = GetHandlerNamespace(endpoint);
    if (namespaceName is null)
    {
      return null;
    }

    if (_keysByNamespace.Value.TryGetValue(namespaceName, out var key))
    {
      return key;
    }

    // Endpoint que no estaba en la tabla (ej. agregado dinámicamente después): se nombra igual, y
    // si su clave ya la produce otro namespace es un choque, que niega el acceso como en la tabla.
    var naming = NamingService.BuildPermissionNaming(namespaceName);
    if (!naming.IsValid || _keysByNamespace.Value.ContainsValue(naming.Key))
    {
      return null;
    }

    return naming.Key;
  }

  private static Dictionary<string, string?> BuildKeysByNamespace(IServiceProvider serviceProvider)
  {
    var endpointDataSource = serviceProvider.GetService<EndpointDataSource>();
    if (endpointDataSource is null)
    {
      return new Dictionary<string, string?>(StringComparer.Ordinal);
    }

    var keysByNamespace = new Dictionary<string, string?>(StringComparer.Ordinal);
    foreach (var namespaceName in endpointDataSource.Endpoints.Select(GetHandlerNamespace).OfType<string>().Distinct(StringComparer.Ordinal))
    {
      var naming = NamingService.BuildPermissionNaming(namespaceName);
      keysByNamespace[namespaceName] = naming.IsValid ? naming.Key : null;
    }

    var collidingKeys = keysByNamespace.Values
      .OfType<string>()
      .GroupBy(key => key, StringComparer.Ordinal)
      .Where(group => group.Count() > 1)
      .Select(group => group.Key)
      .ToHashSet(StringComparer.Ordinal);

    foreach (var namespaceName in keysByNamespace.Keys.ToList())
    {
      if (keysByNamespace[namespaceName] is { } key && collidingKeys.Contains(key))
      {
        keysByNamespace[namespaceName] = null;
      }
    }

    return keysByNamespace;
  }

  private static string? GetHandlerNamespace(Endpoint endpoint)
  {
    return endpoint.Metadata.OfType<MethodInfo>().FirstOrDefault()?.DeclaringType?.Namespace;
  }
}
