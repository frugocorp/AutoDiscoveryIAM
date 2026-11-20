// -----------------------------------------------------------------------
// <copyright file="RequiresSuAdminAccessAttribute.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

/// <summary>
/// Atributo que marca un endpoint como accesible únicamente para usuarios con IsSuAdmin=true.
/// Este atributo proporciona el nivel más alto de restricción para operaciones críticas del sistema.
/// </summary>
/// <remarks>
/// Casos de uso típicos:
/// - Operaciones críticas de administración del sistema
/// - Configuración de seguridad y políticas globales
/// - Gestión de usuarios administrativos
/// - Operaciones de backup/restore críticas
/// - Configuración de infraestructura y deployment
/// - Acceso a logs y auditoría completa del sistema
///
/// Comportamiento del Authorization Middleware:
/// - Solo usuarios con IsSuAdmin=true o IsDevelopment=true pueden acceder
/// - Usuarios con IsAdmin=true NO tienen acceso automático
/// - Todos los demás usuarios reciben 403 Forbidden
///
/// IMPORTANTE: Este es el nivel de acceso más restrictivo del sistema.
/// Solo debe usarse para operaciones que requieren máximos privilegios
/// y que pueden afectar la seguridad o estabilidad global del sistema.
///
/// Principio: "Super Admin Only" - operaciones de máximo nivel que requieren
/// el usuario con mayor nivel de privilegios en el sistema.
/// </remarks>
/// <example>
/// <code>
/// [RequiresSuAdminAccess("Configuración crítica de seguridad del sistema")]
/// public async Task&lt;IResult&gt; UpdateGlobalSecurityPolicies()
/// {
///     // Solo accesible por super admin
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequiresSuAdminAccessAttribute : Attribute
{
    /// <summary>
    /// Razón por la que este endpoint requiere acceso de super admin.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Inicializa una nueva instancia de RequiresSuAdminAccessAttribute.
    /// </summary>
    /// <param name="reason">Razón por la que este endpoint requiere acceso de super admin.</param>
    public RequiresSuAdminAccessAttribute(string reason)
    {
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    /// <summary>
    /// Inicializa una nueva instancia de RequiresSuAdminAccessAttribute sin razón específica.
    /// </summary>
    public RequiresSuAdminAccessAttribute()
    {
        Reason = "Endpoint requiere acceso de super admin";
    }
}
