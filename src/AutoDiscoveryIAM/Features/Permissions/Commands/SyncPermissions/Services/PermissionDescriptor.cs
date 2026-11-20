// -----------------------------------------------------------------------
// <copyright file="PermissionDescriptor.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Services;

/// <summary>
/// Descriptor de un permiso descubierto.
/// </summary>
/// <remarks>
/// Contiene toda la información técnica extraída de un endpoint para crear o actualizar permisos.
/// </remarks>
public class PermissionDescriptor
{
    /// <summary>
    /// Clave única del permiso (ej: "users.create_user").
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Feature al que pertenece el permiso (ej: "users").
    /// </summary>
    public string Feature { get; set; } = string.Empty;

    /// <summary>
    /// Acción del permiso (ej: "create_user").
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Descripción opcional del permiso.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Tipo CQRS del endpoint ("Command" o "Query").
    /// </summary>
    public string CqrsType { get; set; } = string.Empty;

    /// <summary>
    /// Ruta completa del archivo fuente del endpoint.
    /// </summary>
    public string SourceFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del método endpoint (ej: "MapCreateUserEndpoint").
    /// </summary>
    public string EndpointMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Patrones HTTP asignados al endpoint (ej: GET, POST, etc.).
    /// </summary>
    public IList<string> HttpMethods { get; set; } = new List<string>();

    /// <summary>
    /// Rutas HTTP asignadas al endpoint.
    /// </summary>
    public IList<string> Routes { get; set; } = new List<string>();

    /// <summary>
    /// Indica si el permiso fue marcado como obsoleto por falta de uso.
    /// </summary>
    public bool IsObsolete { get; set; }

    /// <summary>
    /// Timestamp de cuando fue descubierto este permiso.
    /// </summary>
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Hash único basado en propiedades clave para detectar cambios.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Lista de errores encontrados durante el análisis de este endpoint.
    /// </summary>
    public IList<string> ValidationErrors { get; set; } = new List<string>();

    /// <summary>
    /// Lista de advertencias encontradas durante el análisis de este endpoint.
    /// </summary>
    public IList<string> ValidationWarnings { get; set; } = new List<string>();

    // 🆕 NUEVO: Metadatos de autorización capturados

    /// <summary>
    /// Indica si el endpoint está marcado con [PermissionsNotRequired].
    /// </summary>
    public bool HasPermissionsNotRequired { get; set; } = false;

    /// <summary>
    /// Razón por la que el endpoint no requiere permisos (desde [PermissionsNotRequired("razón")]).
    /// </summary>
    public string? PermissionsNotRequiredReason { get; set; }

    /// <summary>
    /// Indica si el endpoint requiere acceso de desarrollo.
    /// </summary>
    public bool RequiresDevelopmentAccess { get; set; } = false;

    /// <summary>
    /// Razón por la que el endpoint requiere acceso de desarrollo.
    /// </summary>
    public string? RequiresDevelopmentAccessReason { get; set; }

    /// <summary>
    /// Indica si el endpoint requiere acceso de super admin.
    /// </summary>
    public bool RequiresSuAdminAccess { get; set; } = false;

    /// <summary>
    /// Razón por la que el endpoint requiere acceso de super admin.
    /// </summary>
    public string? RequiresSuAdminAccessReason { get; set; }

    /// <summary>
    /// Indica si el endpoint tiene explícitamente [RequirePermissions].
    /// </summary>
    public bool HasRequirePermissions { get; set; } = false;

    /// <summary>
    /// Nota del atributo RequirePermissions.
    /// </summary>
    public string? RequirePermissionsNote { get; set; }

    /// <summary>
    /// Namespace real del slice que declaró el endpoint (ej.
    /// <c>MiApi.Features.Pedidos.Commands.CrearPedido</c>). Solo se usa durante el descubrimiento,
    /// no se persiste: distingue varias rutas del mismo slice (se fusionan en un permiso) de dos
    /// slices distintos que producen la misma <see cref="Key"/> (choque, se reporta como error).
    /// </summary>
    public string SourceNamespace { get; set; } = string.Empty;
}
