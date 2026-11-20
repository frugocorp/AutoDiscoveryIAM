// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMRateLimiter.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Threading.RateLimiting;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using Microsoft.AspNetCore.RateLimiting;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;

/// <summary>
/// Traduce <see cref="RateLimitingSettings"/> al limitador global de <c>app.UseRateLimiter()</c>: un
/// limitador de ventana fija por regla y por IP del cliente, encadenados (una petición pasa solo si
/// todas las reglas que le aplican la permiten).
/// </summary>
internal static class AutoDiscoveryIAMRateLimiter
{
  /// <summary>Mensaje de la respuesta 429.</summary>
  internal const string RejectionMessage = "Demasiadas peticiones. Espera un momento antes de volver a intentarlo.";

  private const string UnknownClient = "unknown";

  /// <summary>
  /// Configura <paramref name="options"/> con las reglas ya validadas de <paramref name="settings"/>.
  /// </summary>
  internal static void Configure(RateLimiterOptions options, RateLimitingSettings settings)
  {
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = OnRejectedAsync;
    options.GlobalLimiter = CreateGlobalLimiter(settings);
  }

  /// <summary>
  /// Crea el limitador global. Las reglas se copian: cambiar <paramref name="settings"/> después no lo afecta.
  /// </summary>
  internal static PartitionedRateLimiter<HttpContext> CreateGlobalLimiter(RateLimitingSettings settings)
  {
    var whitelist = settings.EndpointWhitelist.ToArray();
    var limiters = settings.Rules.Select(rule => CreateRuleLimiter(rule, whitelist)).ToArray();

    return limiters.Length == 0
      ? PartitionedRateLimiter.Create<HttpContext, string>(_ => RateLimitPartition.GetNoLimiter(string.Empty))
      : PartitionedRateLimiter.CreateChained(limiters);
  }

  /// <summary>
  /// Clave del cliente: la IP de la conexión (ya corregida por <c>UseForwardedHeaders()</c> si vino de
  /// un proxy de confianza), con las IPv4 mapeadas a IPv6 normalizadas para no tener dos contadores.
  /// </summary>
  internal static string ClientKey(HttpContext context)
  {
    var address = context.Connection.RemoteIpAddress;
    if (address is null)
    {
      return UnknownClient;
    }

    return (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
  }

  private static PartitionedRateLimiter<HttpContext> CreateRuleLimiter(RateLimitRule rule, string[] whitelist)
  {
    var endpoint = rule.Endpoint;
    var limit = rule.Limit;
    if (!RateLimitRule.TryParsePeriod(rule.Period, out var window))
    {
      throw new InvalidOperationException($"RateLimiting:Rules tiene un Period inválido en '{endpoint}': '{rule.Period}'.");
    }

    return PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
      var method = context.Request.Method;
      var path = context.Request.Path.Value ?? "/";

      if (!RateLimitEndpointPattern.Matches(endpoint, method, path)
        || Array.Exists(whitelist, entry => RateLimitEndpointPattern.Matches(entry, method, path)))
      {
        return RateLimitPartition.GetNoLimiter(string.Empty);
      }

      return RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => new FixedWindowRateLimiterOptions
      {
        PermitLimit = limit,
        Window = window,
        QueueLimit = 0,
        AutoReplenishment = true,
      });
    });
  }

  private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
  {
    var response = context.HttpContext.Response;
    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
    {
      response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
    }

    var body = new ApiResponse<object>(false, null, RejectionMessage, ErrorInfo.Status429TooManyRequests);
    await response.WriteAsJsonAsync(body, cancellationToken);
  }
}
