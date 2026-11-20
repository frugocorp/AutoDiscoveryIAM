// -----------------------------------------------------------------------
// <copyright file="CorsSecurityTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Security;

/// <summary>
/// <c>AddCors</c> ya construye la política a partir de <c>Cors:AllowedOrigins</c> vía
/// <c>WithOrigins</c> (nunca <c>AllowAnyOrigin</c>) y nunca llama <c>AllowCredentials</c> --
/// combinado, un origen arbitrario nunca debería ser reflejado con credenciales habilitadas. Este
/// test es una guarda de regresión: si un futuro refactor cambia esa política (ej. agrega
/// <c>AllowAnyOrigin</c> o <c>AllowCredentials</c> por conveniencia de desarrollo), este test debe
/// fallar antes de que ese cambio llegue a producción.
/// </summary>
[Collection("GlobalTestSetup")]
[Trait("Category", "Security")]
public class CorsSecurityTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  [Fact(DisplayName = "CORS: un origen arbitrario no configurado no es reflejado en Access-Control-Allow-Origin")]
  public async Task ArbitraryOrigin_IsNeverReflectedInAccessControlAllowOrigin()
  {
    using var request = new HttpRequestMessage(HttpMethod.Get, "/users/me/permissions");
    request.Headers.Add("Origin", "https://attacker-controlled.example");

    var response = await _client.SendAsync(request, _cancellationToken);

    response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowOriginValues).Should().BeFalse(
      "ningún origen configurado en el entorno de test debería coincidir con un origen arbitrario no confiable");
    response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse(
      "la política CORS nunca debería habilitar credenciales para un origen no confiable");
  }
}
