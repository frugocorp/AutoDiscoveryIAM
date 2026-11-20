// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.AnalyzeUpdatedPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.SyncPermissionsService;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;

/// <summary>
/// Configuración de inyección de dependencias para SyncPermissions.
/// </summary>
/// <remarks>
/// Registra todos los servicios necesarios para el comando de sincronización de permisos.
/// </remarks>
public static class SyncPermissionsDI
{
  /// <summary>
  /// Registra los servicios de sincronización de permisos en el contenedor DI.
  /// </summary>
  /// <param name="services">Colección de servicios para registrar dependencias.</param>
  /// <returns>La colección de servicios para encadenamiento.</returns>
  public static IServiceCollection AddSyncPermissionsDI(this IServiceCollection services)
  {
    // Registrar servicios internos del comando como Scoped
    services.AddScoped<IEndpointDiscoveryService, EndpointDiscoveryService>();
    services.AddScoped<IDiscoveryPermissionsService, DiscoveryPermissionsService>();
    services.AddScoped<ISyncPermissionsService, SyncPermissionsService>();

    // Registrar servicio orquestador
    services.AddScoped<IAnalyzeUpdatedPermissionsService, AnalyzeUpdatedPermissionsService>();

    // Registrar handler del comando
    services.AddScoped<SyncPermissionsHandler>();

    // Registrar validador
    services.AddScoped<SyncPermissionsCommandValidator>();

    return services;
  }
}
