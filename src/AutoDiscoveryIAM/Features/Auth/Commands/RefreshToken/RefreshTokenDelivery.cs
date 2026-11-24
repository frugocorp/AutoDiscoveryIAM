// -----------------------------------------------------------------------
// <copyright file="RefreshTokenDelivery.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Dónde entrega la API el refresh token.
/// </summary>
public enum RefreshTokenDeliveryMode
{
  /// <summary>
  /// Solo en el cuerpo JSON (comportamiento por defecto).
  /// </summary>
  Body,

  /// <summary>
  /// Solo en una cookie HttpOnly: el cuerpo lleva <c>refreshToken: null</c>, para las SPA que no
  /// quieren guardarlo en storage del navegador.
  /// </summary>
  Cookie,

  /// <summary>
  /// En la cookie y en el cuerpo, para servir a la vez a una SPA y a clientes sin cookies (móvil),
  /// o para migrar de uno a otro.
  /// </summary>
  Both,
}

/// <summary>
/// Atributos de la cookie del refresh token (<see cref="RefreshTokenSettings.Cookie"/>).
/// </summary>
public sealed class RefreshTokenCookieSettings
{
  /// <summary>
  /// Nombre de la cookie. Default <c>adiam_refresh_token</c>.
  /// </summary>
  public string Name { get; set; } = "adiam_refresh_token";

  /// <summary>
  /// Ruta de la cookie, relativa al <c>PathBase</c> de la app: el navegador solo la manda a las rutas
  /// de autenticación. Default <c>/auth</c>.
  /// </summary>
  public string Path { get; set; } = "/auth";

  /// <summary>
  /// Dominio de la cookie. <c>null</c> (default): solo el host de la API.
  /// </summary>
  public string? Domain { get; set; }

  /// <summary>
  /// Política SameSite. Default <c>Strict</c>: el navegador no la manda en peticiones iniciadas por
  /// otro sitio. <c>None</c> solo si la SPA vive en otro sitio (no solo otro subdominio).
  /// </summary>
  public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;
}

/// <summary>
/// Entrega, lee y borra el refresh token según <see cref="RefreshTokenSettings.Delivery"/>.
/// </summary>
public interface IRefreshTokenDelivery
{
  /// <summary>
  /// Si la cookie está en uso (<c>Cookie</c> o <c>Both</c>).
  /// </summary>
  bool UsesCookie { get; }

  /// <summary>
  /// Escribe la cookie si corresponde y devuelve el valor que debe ir en el cuerpo (<c>null</c> en
  /// modo <c>Cookie</c>).
  /// </summary>
  string? Deliver(HttpContext context, string? refreshToken);

  /// <summary>
  /// El refresh token de la cookie, o <c>null</c> si la cookie no está en uso o no viene.
  /// </summary>
  string? ReadFromCookie(HttpContext context);

  /// <summary>
  /// Borra la cookie del navegador (logout o refresh rechazado).
  /// </summary>
  void Clear(HttpContext context);
}

/// <summary>
/// Implementación de <see cref="IRefreshTokenDelivery"/>.
/// </summary>
public sealed class RefreshTokenDelivery(IOptions<RefreshTokenSettings> settings) : IRefreshTokenDelivery
{
  /// <summary>
  /// Encabezado que exige <c>POST /auth/refresh</c> cuando el token sale de la cookie (con cualquier
  /// valor, por ejemplo <c>XMLHttpRequest</c>). Un encabezado propio obliga al navegador a hacer el
  /// preflight de CORS, así que una página de otro sitio no puede forzar la rotación aunque la cookie
  /// viaje con <c>SameSite=None</c>.
  /// </summary>
  public const string RequiredCookieRequestHeader = "X-Requested-With";

  private readonly RefreshTokenSettings _settings = settings.Value;

  /// <inheritdoc />
  public bool UsesCookie => _settings.Delivery != RefreshTokenDeliveryMode.Body;

  /// <inheritdoc />
  public string? Deliver(HttpContext context, string? refreshToken)
  {
    ArgumentNullException.ThrowIfNull(context);
    if (refreshToken == null || !UsesCookie)
    {
      return refreshToken;
    }

    var options = BuildOptions(context);
    options.Expires = DateTimeOffset.UtcNow.AddDays(_settings.ExpiryDays);
    context.Response.Cookies.Append(_settings.Cookie.Name, refreshToken, options);
    return _settings.Delivery == RefreshTokenDeliveryMode.Cookie ? null : refreshToken;
  }

  /// <inheritdoc />
  public string? ReadFromCookie(HttpContext context)
  {
    ArgumentNullException.ThrowIfNull(context);
    return UsesCookie && context.Request.Cookies.TryGetValue(_settings.Cookie.Name, out var value) && !string.IsNullOrEmpty(value)
      ? value
      : null;
  }

  /// <inheritdoc />
  public void Clear(HttpContext context)
  {
    ArgumentNullException.ThrowIfNull(context);
    if (UsesCookie)
    {
      context.Response.Cookies.Delete(_settings.Cookie.Name, BuildOptions(context));
    }
  }

  private CookieOptions BuildOptions(HttpContext context)
  {
    return new CookieOptions
    {
      HttpOnly = true,
      Secure = true,
      SameSite = _settings.Cookie.SameSite,
      Path = context.Request.PathBase.Add(new PathString(_settings.Cookie.Path)).Value,
      Domain = _settings.Cookie.Domain,
      IsEssential = true,
    };
  }
}
