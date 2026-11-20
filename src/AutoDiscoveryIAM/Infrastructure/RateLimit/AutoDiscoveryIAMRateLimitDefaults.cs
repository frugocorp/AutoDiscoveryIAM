// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMRateLimitDefaults.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;

/// <summary>
/// Límites por IP que la librería aplica siempre a sus propias rutas sensibles (login, segundo
/// factor, refresh, restablecimiento de contraseña, activación, registro y eliminación de la propia
/// cuenta), aunque el consumidor no declare la sección <c>RateLimiting</c>.
/// </summary>
/// <remarks>
/// El consumidor sobrescribe una regla declarando en <c>RateLimiting:Rules</c> otra con el mismo
/// <c>Endpoint</c> y el mismo <c>Period</c>: la suya reemplaza a la de aquí (todas las reglas que
/// aplican a una petición se exigen, así que agregar otra sin reemplazar no permitiría subir el límite).
/// Las rutas terminan en <c>*</c> para que una barra final (<c>/auth/login/</c>), que el enrutador
/// acepta igual, comparta el mismo contador.
/// </remarks>
internal static class AutoDiscoveryIAMRateLimitDefaults
{
  /// <summary>
  /// Reglas por defecto (por IP del cliente).
  /// </summary>
  internal static IReadOnlyList<RateLimitRule> Rules { get; } =
  [
    Rule("post:/auth/login*", "1m", 10),
    Rule("post:/auth/login*", "1h", 60),
    Rule("post:/auth/2fa/verify*", "1m", 10),
    Rule("post:/auth/2fa/verify*", "1h", 60),
    Rule("post:/auth/refresh*", "1m", 30),
    Rule("post:/auth/external-login/exchange*", "1m", 10),
    Rule("post:/users/password/reset/request*", "1m", 5),
    Rule("post:/users/password/reset/request*", "1h", 20),
    Rule("post:/users/password/reset/confirm*", "1m", 10),
    Rule("post:/users/password/reset/confirm*", "1h", 60),
    Rule("post:/users/activation/resend*", "1m", 5),
    Rule("post:/users/activation/resend*", "1h", 20),
    Rule("*:/users/activation/confirm*", "1m", 10),
    Rule("*:/users/activation/confirm*", "1h", 60),
    Rule("post:/users/me/password*", "1m", 10),
    Rule("post:/users/me/2fa/*", "1m", 10),

    // Eliminar la propia cuenta pide la contraseña actual: sin límite, un token robado serviría para adivinarla.
    Rule("delete:/users/me*", "1m", 10),

    // Login con Google: el callback hace dos llamadas salientes a Google por petición.
    Rule("get:/auth/google*", "1m", 20),
    Rule("post:/auth/google/link*", "1m", 10),

    // Cada solicitud de cambio de email queda en la base de datos para revisión administrativa.
    Rule("post:/users/me/email-change-requests*", "1m", 3),
    Rule("post:/users/me/email-change-requests*", "1h", 10),

    // Registro: POST /users exacto (con y sin barra final); un comodín aquí alcanzaría a todas las
    // rutas POST bajo /users.
    Rule("post:/users", "1m", 5),
    Rule("post:/users", "1h", 30),
    Rule("post:/users/", "1m", 5),
    Rule("post:/users/", "1h", 30),
  ];

  /// <summary>
  /// Agrega a <paramref name="options"/> las reglas por defecto que el consumidor no haya sobrescrito.
  /// </summary>
  /// <param name="options">Opciones ya enlazadas desde la sección <c>RateLimiting</c>.</param>
  internal static void Apply(RateLimitingSettings options)
  {
    ArgumentNullException.ThrowIfNull(options);

    options.Rules ??= [];
    options.EndpointWhitelist ??= [];

    foreach (var rule in Rules)
    {
      var overridden = options.Rules.Any(r =>
        string.Equals(r.Endpoint, rule.Endpoint, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(r.Period, rule.Period, StringComparison.OrdinalIgnoreCase));

      if (!overridden)
      {
        options.Rules.Add(Rule(rule.Endpoint, rule.Period, rule.Limit));
      }
    }
  }

  private static RateLimitRule Rule(string endpoint, string period, int limit) =>
    new() { Endpoint = endpoint, Period = period, Limit = limit };
}
