// -----------------------------------------------------------------------
// <copyright file="IEndpointPermissionKeyResolver.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

/// <summary>
/// Resuelve la clave de permiso del endpoint que el router ya eligió para la petición, con la misma
/// regla que usa el sync (namespace de la clase que declara el handler). Usado por
/// <see cref="AuthorizationMiddleware"/>.
/// </summary>
/// <remarks>
/// Reemplaza a la antigua caché <c>(HttpMethod, Route) → Key</c>, que solo guardaba la primera ruta de
/// cada permiso: en un slice con varias rutas (ej. <c>approve</c>/<c>reject</c> dentro de
/// <c>Activation</c>), las demás respondían 403 a todo usuario sin flags especiales. Partir del endpoint
/// resuelto también elimina la ambigüedad entre plantillas que se solapan (<c>/users/me</c> frente a
/// <c>/users/{publicId}</c>): esa decisión ya la tomó el router.
/// </remarks>
public interface IEndpointPermissionKeyResolver
{
  /// <summary>
  /// Devuelve la clave de permiso del endpoint, o <c>null</c> si su namespace no permite derivarla o si
  /// la misma clave la producen dos namespaces distintos (choque: se niega el acceso a ambos en vez de
  /// dejar que compartan permiso; el sync lo reporta como error).
  /// </summary>
  string? ResolveKey(Endpoint endpoint);
}
