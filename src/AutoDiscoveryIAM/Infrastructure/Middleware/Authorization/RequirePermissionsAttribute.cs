// -----------------------------------------------------------------------
// <copyright file="RequirePermissionsAttribute.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

/// <summary>
/// Atributo que marca explícitamente que un endpoint requiere validación de permisos.
/// Este atributo es opcional ya que la autorización es por defecto, pero puede usarse
/// para documentar explícitamente la intención o para configuración futura.
/// </summary>
/// <remarks>
/// Comportamiento del Authorization Middleware:
/// - SIN atributo [PermissionsNotRequired]: Se validan permisos (comportamiento por defecto)
/// - CON [RequirePermissions]: Se validan permisos (explícito, mismo comportamiento que por defecto)
/// - CON [PermissionsNotRequired]: NO se validan permisos (opt-out)
///
/// El principio es "Secure by Default" - todos los endpoints requieren autorización
/// a menos que específicamente se marque lo contrario.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequirePermissionsAttribute : Attribute
{
    /// <summary>
    /// Inicializa una nueva instancia de RequirePermissionsAttribute.
    /// </summary>
    public RequirePermissionsAttribute()
    {
    }

    /// <summary>
    /// Inicializa una nueva instancia de RequirePermissionsAttribute con una nota personalizada.
    /// </summary>
    /// <param name="note">Nota explicativa sobre por qué se requieren permisos específicos para este endpoint.</param>
    public RequirePermissionsAttribute(string note)
    {
        Note = note;
    }

    /// <summary>
    /// Nota opcional que documenta por qué este endpoint requiere validación de permisos.
    /// Útil para documentar casos especiales o requerimientos de seguridad específicos.
    /// </summary>
    public string? Note { get; }
}
