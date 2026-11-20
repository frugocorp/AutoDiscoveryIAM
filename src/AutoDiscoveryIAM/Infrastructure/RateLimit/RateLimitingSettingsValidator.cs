// -----------------------------------------------------------------------
// <copyright file="RateLimitingSettingsValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;

/// <summary>
/// Valida <see cref="RateLimitingSettings"/> al arranque (<c>ValidateOnStart</c>): una regla mal escrita
/// detiene la app en vez de dejar una ruta sin límite sin que nadie lo note.
/// </summary>
internal sealed class RateLimitingSettingsValidator : IValidateOptions<RateLimitingSettings>
{
  /// <inheritdoc />
  public ValidateOptionsResult Validate(string? name, RateLimitingSettings options)
  {
    ArgumentNullException.ThrowIfNull(options);

    var errors = new List<string>();
    foreach (var rule in options.Rules)
    {
      if (!RateLimitEndpointPattern.IsValid(rule.Endpoint))
      {
        errors.Add($"RateLimiting:Rules tiene un Endpoint inválido: '{rule.Endpoint}'. Usa '*' o 'metodo:/ruta' (ej. 'post:/auth/login*').");
      }

      if (!RateLimitRule.TryParsePeriod(rule.Period, out _))
      {
        errors.Add($"RateLimiting:Rules tiene un Period inválido en '{rule.Endpoint}': '{rule.Period}'. Usa un número seguido de s, m, h o d (ej. '1m').");
      }

      if (rule.Limit <= 0)
      {
        errors.Add($"RateLimiting:Rules tiene un Limit inválido en '{rule.Endpoint}' ({rule.Period}): debe ser mayor que cero.");
      }
    }

    foreach (var entry in options.EndpointWhitelist)
    {
      if (!RateLimitEndpointPattern.IsValid(entry))
      {
        errors.Add($"RateLimiting:EndpointWhitelist tiene un valor inválido: '{entry}'. Usa '*' o 'metodo:/ruta' (ej. 'get:/openapi/*').");
      }
    }

    return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
  }
}
