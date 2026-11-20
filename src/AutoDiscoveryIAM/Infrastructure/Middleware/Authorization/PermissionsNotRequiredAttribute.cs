// -----------------------------------------------------------------------
// <copyright file="PermissionsNotRequiredAttribute.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

/// <summary>
/// Atributo que marca un endpoint como exento de validación de permisos.
/// Este es el único atributo que permite el opt-out del sistema de autorización por defecto.
/// </summary>
/// <remarks>
/// Casos de uso típicos:
/// - Endpoints públicos (login, register, health checks)
/// - APIs de diagnóstico y monitoreo
/// - Endpoints de documentación (Swagger)
/// - Recursos estáticos públicos
///
/// IMPORTANTE: Usar con extremo cuidado. Cada uso debe estar justificado
/// desde perspectiva de seguridad y documentado apropiadamente.
///
/// Principio: "Secure by Default" - solo endpoints específicamente marcados
/// con este atributo evitan la validación de permisos.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class PermissionsNotRequiredAttribute : Attribute
{
    /// <summary>
    /// Inicializa una nueva instancia de PermissionsNotRequiredAttribute.
    /// </summary>
    public PermissionsNotRequiredAttribute()
    {
    }

    /// <summary>
    /// Inicializa una nueva instancia de PermissionsNotRequiredAttribute con una justificación.
    /// </summary>
    /// <param name="reason">
    /// Razón por la cual este endpoint no requiere permisos.
    /// Es altamente recomendado proporcionar una justificación clara.
    /// </param>
    public PermissionsNotRequiredAttribute(string reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// Justificación de por qué este endpoint está exento de validación de permisos.
    /// Útil para auditorías de seguridad y revisiones de código.
    /// </summary>
    public string? Reason { get; }
}
