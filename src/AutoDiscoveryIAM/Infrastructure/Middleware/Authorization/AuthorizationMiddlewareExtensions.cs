// -----------------------------------------------------------------------
// <copyright file="AuthorizationMiddlewareExtensions.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

/// <summary>
/// Extensions para registrar el AuthorizationMiddleware en el pipeline de middleware.
/// Proporciona métodos fluidos para facilitar la configuración.
/// </summary>
public static class AuthorizationMiddlewareExtensions
{
    /// <summary>
    /// Registra el AuthorizationMiddleware en el pipeline de middleware.
    /// Debe llamarse después de UseAuthentication() y antes de UseAuthorization().
    /// </summary>
    /// <param name="app">El IApplicationBuilder para configurar el pipeline.</param>
    /// <returns>El IApplicationBuilder para encadenamiento fluido.</returns>
    /// <remarks>
    /// El orden correcto del pipeline debe ser:
    /// 1. UseAuthentication() - verifica si el usuario está autenticado
    /// 2. UsePermissionsAuthorization() - verifica permisos específicos (este middleware)
    /// 3. UseAuthorization() - autorización estándar de ASP.NET Core
    ///
    /// Este middleware implementa "Secure by Default" donde todos los endpoints
    /// requieren validación de permisos a menos que estén marcados con [PermissionsNotRequired].
    /// </remarks>
    public static IApplicationBuilder UsePermissionsAuthorization(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuthorizationMiddleware>();
    }
}
