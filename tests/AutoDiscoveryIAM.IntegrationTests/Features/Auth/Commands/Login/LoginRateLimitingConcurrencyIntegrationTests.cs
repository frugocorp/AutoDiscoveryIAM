// -----------------------------------------------------------------------
// <copyright file="LoginRateLimitingConcurrencyIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Commands.Login;

/// <summary>
/// Issue #075: a diferencia de <see cref="LoginRateLimitingIntegrationTests"/> (secuencial), esta
/// prueba dispara una ráfaga REALMENTE concurrente (<c>Task.WhenAll</c>) contra el login, para
/// confirmar que el límite por defecto se respeta exactamente también bajo concurrencia real.
/// </summary>
[Collection("GlobalTestSetup")]
[Trait("Category", "Security")]
public class LoginRateLimitingConcurrencyIntegrationTests(RateLimitedWebApplicationFactory factory)
    : IClassFixture<RateLimitedWebApplicationFactory>
{
  private readonly RateLimitedWebApplicationFactory _factory = factory;
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  [Fact(DisplayName = "Rate Limiting: una rafaga realmente concurrente respeta el limite configurado")]
  public async Task Login_WithConcurrentBurst_ShouldRespectConfiguredLimit()
  {
    var loginRateLimit = (int)AutoDiscoveryIAMRateLimitDefaults.Rules
      .Single(r => r.Endpoint == "post:/auth/login*" && r.Period == "1m").Limit;
    var burstSize = loginRateLimit + 5;
    var loginRequest = new LoginRequestDto($"concurrent-ratelimit-{Guid.NewGuid():N}@example.com", "InvalidPassword123!");

    using var client = _factory.CreateClient();

    var tasks = Enumerable.Range(0, burstSize)
        .Select(_ => client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken))
        .ToArray();
    var responses = await Task.WhenAll(tasks);

    var statusCodes = responses.Select(r => r.StatusCode).ToList();

    statusCodes.Count(sc => sc == HttpStatusCode.Unauthorized).Should().Be(loginRateLimit, "exactamente las peticiones dentro del límite llegan al handler");
    statusCodes.Count(sc => sc == HttpStatusCode.TooManyRequests).Should().Be(burstSize - loginRateLimit, "el resto de la ráfaga debe recibir 429");

    foreach (var response in responses)
    {
      response.Dispose();
    }
  }
}
