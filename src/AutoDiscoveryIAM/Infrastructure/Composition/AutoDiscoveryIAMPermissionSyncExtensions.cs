// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMPermissionSyncExtensions.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;
using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Composition;

/// <summary>
/// Sincronización de permisos desde código, sin login HTTP: la misma operación que
/// <c>POST /permissions/sync</c>, para un pipeline de despliegue o una bandera de arranque.
/// </summary>
public static class AutoDiscoveryIAMPermissionSyncExtensions
{
  /// <summary>
  /// Nombre del endpoint de sync: si no está en la tabla de rutas, los endpoints aún no se mapearon.
  /// </summary>
  private const string SyncEndpointName = "SyncPermissions";

  /// <summary>
  /// Sincroniza los permisos con los endpoints mapeados de la app (los de la librería y los tuyos).
  /// Llámalo después de <c>app.MapAutoDiscoveryIAMEndpoints()</c> y de mapear tus propios endpoints:
  /// el sync lee la tabla de rutas, y lo que todavía no esté mapeado no existe para él.
  /// </summary>
  /// <param name="app">La app ya construida, con sus endpoints mapeados.</param>
  /// <param name="request">Opciones del sync (<c>DryRun</c>, <c>ForceFullSync</c>...). <c>null</c>: un sync normal que persiste.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  /// <returns>El mismo resultado que devuelve el endpoint (permisos nuevos, obsoletos, endpoints sin permiso...).</returns>
  /// <exception cref="InvalidOperationException">
  /// Si los endpoints de la librería no están mapeados, o si el sync falla (por ejemplo, una clave
  /// duplicada entre dos namespaces): un pipeline de despliegue debe detenerse en ese caso.
  /// </exception>
  public static async Task<SyncPermissionsCommandResponseDto> SyncAutoDiscoveryIAMPermissionsAsync(
    this WebApplication app,
    SyncPermissionsRequestDto? request = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(app);

    // Las rutas de la propia app, no las del contenedor: ASP.NET Core conecta las rutas mapeadas al
    // EndpointDataSource del contenedor recién al armar el pipeline (app.Run()), y este sync corre antes.
    using var routes = new CompositeEndpointDataSource(((IEndpointRouteBuilder)app).DataSources);
    return await SyncPermissionsAsync(app.Services, routes, request, cancellationToken);
  }

  /// <summary>
  /// Núcleo de <see cref="SyncAutoDiscoveryIAMPermissionsAsync"/> sobre el contenedor de la app.
  /// </summary>
  internal static async Task<SyncPermissionsCommandResponseDto> SyncPermissionsAsync(
    IServiceProvider services,
    EndpointDataSource routes,
    SyncPermissionsRequestDto? request,
    CancellationToken cancellationToken)
  {
    var endpointsMapped = routes.Endpoints.Any(e =>
      string.Equals(e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName, SyncEndpointName, StringComparison.Ordinal));
    if (!endpointsMapped)
    {
      throw new InvalidOperationException(
        "SyncAutoDiscoveryIAMPermissionsAsync se llamó antes de mapear los endpoints: llámalo después de app.MapAutoDiscoveryIAMEndpoints() y de mapear los endpoints de tu app.");
    }

    var command = new SyncPermissionsCommand(request ?? new SyncPermissionsRequestDto());
    using var scope = services.CreateScope();
    scope.ServiceProvider.GetRequiredService<SyncEndpointSource>().Override = routes;
    var validation = await scope.ServiceProvider.GetRequiredService<SyncPermissionsCommandValidator>().ValidateAsync(command, cancellationToken);
    if (!validation.IsValid)
    {
      throw new InvalidOperationException($"Opciones de sincronización inválidas: {string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))}");
    }

    var result = await scope.ServiceProvider.GetRequiredService<SyncPermissionsHandler>().Handle(command, cancellationToken);
    return result.Success
      ? result.Data!
      : throw new InvalidOperationException($"La sincronización de permisos falló: {result.Error}");
  }
}
