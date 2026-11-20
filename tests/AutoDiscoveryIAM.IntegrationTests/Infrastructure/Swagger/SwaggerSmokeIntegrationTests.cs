using System.Net;
using System.Text.Json;

using System.Globalization;
namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Infrastructure.Swagger;

[Collection("GlobalTestSetup")]
public class SwaggerSmokeIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{

  private readonly HttpClient _client = factory.CreateClient();
  private static readonly string[] SupportedHttpMethods = ["post", "put", "get"];

  [Fact]
  public async Task SwaggerJson_ShouldBeAvailable_AndContainOpenApiShape()
  {
    var response = await _client.GetAsync("/openapi/v1.json", CancellationToken.None);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Content.Headers.ContentType.Should().NotBeNull();
    response.Content.Headers.ContentType!.MediaType.Should().NotBeNull();
    response.Content.Headers.ContentType!.MediaType!.Should().Contain("json");

    var payload = await response.Content.ReadAsStringAsync(CancellationToken.None);
    payload.Should().NotBeNullOrWhiteSpace();
    payload.Should().Contain("\"openapi\"");
    payload.Should().Contain("\"paths\"");
    payload.Should().Contain("/roles");
  }

  [Fact]
  public async Task SwaggerJson_MainEndpoints_ShouldHaveResponseExamples()
  {
    var response = await _client.GetAsync("/openapi/v1.json", CancellationToken.None);
    response.EnsureSuccessStatusCode();

    var payload = await response.Content.ReadAsStringAsync(CancellationToken.None);
    var jsonDoc = JsonDocument.Parse(payload);
    var root = jsonDoc.RootElement;

    root.TryGetProperty("paths", out var paths).Should().BeTrue("OpenAPI debe tener paths");

    var operationsMissingExamples = new List<string>();

    foreach (var pathItem in paths.EnumerateObject())
    {
      var pathName = pathItem.Name;

      foreach (var methodItem in pathItem.Value.EnumerateObject())
      {
        var methodName = methodItem.Name;

        // Solo validar operaciones principales: post, put, get (no delete, patch)
        if (!SupportedHttpMethods.Contains(methodName.ToLower(CultureInfo.InvariantCulture)))
        {
          continue;
        }

        if (!methodItem.Value.TryGetProperty("responses", out var responses))
        {
          continue;
        }

        var hasAny2xxResponse = false;
        var hasResponseExample = false;

        foreach (var responseItem in responses.EnumerateObject())
        {
          var statusCode = responseItem.Name;

          // Solo validar respuestas exitosas 2xx
          if (!statusCode.StartsWith('2'))
          {
            continue;
          }

          hasAny2xxResponse = true;

          if (responseItem.Value.TryGetProperty("content", out var content))
          {
            if (content.TryGetProperty("application/json", out var jsonContent))
            {
              // Validar que existe 'example' o 'examples'
              if (jsonContent.TryGetProperty("example", out _) || jsonContent.TryGetProperty("examples", out _))
              {
                hasResponseExample = true;
                break;
              }
            }
          }
        }

        // Endpoints sin ninguna respuesta 2xx (p. ej. redirects puros como 302) no aplican a esta regla.
        if (hasAny2xxResponse && !hasResponseExample)
        {
          operationsMissingExamples.Add($"{methodName.ToUpper(CultureInfo.InvariantCulture)} {pathName}");
        }
      }
    }

    operationsMissingExamples.Should().BeEmpty(
        $"Los siguientes endpoints principales no tienen ejemplos de respuesta registrados en OpenAPI. " +
        $"Verifica que el Docs/<Slice>OperationDocs.cs del endpoint sobreescriba SuccessExample " +
        $"(EndpointOperationDocs, ver issue #88 Fase 7). Endpoints sin ejemplos:\n{string.Join("\n", operationsMissingExamples)}");
  }
}
