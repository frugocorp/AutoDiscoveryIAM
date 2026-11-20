// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMRateLimiterTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;
using Microsoft.AspNetCore.Http;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.RateLimit;

public class AutoDiscoveryIAMRateLimiterTests
{
  [Fact(DisplayName = "La regla limita por IP: otra IP tiene su propio contador")]
  public void Limiter_CountsPerClientIp()
  {
    using var limiter = AutoDiscoveryIAMRateLimiter.CreateGlobalLimiter(Settings(new RateLimitRule { Endpoint = "post:/auth/login*", Period = "1m", Limit = 2 }));

    Acquire(limiter, "POST", "/auth/login", "203.0.113.1").Should().BeTrue();
    Acquire(limiter, "POST", "/auth/login/", "203.0.113.1").Should().BeTrue();
    Acquire(limiter, "POST", "/auth/login", "203.0.113.1").Should().BeFalse();
    Acquire(limiter, "POST", "/auth/login", "203.0.113.2").Should().BeTrue();
  }

  [Fact(DisplayName = "Una ruta que ninguna regla cubre no se limita")]
  public void Limiter_IgnoresUnmatchedRoutes()
  {
    using var limiter = AutoDiscoveryIAMRateLimiter.CreateGlobalLimiter(Settings(new RateLimitRule { Endpoint = "post:/auth/login*", Period = "1m", Limit = 1 }));

    for (var i = 0; i < 5; i++)
    {
      Acquire(limiter, "GET", "/users", "203.0.113.1").Should().BeTrue();
    }
  }

  [Fact(DisplayName = "Todas las reglas que aplican se exigen: la más restrictiva decide")]
  public void Limiter_ChainsAllMatchingRules()
  {
    using var limiter = AutoDiscoveryIAMRateLimiter.CreateGlobalLimiter(Settings(
      new RateLimitRule { Endpoint = "*", Period = "1m", Limit = 100 },
      new RateLimitRule { Endpoint = "post:/auth/login*", Period = "1m", Limit = 1 }));

    Acquire(limiter, "POST", "/auth/login", "203.0.113.1").Should().BeTrue();
    Acquire(limiter, "POST", "/auth/login", "203.0.113.1").Should().BeFalse();
    Acquire(limiter, "GET", "/users", "203.0.113.1").Should().BeTrue("la regla general todavía tiene cupo");
  }

  [Fact(DisplayName = "Una ruta en EndpointWhitelist no se limita aunque una regla la cubra")]
  public void Limiter_SkipsWhitelistedRoutes()
  {
    var settings = Settings(new RateLimitRule { Endpoint = "*", Period = "1m", Limit = 1 });
    settings.EndpointWhitelist.Add("get:/openapi/*");
    using var limiter = AutoDiscoveryIAMRateLimiter.CreateGlobalLimiter(settings);

    for (var i = 0; i < 5; i++)
    {
      Acquire(limiter, "GET", "/openapi/v1.json", "203.0.113.1").Should().BeTrue();
    }
  }

  [Fact(DisplayName = "Sin reglas no se limita nada")]
  public void Limiter_WithoutRules_AllowsEverything()
  {
    using var limiter = AutoDiscoveryIAMRateLimiter.CreateGlobalLimiter(new RateLimitingSettings());

    Acquire(limiter, "POST", "/auth/login", "203.0.113.1").Should().BeTrue();
  }

  [Fact(DisplayName = "La clave del cliente es la IP de la conexión, no un header, con IPv4 mapeada normalizada")]
  public void ClientKey_UsesConnectionAddress()
  {
    var context = Context("GET", "/", "::ffff:203.0.113.7");
    context.Request.Headers["X-Real-IP"] = "198.51.100.1";
    context.Request.Headers["X-Forwarded-For"] = "198.51.100.2";

    AutoDiscoveryIAMRateLimiter.ClientKey(context).Should().Be("203.0.113.7");
    AutoDiscoveryIAMRateLimiter.ClientKey(new DefaultHttpContext()).Should().Be("unknown");
  }

  private static RateLimitingSettings Settings(params RateLimitRule[] rules) => new() { Rules = [.. rules] };

  private static bool Acquire(System.Threading.RateLimiting.PartitionedRateLimiter<HttpContext> limiter, string method, string path, string ip)
  {
    using var lease = limiter.AttemptAcquire(Context(method, path, ip));
    return lease.IsAcquired;
  }

  private static DefaultHttpContext Context(string method, string path, string ip)
  {
    var context = new DefaultHttpContext();
    context.Request.Method = method;
    context.Request.Path = path;
    context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
    return context;
  }
}
