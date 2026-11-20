// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMEndpointRouteBuilderExtensions.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Composition;

/// <summary>
/// Mapeo de endpoints del motor RBAC de AutoDiscoveryIAM: health check y todos los endpoints de features vertical slice
/// (<c>MapAllFeatureEndpoints()</c>). Sube un nivel el mismo patrón que ya existe por feature.
/// </summary>
/// <remarks>
/// No incluye ningún renderizador de OpenAPI (Swagger UI, Scalar, etc. -- elección exclusiva del
/// proyecto consumidor) ni el propio <c>MapOpenApi(...)</c> que genera el documento
/// (se deja explícito en el <c>Program.cs</c> de quien lo use, junto a su elección de renderizador).
/// </remarks>
public static class AutoDiscoveryIAMEndpointRouteBuilderExtensions
{
  /// <exception cref="InvalidOperationException">
  /// Si se mapea dentro de un grupo (<c>MapGroup(...)</c>): los límites de peticiones por defecto de
  /// las rutas sensibles (login, 2FA, reseteo de contraseña...) se comparan contra la ruta real y,
  /// con un prefijo, dejarían de aplicarse sin ningún aviso. Para servir la API bajo un prefijo usa
  /// <c>app.UsePathBase(...)</c>, que sí conserva esas rutas.
  /// </exception>
  public static IEndpointRouteBuilder MapAutoDiscoveryIAMEndpoints(this IEndpointRouteBuilder app)
  {
    if (app is RouteGroupBuilder)
    {
      throw new InvalidOperationException(
        "MapAutoDiscoveryIAMEndpoints() debe llamarse sobre la aplicación, no dentro de MapGroup(...): con un prefijo de grupo, los límites de peticiones por defecto de las rutas sensibles dejarían de aplicarse. Para un prefijo, usa app.UsePathBase(\"/prefijo\").");
    }

    // Health check: sin JWT, para que un orquestador (Docker/K8s) pueda consultarlo.
    app.MapHealthChecks("/health")
      .WithMetadata(new PermissionsNotRequiredAttribute("Health check publico para orquestadores (Docker/K8s), no requiere autenticacion"));

    // Mapear endpoints de features vertical slice
    app.MapAllFeatureEndpoints();

    return app;
  }
}
