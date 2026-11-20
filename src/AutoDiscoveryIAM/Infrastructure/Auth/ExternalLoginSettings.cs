// -----------------------------------------------------------------------
// <copyright file="ExternalLoginSettings.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;

/// <summary>
/// Configuración compartida por cualquier proveedor de login externo (Google hoy, otros a futuro
/// vía <see cref="FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Contracts.IExternalLoginProvider"/>),
/// a diferencia de <see cref="GoogleAuthSettings"/> que es específica de Google. Se enlaza a la
/// sección <c>Authentication</c> (no <c>Authentication:Google</c>), como hermana de la sección de
/// cada proveedor.
/// </summary>
public class ExternalLoginSettings
{
  /// <summary>
  /// URL de la app cliente (SPA) a la que se redirige el navegador al terminar el callback de
  /// cualquier proveedor externo -- éxito o error. <c>null</c>/vacío (default) preserva el
  /// comportamiento anterior: el callback responde JSON directo en vez de redirigir, útil para
  /// probar el flujo sin una app cliente real (Swagger, Postman, etc.).
  /// </summary>
  public string? ExternalLoginRedirectUri { get; set; }

  /// <summary>
  /// Cuánto tiempo (segundos) es válido el código de un solo uso que viaja en el redirect exitoso
  /// (<c>#code=...</c>) antes de canjearse en <c>POST /auth/external-login/exchange</c>. Pensado
  /// para segundos, no minutos: la SPA lo canjea de inmediato al cargar. Default 60s.
  /// </summary>
  public int ExchangeCodeExpirySeconds { get; set; } = 60;
}
