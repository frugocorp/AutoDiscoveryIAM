// -----------------------------------------------------------------------
// <copyright file="IEndpointDiscoveryService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;

/// <summary>
/// Servicio para descubrir endpoints y sus metadatos de autorización.
/// Alternativa al parsing de código fuente que accede a los metadatos registrados.
/// </summary>
public interface IEndpointDiscoveryService
{
    /// <summary>
    /// Descubre todos los endpoints registrados en la aplicación y extrae sus metadatos de autorización.
    /// </summary>
    /// <returns>Lista de descriptores de permisos extraídos de metadatos de endpoints.</returns>
    Task<IList<PermissionDescriptor>> DiscoverEndpointsFromMetadataAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Lista los endpoints que exigen un permiso RBAC pero cuyo namespace no permite derivar una
    /// clave (ej. falta el segmento <c>Features</c>): sin permiso registrado, responden 403 a todo
    /// usuario sin <c>IsAdmin</c>/<c>IsDevelopment</c>/<c>IsSuAdmin</c>. Excluye los marcados con
    /// <c>[PermissionsNotRequired]</c>, <c>[RequiresDevelopmentAccess]</c> o <c>[RequiresSuAdminAccess]</c>,
    /// que no dependen de un permiso.
    /// </summary>
    /// <returns>Una entrada legible por endpoint, ej. <c>GET /pedidos (MiApi.Pedidos.Endpoints)</c>.</returns>
    IList<string> FindEndpointsWithoutPermission();
}