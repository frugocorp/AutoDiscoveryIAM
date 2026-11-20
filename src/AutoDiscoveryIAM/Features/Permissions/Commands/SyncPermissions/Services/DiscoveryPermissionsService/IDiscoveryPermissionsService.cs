// -----------------------------------------------------------------------
// <copyright file="IDiscoveryPermissionsService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services.DiscoveryPermissionsService;

/// <summary>
/// Interfaz para el servicio de descubrimiento de permisos.
/// </summary>
/// <remarks>
/// Define el contrato para descubrir automáticamente permisos a partir de endpoints existentes.
/// </remarks>
public interface IDiscoveryPermissionsService
{
    /// <summary>
    /// Descubre todos los permisos disponibles en el sistema.
    /// </summary>
    /// <param name="forceFullScan">Si es true, ignora cache y escanea todos los archivos.</param>
    /// <param name="enableDetailedConsoleOutput">Si es true, imprime cada permiso descubierto en consola (útil para debugging).</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Resultado de la operación con lista de descriptores de permisos descubiertos.</returns>
    Task<OperationResult<IList<PermissionDescriptor>>> DiscoverPermissionsAsync(
        bool forceFullScan = false,
        bool enableDetailedConsoleOutput = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida la consistencia de permisos descubiertos.
    /// </summary>
    /// <param name="descriptors">Lista de descriptores a validar.</param>
    /// <returns>Resultado de la operación con lista de errores de validación, vacía si no hay errores.</returns>
    OperationResult<IList<string>> ValidateConsistency(IList<PermissionDescriptor> descriptors);

    /// <summary>
    /// Endpoints que exigen un permiso RBAC pero no pudieron nombrarse (ver
    /// <see cref="IEndpointDiscoveryService.FindEndpointsWithoutPermission"/>), para reportarlos en la
    /// respuesta del sync en vez de descartarlos sin aviso.
    /// </summary>
    /// <returns>Una entrada legible por endpoint; vacía si todos tienen permiso o no lo necesitan.</returns>
    IList<string> FindEndpointsWithoutPermission();
}
