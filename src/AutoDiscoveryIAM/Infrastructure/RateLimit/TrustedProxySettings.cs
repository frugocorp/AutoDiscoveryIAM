// -----------------------------------------------------------------------
// <copyright file="TrustedProxySettings.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;

/// <summary>
/// Proxies inversos de confianza (sección <c>ForwardedHeaders</c>): solo de ellos acepta
/// <c>app.UseForwardedHeaders()</c> los headers <c>X-Forwarded-For</c>/<c>X-Forwarded-Proto</c>.
/// De esa IP dependen los límites de peticiones y los eventos de seguridad.
/// </summary>
public sealed class TrustedProxySettings
{
  /// <summary>
  /// Red que se usa si la sección no declara ni <see cref="KnownNetworks"/> ni
  /// <see cref="KnownProxies"/>: el rango que Docker asigna por defecto a sus redes bridge.
  /// </summary>
  public const string DefaultNetwork = "172.16.0.0/12";

  /// <summary>
  /// Redes de confianza en notación CIDR (ej. <c>10.0.0.0/8</c>).
  /// </summary>
  public IReadOnlyList<string>? KnownNetworks { get; set; }

  /// <summary>
  /// IPs individuales de confianza (ej. <c>10.0.0.5</c>).
  /// </summary>
  public IReadOnlyList<string>? KnownProxies { get; set; }

  /// <summary>
  /// Aplica la sección a <see cref="ForwardedHeadersOptions"/>. Loopback sigue siendo de confianza
  /// (default de ASP.NET Core). Un valor que no se pueda interpretar detiene el arranque.
  /// </summary>
  internal static void Apply(TrustedProxySettings settings, ForwardedHeadersOptions options)
  {
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var networks = settings.KnownNetworks ?? [];
    var proxies = settings.KnownProxies ?? [];
    if (networks.Count == 0 && proxies.Count == 0)
    {
      networks = [DefaultNetwork];
    }

    foreach (var network in networks)
    {
      if (!System.Net.IPNetwork.TryParse(network, out var parsedNetwork))
      {
        throw new InvalidOperationException($"ForwardedHeaders:KnownNetworks contiene un valor que no es una red CIDR válida: '{network}'.");
      }

      options.KnownIPNetworks.Add(parsedNetwork);
    }

    foreach (var proxy in proxies)
    {
      if (!IPAddress.TryParse(proxy, out var parsedProxy))
      {
        throw new InvalidOperationException($"ForwardedHeaders:KnownProxies contiene un valor que no es una IP válida: '{proxy}'.");
      }

      options.KnownProxies.Add(parsedProxy);
    }
  }
}
