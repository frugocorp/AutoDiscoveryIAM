// -----------------------------------------------------------------------
// <copyright file="RolesDependencyInjection.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.Assignments;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Services.RoleQuery;
using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles;

/// <summary>
/// Registra las dependencias de la Feature Roles en el contenedor de DI.
/// </summary>
public static class RolesDI
{
  /// <summary>
  /// Agrega los servicios de la feature Roles al contenedor de DI.
  /// </summary>
  /// <param name="services">Colección de servicios para registrar dependencias.</param>
  /// <returns>La misma colección de servicios con las dependencias registradas.</returns>
  public static IServiceCollection AddRolesDI(this IServiceCollection services)
  {
    services.AddTransient<IRoleLogger, RoleLogger>();
    services.AddCreateRoleDI();
    services.AddGetRolesDI();
    services.AddGetRoleByPublicIdDI();
    services.AddUpdateRoleByPublicIdDI();
    services.AddDeleteRoleByPublicIdDI();
    services.AddRestoreRoleByPublicId();

    services.AddCreateRolePermissionsDI();
    services.AddDeleteRolePermissionsByPermissionPublicIdDI();
    services.AddRestoreRolePermissionsByPermissionPublicIdDI();
    services.AddUpdateBulkRolePermissionsDI();
    services.AddClearBulkRoleHiddenRelationsDI();

    services.AddGetRolePermissionsByPublicIdDI();
    services.AddGetUsersByPublicId();

    services.AddScoped<IRoleUserAssignmentRepository, RoleUserAssignmentRepository>();
    services.AddScoped<IRoleUserAssignmentService, RoleUserAssignmentService>();

    // Servicios de consulta de roles
    services.AddScoped<IRoleQueryService, RoleQueryService>();

    return services;
  }
}
