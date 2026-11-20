// -----------------------------------------------------------------------
// <copyright file="RefreshTokenReuseFamilyRevocationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Security;

/// <summary>
/// <see cref="FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken.RefreshTokenHandler"/>
/// ya revoca toda la sesión del usuario al detectar el reuso de un refresh token ya rotado
/// (<c>RevokeUserTokensAsync</c>). <c>RefreshTokenIntegrationTests.RefreshToken_WithUsedToken_ReturnsUnauthorized</c>
/// solo confirma que el token reusado en sí falla; este test confirma la parte que faltaba: que
/// esa detección invalida también el resto de la cadena de tokens de la sesión, no solo el token
/// puntual reusado -- la diferencia real entre "rotación básica" y "detección de reutilización".
/// </summary>
[Collection("GlobalTestSetup")]
[Trait("Category", "Security")]
public class RefreshTokenReuseFamilyRevocationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  [Fact(DisplayName = "Refresh token: reusar un token ya rotado revoca también el token vigente derivado de la misma sesión")]
  public async Task ReuseOfRotatedToken_RevokesEntireTokenFamily_NotJustTheReusedToken()
  {
    // Arrange - Login, obtener el refresh token original de la sesión.
    var loginRequest = new LoginRequestDto("admin@example.com", "AdminPass123!");
    var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
    loginResponse.EnsureSuccessStatusCode();
    var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
    var originalRefreshToken = loginContent!.Data!.RefreshToken!;

    // Act 1 - Rotar el token una vez (legítimo): el original queda revocado, se emite uno nuevo,
    // todavía vigente en este punto.
    var firstRefreshResponse = await _client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(originalRefreshToken), _cancellationToken);
    firstRefreshResponse.EnsureSuccessStatusCode();
    var firstRefreshContent = await firstRefreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>(cancellationToken: _cancellationToken);
    var currentValidRefreshToken = firstRefreshContent!.Data!.RefreshToken!;

    // Act 2 - Reusar el token original ya revocado: dispara la detección de reuso.
    var reuseResponse = await _client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(originalRefreshToken), _cancellationToken);
    reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "el token original ya fue rotado, reusarlo debe fallar");

    // Assert - El token vigente emitido en el Act 1 (parte de la MISMA sesión, no el reusado)
    // también debe quedar invalidado como consecuencia de la detección de reuso, no solo el
    // token puntual que se reusó.
    var followUpResponse = await _client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(currentValidRefreshToken), _cancellationToken);

    followUpResponse.StatusCode.Should().Be(
      HttpStatusCode.Unauthorized,
      "la detección de reuso debe revocar toda la sesión, incluido el token todavía no comprometido directamente");
  }
}
