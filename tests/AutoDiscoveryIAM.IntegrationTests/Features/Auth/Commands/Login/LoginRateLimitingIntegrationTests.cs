// -----------------------------------------------------------------------
// <copyright file="LoginRateLimitingIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;
using AwesomeAssertions;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Commands.Login;

/// <summary>
/// Límites por defecto que la librería aplica a sus rutas sensibles sin que el consumidor declare
/// ninguna regla (<see cref="AutoDiscoveryIAMRateLimitDefaults"/>). Antes estas pruebas aceptaban
/// 401 o 429 indistintamente, y por eso nunca detectaron que el límite de login no se aplicaba.
/// </summary>
[Collection("GlobalTestSetup")]
[Trait("Category", "Security")]
public class LoginRateLimitingIntegrationTests(RateLimitedWebApplicationFactory factory) : IClassFixture<RateLimitedWebApplicationFactory>
{
  private readonly HttpClient _client = factory.CreateClient();
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  [Fact(DisplayName = "Rate Limiting: el login se bloquea con 429 al pasar el límite por minuto, también con barra final")]
  public async Task Login_ExceedingDefaultLimit_Returns429()
  {
    var limit = PerMinuteLimit("post:/auth/login*");
    var loginRequest = new LoginRequestDto($"ratelimit-{Guid.NewGuid():N}@example.com", "InvalidPassword123!");

    for (var i = 1; i <= limit; i++)
    {
      using var allowed = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
      allowed.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"la petición {i} está dentro del límite");
    }

    using var blocked = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
    blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    blocked.Headers.RetryAfter.Should().NotBeNull("el cliente debe saber cuándo reintentar");
    var body = await blocked.Content.ReadFromJsonAsync<ApiResponse<object>>(cancellationToken: _cancellationToken);
    body!.Success.Should().BeFalse();
    body.ErrorInfo!.Key.Should().Be(ErrorInfo.Status429TooManyRequests.Key, "el 429 usa el mismo envelope que el resto de la API");

    using var trailingSlash = await _client.PostAsJsonAsync("/auth/login/", loginRequest, _cancellationToken);
    trailingSlash.StatusCode.Should().Be(HttpStatusCode.TooManyRequests, "una barra final no debe abrir un contador nuevo");
  }

  [Fact(DisplayName = "Rate Limiting: cambiar X-Real-IP en cada petición no salta el límite")]
  public async Task Login_SpoofingClientIpHeaders_StillReturns429()
  {
    // Ruta que ninguna otra prueba de esta clase usa: el contador se comparte dentro de la fábrica.
    // X-Forwarded-For no se prueba aquí: el TestServer llega desde loopback, que es un proxy de
    // confianza, así que ese header sí debe respetarse.
    var limit = PerMinuteLimit("post:/auth/external-login/exchange*");

    for (var i = 1; i <= limit + 1; i++)
    {
      using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/external-login/exchange")
      {
        Content = JsonContent.Create(new { }),
      };
      request.Headers.Add("X-Real-IP", $"203.0.113.{i}");

      using var response = await _client.SendAsync(request, _cancellationToken);

      if (i <= limit)
      {
        response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"la petición {i} está dentro del límite");
      }
      else
      {
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests, "la IP sale de la conexión, no de un header que manda el cliente");
      }
    }
  }

  [Theory(DisplayName = "Rate Limiting: las demás rutas sensibles también tienen límite por defecto")]
  [InlineData("/auth/2fa/verify", "post:/auth/2fa/verify*")]
  [InlineData("/auth/refresh", "post:/auth/refresh*")]
  [InlineData("/users/password/reset/request", "post:/users/password/reset/request*")]
  [InlineData("/users/password/reset/confirm", "post:/users/password/reset/confirm*")]
  [InlineData("/users/activation/resend", "post:/users/activation/resend*")]
  [InlineData("/users", "post:/users")]
  public async Task SensitiveEndpoint_ExceedingDefaultLimit_Returns429(string path, string ruleEndpoint)
  {
    var limit = PerMinuteLimit(ruleEndpoint);

    // Cuerpo vacío: el límite se aplica antes de validar, así que no importa la respuesta de cada
    // petición permitida, solo que ninguna sea 429 hasta pasar el límite.
    for (var i = 1; i <= limit; i++)
    {
      using var allowed = await _client.PostAsJsonAsync(path, new { }, _cancellationToken);
      allowed.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"la petición {i} a {path} está dentro del límite");
    }

    using var blocked = await _client.PostAsJsonAsync(path, new { }, _cancellationToken);
    blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests, $"{path} debe limitarse sin que el consumidor declare reglas");
  }

  [Fact(DisplayName = "Rate Limiting: Endpoints en whitelist no están limitados")]
  public async Task SwaggerEndpoint_NoRateLimit_AlwaysReturns200()
  {
    for (var i = 1; i <= 30; i++)
    {
      using var response = await _client.GetAsync(new Uri("/swagger/index.html", UriKind.Relative), _cancellationToken);
      response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"la petición {i} a /swagger no debe limitarse");
    }
  }

  private static int PerMinuteLimit(string endpoint) =>
    (int)AutoDiscoveryIAMRateLimitDefaults.Rules.Single(r => r.Endpoint == endpoint && r.Period == "1m").Limit;
}
