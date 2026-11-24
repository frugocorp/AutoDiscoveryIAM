using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Pruebas de la entrega del refresh token en el cuerpo o en cookie HttpOnly.
/// </summary>
public class RefreshTokenDeliveryTests
{
  private static RefreshTokenDelivery Create(RefreshTokenDeliveryMode mode) =>
    new(Options.Create(new RefreshTokenSettings { Delivery = mode }));

  private static string SetCookieHeader(HttpContext context) => context.Response.Headers.SetCookie.ToString();

  [Fact(DisplayName = "Body: el token va en el cuerpo y no se escribe cookie")]
  public void Deliver_Body_ReturnsTokenWithoutCookie()
  {
    var context = new DefaultHttpContext();

    Create(RefreshTokenDeliveryMode.Body).Deliver(context, "token").Should().Be("token");

    SetCookieHeader(context).Should().BeEmpty();
  }

  [Fact(DisplayName = "Cookie: el cuerpo queda sin token y la cookie es HttpOnly, Secure, SameSite=Strict, bajo PathBase/auth")]
  public void Deliver_Cookie_WritesHardenedCookieAndHidesToken()
  {
    var context = new DefaultHttpContext();
    context.Request.PathBase = "/api";

    Create(RefreshTokenDeliveryMode.Cookie).Deliver(context, "token").Should().BeNull();

    var header = SetCookieHeader(context);
    header.Contains("adiam_refresh_token=token", StringComparison.Ordinal).Should().BeTrue();
    header.Contains("httponly", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    header.Contains("secure", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    header.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    header.Contains("path=/api/auth", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
  }

  [Fact(DisplayName = "Both: el token va en el cuerpo y en la cookie")]
  public void Deliver_Both_ReturnsTokenAndWritesCookie()
  {
    var context = new DefaultHttpContext();

    Create(RefreshTokenDeliveryMode.Both).Deliver(context, "token").Should().Be("token");

    SetCookieHeader(context).Contains("adiam_refresh_token=token", StringComparison.Ordinal).Should().BeTrue();
  }

  [Fact(DisplayName = "La cookie solo se lee cuando está en uso")]
  public void ReadFromCookie_OnlyWhenCookieInUse()
  {
    var context = new DefaultHttpContext();
    context.Request.Headers.Cookie = "adiam_refresh_token=desde-cookie";

    Create(RefreshTokenDeliveryMode.Cookie).ReadFromCookie(context).Should().Be("desde-cookie");
    Create(RefreshTokenDeliveryMode.Body).ReadFromCookie(context).Should().BeNull();
  }

  [Fact(DisplayName = "Clear borra la cookie con la misma ruta")]
  public void Clear_ExpiresCookie()
  {
    var context = new DefaultHttpContext();

    Create(RefreshTokenDeliveryMode.Cookie).Clear(context);

    var header = SetCookieHeader(context);
    header.Contains("adiam_refresh_token=;", StringComparison.Ordinal).Should().BeTrue();
    header.Contains("expires=thu, 01 jan 1970", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    header.Contains("path=/auth", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
  }
}
