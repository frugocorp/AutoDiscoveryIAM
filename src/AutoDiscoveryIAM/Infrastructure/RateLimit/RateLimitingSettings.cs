// -----------------------------------------------------------------------
// <copyright file="RateLimitingSettings.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;

/// <summary>
/// Límites de peticiones por IP del cliente (sección <c>RateLimiting</c>), aplicados con el limitador
/// nativo de ASP.NET Core (<c>Microsoft.AspNetCore.RateLimiting</c>). Las rutas sensibles de la librería
/// tienen límites por defecto aunque la sección no exista (ver <see cref="AutoDiscoveryIAMRateLimitDefaults"/>).
/// </summary>
/// <remarks>
/// La IP es <c>HttpContext.Connection.RemoteIpAddress</c>: nunca un header que mande el cliente. Detrás de
/// un proxy inverso, <c>app.UseForwardedHeaders()</c> la corrige solo si el proxy está declarado en la
/// sección <c>ForwardedHeaders</c> (<see cref="TrustedProxySettings"/>). Los contadores viven en memoria
/// de cada instancia.
/// </remarks>
public sealed class RateLimitingSettings
{
  /// <summary>
  /// Nombre de la sección de configuración.
  /// </summary>
  public const string SectionName = "RateLimiting";

  /// <summary>
  /// Reglas del consumidor. Una con el mismo <see cref="RateLimitRule.Endpoint"/> y
  /// <see cref="RateLimitRule.Period"/> que una regla por defecto la reemplaza. Cuando varias reglas
  /// aplican a una petición, se exigen todas.
  /// </summary>
  public IList<RateLimitRule> Rules { get; set; } = [];

  /// <summary>
  /// Rutas que ninguna regla limita, con el mismo formato que <see cref="RateLimitRule.Endpoint"/>
  /// (ej. <c>get:/openapi/*</c>).
  /// </summary>
  public IList<string> EndpointWhitelist { get; set; } = [];
}
