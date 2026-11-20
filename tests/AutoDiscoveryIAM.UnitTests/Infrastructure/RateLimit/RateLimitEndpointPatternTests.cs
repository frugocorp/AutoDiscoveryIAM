// -----------------------------------------------------------------------
// <copyright file="RateLimitEndpointPatternTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.RateLimit;

public class RateLimitEndpointPatternTests
{
  [Theory(DisplayName = "Matches compara método y ruta sin distinguir mayúsculas, con * como comodín")]
  [InlineData("*", "GET", "/cualquier/ruta", true)]
  [InlineData("post:/auth/login*", "POST", "/auth/login", true)]
  [InlineData("post:/auth/login*", "post", "/AUTH/LOGIN/", true)]
  [InlineData("post:/auth/login*", "GET", "/auth/login", false)]
  [InlineData("post:/auth/login*", "POST", "/auth/logout", false)]
  [InlineData("post:/users", "POST", "/users", true)]
  [InlineData("post:/users", "POST", "/users/", false)]
  [InlineData("post:/users", "POST", "/users/abc/roles", false)]
  [InlineData("*:/users/activation/confirm*", "GET", "/users/activation/confirm", true)]
  [InlineData("post:/users/me/2fa/*", "POST", "/users/me/2fa/enroll", true)]
  [InlineData("post:/users/me/2fa/*", "POST", "/users/me/2fa", false)]
  [InlineData("get:/a*b*c", "GET", "/axxbyyc", true)]
  [InlineData("get:/a*b*c", "GET", "/axxbyy", false)]
  public void Matches_ComparesMethodAndPath(string endpoint, string method, string path, bool expected)
  {
    RateLimitEndpointPattern.Matches(endpoint, method, path).Should().Be(expected);
  }

  [Theory(DisplayName = "IsValid acepta * o metodo:/ruta y rechaza lo demás")]
  [InlineData("*", true)]
  [InlineData("post:/auth/login*", true)]
  [InlineData("*:/users*", true)]
  [InlineData("get:*", true)]
  [InlineData(null, false)]
  [InlineData("", false)]
  [InlineData("/auth/login", false)]
  [InlineData("post:", false)]
  [InlineData(":/auth", false)]
  [InlineData("post:auth/login", false)]
  public void IsValid_ChecksFormat(string? endpoint, bool expected)
  {
    RateLimitEndpointPattern.IsValid(endpoint).Should().Be(expected);
  }

  [Theory(DisplayName = "TryParsePeriod interpreta s, m, h y d")]
  [InlineData("30s", 30)]
  [InlineData("1m", 60)]
  [InlineData("1H", 3600)]
  [InlineData("1d", 86400)]
  public void TryParsePeriod_ValidValues(string period, int expectedSeconds)
  {
    RateLimitRule.TryParsePeriod(period, out var window).Should().BeTrue();
    window.TotalSeconds.Should().Be(expectedSeconds);
  }

  [Theory(DisplayName = "TryParsePeriod rechaza valores inválidos")]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("m")]
  [InlineData("0m")]
  [InlineData("-1m")]
  [InlineData("1w")]
  [InlineData("1.5m")]
  public void TryParsePeriod_InvalidValues(string? period)
  {
    RateLimitRule.TryParsePeriod(period, out _).Should().BeFalse();
  }
}
