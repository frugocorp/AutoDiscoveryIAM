// -----------------------------------------------------------------------
// <copyright file="SyncEndpointSource.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;

/// <summary>
/// Tabla de rutas que lee el sync dentro de un scope. Por defecto es el <see cref="EndpointDataSource"/>
/// del contenedor, que ASP.NET Core llena con las rutas de la app recién al armar el pipeline
/// (<c>app.Run()</c>). El sync desde código corre antes de arrancar, así que fija aquí las rutas de
/// la propia app (<c>IEndpointRouteBuilder.DataSources</c>).
/// </summary>
internal sealed class SyncEndpointSource(IServiceProvider serviceProvider)
{
  private readonly IServiceProvider _serviceProvider = serviceProvider;

  /// <summary>
  /// Rutas fijadas para este scope; <c>null</c>: las del contenedor.
  /// </summary>
  internal EndpointDataSource? Override { get; set; }

  /// <summary>
  /// Las rutas que debe leer el sync.
  /// </summary>
  internal EndpointDataSource? Resolve() => Override ?? _serviceProvider.GetService<EndpointDataSource>();
}
