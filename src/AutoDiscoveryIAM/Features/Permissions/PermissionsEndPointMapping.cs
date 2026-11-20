// -----------------------------------------------------------------------
// <copyright file="PermissionsEndPointMapping.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetUsersByPublicId;
using Microsoft.AspNetCore.Routing;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions;

/// <summary>
/// Registro de endpoints de la Feature Permissions en el pipeline de Minimal API.
/// </summary>
public static class PermissionsEndPointMapping
{
  /// <summary>
  /// Mapea todos los endpoints de la feature Permissions.
  /// </summary>
  /// <param name="app">Builder de rutas para registrar endpoints.</param>
  public static void MapPermissionsEndpoints(this IEndpointRouteBuilder app)
  {
    SyncPermissionsEndPoint.MapSyncPermissionsEndPoint(app);
    GetPermissionsEndPoint.MapGetPermissionsEndpoint(app);
    GetRolesByPublicIdEndPoint.MapGetRolesByPublicIdEndPoint(app);
    GetUsersByPublicIdEndPoint.MapGetUsersByPublicIdEndPoint(app);
  }
}
