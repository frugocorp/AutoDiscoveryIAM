// -----------------------------------------------------------------------
// <copyright file="RolesEndPointMapping.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles;

/// <summary>
/// Registro del endpoint de la Feature Roles en el pipeline de Minimal API.
/// </summary>
public static class RolesEndPointMapping
{
  public static void MapRolesEndpoints(this IEndpointRouteBuilder app)
  {
    CreateRoleEndPoint.MapCreateRoleEndpoint(app);
    GetRolesEndPoint.MapGetRolesEndpoint(app);
    GetRoleByPublicIdEndPoint.MapGetRoleByPublicIdEndpoint(app);
    UpdateRoleByPublicIdEndpoint.MapUpdateRoleByPublicIdEndpoint(app);
    DeleteRoleByPublicIdEndPoint.MapDeleteRoleByPublicIdEndpoint(app);
    RestoreRoleByPublicIdEndPoint.MapRestoreRoleByPublicIdEndpoint(app);

    CreateRolePermissionsEndPoint.MapCreateRolePermissionsEndPoint(app);
    DeleteRolePermissionsByPermissionPublicIdEndPoint.MapDeleteRolePermissionsByPermissionPublicIdEndPoint(app);
    UpdateBulkRolePermissionsEndPoint.MapUpdateBulkRolePermissionsEndPoint(app);
    ClearBulkRoleHiddenRelationsEndPoint.MapClearBulkRoleHiddenRelationsEndPoint(app);

    GetPermissionsByPublicIdEndPoint.MapGetPermissionsByPublicIdEndPoint(app);
    GetUsersByPublicIdEndPoint.MapGetUsersByPublicIdEndPoint(app);
  }
}
