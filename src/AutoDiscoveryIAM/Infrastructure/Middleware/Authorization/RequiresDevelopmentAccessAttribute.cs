// -----------------------------------------------------------------------
// <copyright file="RequiresDevelopmentAccessAttribute.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

/// <summary>
/// Atributo que marca un endpoint como accesible únicamente para usuarios con IsDevelopment=true.
/// Este atributo proporciona control granular para endpoints específicos de desarrollo.
/// </summary>
/// <remarks>
/// Casos de uso típicos:
/// - APIs de debugging y diagnóstico avanzado
/// - Endpoints de configuración de desarrollo
/// - Herramientas de testing y mocking
/// - Features experimentales en desarrollo
/// - Endpoints de seeding de datos de prueba
///
/// Comportamiento del Authorization Middleware:
/// - Solo usuarios con IsDevelopment=true pueden acceder
/// - Usuarios con IsAdmin=true NO tienen acceso automático
/// - Usuarios con IsSuAdmin=true NO tienen acceso automático
/// - Todos los demás usuarios reciben 403 Forbidden
///
/// IMPORTANTE: Estos endpoints NO deben estar disponibles en producción
/// a menos que sea específicamente requerido para operaciones de soporte técnico.
///
/// Principio: "Development-Only Access" - funcionalidades exclusivas para
/// usuarios de desarrollo identificados en el sistema.
/// </remarks>
/// <example>
/// <code>
/// [RequiresDevelopmentAccess("Endpoint para debugging de caché")]
/// public async Task&lt;IResult&gt; ClearAllCache()
/// {
///     // Solo accesible por usuarios development
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequiresDevelopmentAccessAttribute : Attribute
{
    /// <summary>
    /// Razón por la que este endpoint requiere acceso de desarrollo.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Inicializa una nueva instancia de RequiresDevelopmentAccessAttribute.
    /// </summary>
    /// <param name="reason">Razón por la que este endpoint requiere acceso de desarrollo.</param>
    public RequiresDevelopmentAccessAttribute(string reason)
    {
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    /// <summary>
    /// Inicializa una nueva instancia de RequiresDevelopmentAccessAttribute sin razón específica.
    /// </summary>
    public RequiresDevelopmentAccessAttribute()
    {
        Reason = "Endpoint requiere acceso de desarrollo";
    }
}
