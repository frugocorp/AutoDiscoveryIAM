// -----------------------------------------------------------------------
// <copyright file="ExceptionHandlingMiddlewareDiagnosticsEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.SampleHost.Infrastructure;

/// <summary>
/// Endpoint de diagnóstico de este host (solo en builds Debug y solo para usuarios IsDevelopment):
/// lanza una excepción a propósito para comprobar que ExceptionHandlingMiddleware la convierte al sobre
/// ApiResponse. Vive aquí y no en la librería: el paquete se compila en Release, así que para un
/// consumidor nunca existiría, y una ruta que falla a propósito no es parte del motor.
/// </summary>
public static class ExceptionHandlingMiddlewareDiagnosticsEndPoint
{
  public static void MapExceptionHandlingMiddlewareDiagnostics(this WebApplication app)
  {
#if DEBUG
    app.MapGet("/api/ExceptionHandlingMiddlewareDiagnostics/throw", () =>
    {
      throw new InvalidOperationException("************* Excepción intencional para pruebas de middleware de excepciones. ************");
    })
    .ExcludeFromDescription() // Oculta de Swagger
    .WithMetadata(new RequiresDevelopmentAccessAttribute("Endpoint de diagnóstico crítico - Solo para usuarios Development"));
#endif
  }
}
