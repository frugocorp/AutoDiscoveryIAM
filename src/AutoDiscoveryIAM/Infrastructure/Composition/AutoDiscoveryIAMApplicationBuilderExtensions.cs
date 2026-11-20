// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMApplicationBuilderExtensions.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Exceptions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Transactions;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Composition;

/// <summary>
/// Pipeline de middleware del motor RBAC de AutoDiscoveryIAM: enrutamiento, manejo de
/// excepciones, CORS, rate limiting, autenticación JWT, autorización por permisos, y el middleware
/// transaccional de <c>AutoDiscoveryIAMUnitOfWork</c> -- en el orden exacto que ya funcionaba en <c>Program.cs</c>.
/// Sube un nivel el mismo patrón que ya existe por feature (<c>MapAllFeatureEndpoints()</c>).
/// </summary>
/// <remarks>
/// No incluye <c>UseForwardedHeaders()</c> (topología de red específica de cada despliegue, se deja
/// explícito en el <c>Program.cs</c> de quien lo use) ni ningún renderizador de OpenAPI (Swagger UI,
/// Scalar, etc. -- elección exclusiva del proyecto consumidor). Debe llamarse **antes**
/// de <c>MapAutoDiscoveryIAMEndpoints()</c>.
/// </remarks>
public static class AutoDiscoveryIAMApplicationBuilderExtensions
{
  public static WebApplication UseAutoDiscoveryIAM(this WebApplication app)
  {
    // Configurar enrutamiento básico.
    app.UseRouting();

    // Configurar middleware de manejo de excepciones
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Aplicar la política CORS definida en AddAutoDiscoveryIAM()
    app.UseCors("AllowSpecificOrigins");

    // Límites de peticiones por IP (limitador nativo; antes de la autenticación para que un
    // atacante no gaste ni una validación de token ni una consulta a la base de datos).
    app.UseRateLimiter();

    // Habilitar middleware de Autenticación
    app.UseAuthentication();

    // Habilitar middleware de Autorización por Permisos (Secure by Default)
    app.UsePermissionsAuthorization();

    // Habilitar middleware de Autorización
    app.UseAuthorization();

    // Middleware transaccional para AutoDiscoveryIAMUnitOfWork
    app.UseMiddleware<UnitOfWorkMiddleware>();

    return app;
  }
}
