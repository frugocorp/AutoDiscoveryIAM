// -----------------------------------------------------------------------
// <copyright file="PermissionsDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Repositories;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetUsersByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Repositories;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Services.PermissionNaming;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions;

/// <summary>
/// Configuración de inyección de dependencias para la feature Permissions.
/// </summary>
public static class PermissionsDI
{
  /// <summary>
  /// Registra todos los servicios de la feature Permissions en el contenedor DI.
  /// </summary>
  /// <param name="services">Colección de servicios para registrar dependencias.</param>
  /// <returns>La colección de servicios para encadenamiento.</returns>
  public static IServiceCollection AddPermissionsDI(this IServiceCollection services)
  {
    // Registrar logger especializado para permisos
    services.AddScoped<IPermissionLogger, PermissionLogger>();

    // Registrar servicio de nomenclatura de permisos (para autorización y discovery)
    services.AddScoped<IPermissionNamingService, PermissionNamingService>();

    // Registrar repositorios con herencia
    services.AddScoped<IPermissionsRepository, PermissionsRepository>();
    services.AddScoped<ISyncPermissionsRepository, SyncPermissionsRepository>();

    // Registrar comandos de permisos
    services.AddSyncPermissionsDI();

    // Registrar queries de permisos
    services.AddGetPermissionsDI();
    services.AddGetRolesByPublicId();
    services.AddGetUsersByPublicId();

    return services;
  }
}
