using System.Net;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Infrastructure.HealthCheck;

/// <summary>
/// Issue #076: <c>/health</c> debe responder sin requerir JWT — un orquestador (Docker/K8s) no
/// tiene forma de autenticarse, y hoy la app no puede reportar si puede hablar con la BD.
/// </summary>
[Collection("GlobalTestSetup")]
public class HealthCheckIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task Health_WhenCalledWithoutAuthentication_ShouldReturnOk()
  {
    var response = await _client.GetAsync("/health", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }
}
