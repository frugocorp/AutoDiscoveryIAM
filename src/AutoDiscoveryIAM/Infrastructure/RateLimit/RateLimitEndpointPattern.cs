// -----------------------------------------------------------------------
// <copyright file="RateLimitEndpointPattern.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;

/// <summary>
/// Formato <c>*</c> | <c>metodo:/ruta</c> de <see cref="RateLimitRule.Endpoint"/> y
/// <see cref="RateLimitingSettings.EndpointWhitelist"/>.
/// </summary>
internal static class RateLimitEndpointPattern
{
  /// <summary>
  /// Indica si <paramref name="endpoint"/> tiene un formato válido.
  /// </summary>
  internal static bool IsValid(string? endpoint)
  {
    if (string.Equals(endpoint, "*", StringComparison.Ordinal))
    {
      return true;
    }

    var separator = endpoint?.IndexOf(':', StringComparison.Ordinal) ?? -1;
    return separator > 0
      && separator < endpoint!.Length - 1
      && endpoint[separator + 1] is '/' or '*';
  }

  /// <summary>
  /// Indica si una petición con <paramref name="method"/> y <paramref name="path"/> cae en
  /// <paramref name="endpoint"/>. Sin distinguir mayúsculas; <c>*</c> en la ruta acepta cualquier
  /// secuencia de caracteres (también vacía).
  /// </summary>
  internal static bool Matches(string endpoint, string method, string path)
  {
    if (string.Equals(endpoint, "*", StringComparison.Ordinal))
    {
      return true;
    }

    var separator = endpoint.IndexOf(':', StringComparison.Ordinal);
    if (separator <= 0)
    {
      return false;
    }

    var verb = endpoint.AsSpan(0, separator);
    if (verb is not "*" && !verb.Equals(method, StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    return WildcardMatch(endpoint.AsSpan(separator + 1), path);
  }

  private static bool WildcardMatch(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
  {
    int p = 0, t = 0, star = -1, resumeAt = 0;
    while (t < text.Length)
    {
      if (p < pattern.Length && pattern[p] == '*')
      {
        star = p++;
        resumeAt = t;
      }
      else if (p < pattern.Length && char.ToUpperInvariant(pattern[p]) == char.ToUpperInvariant(text[t]))
      {
        p++;
        t++;
      }
      else if (star >= 0)
      {
        p = star + 1;
        t = ++resumeAt;
      }
      else
      {
        return false;
      }
    }

    while (p < pattern.Length && pattern[p] == '*')
    {
      p++;
    }

    return p == pattern.Length;
  }
}
