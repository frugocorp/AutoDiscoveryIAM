// -----------------------------------------------------------------------
// <copyright file="RefreshTokenIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Pruebas de integración para el endpoint /auth/refresh.
/// Valida la funcionalidad completa de renovación de tokens JWT usando refresh tokens.
/// </summary>
[Collection("GlobalTestSetup")]
public class RefreshTokenIntegrationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    /// <summary>
    /// Prueba el flujo completo de login seguido por refresh token.
    /// Verifica que se puede renovar un token JWT usando un refresh token válido.
    /// </summary>
    [Theory]
    [InlineData("admin@example.com", "AdminPass123!")]
    [InlineData("user@example.com", "UserPass123!")]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens(string email, string password)
    {
        // Arrange - Primero hacer login para obtener un refresh token
        var loginRequest = new LoginRequestDto(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
        loginContent.Should().NotBeNull();
        loginContent!.Success.Should().BeTrue();
        loginContent.Data.Should().NotBeNull();
        loginContent.Data!.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var originalRefreshToken = loginContent.Data.RefreshToken!;
        var originalAccessToken = loginContent.Data.Token!;

        // Esperar un poco para asegurar que los timestamps sean diferentes
        await Task.Delay(1000);

        // Act - Usar el refresh token para obtener nuevos tokens
        var refreshRequest = new RefreshTokenRequestDto(originalRefreshToken);
        var refreshResponse = await _client.PostAsJsonAsync("/auth/refresh", refreshRequest, _cancellationToken);

        // Assert
        refreshResponse.EnsureSuccessStatusCode();
        var refreshContent = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>(cancellationToken: _cancellationToken);

        refreshContent.Should().NotBeNull();
        refreshContent!.Success.Should().BeTrue();
        refreshContent.Data.Should().NotBeNull();

        // Verificar que se generaron nuevos tokens
        refreshContent.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshContent.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
        refreshContent.Data.TokenType.Should().Be("Bearer");
        refreshContent.Data.ExpiresIn.Should().BeGreaterThan(0);
        refreshContent.Data.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // Verificar rotación de tokens (nuevos tokens deben ser diferentes)
        refreshContent.Data.AccessToken.Should().NotBe(originalAccessToken);
        refreshContent.Data.RefreshToken.Should().NotBe(originalRefreshToken);
    }

    /// <summary>
    /// Prueba que un refresh token inválido retorna error 401.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequestDto("invalid-refresh-token-123");

        // Act
        var response = await _client.PostAsJsonAsync("/auth/refresh", refreshRequest, _cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(cancellationToken: _cancellationToken);
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.Error.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Prueba que un refresh token vacío o nulo retorna error de validación.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RefreshToken_WithEmptyToken_ReturnsBadRequest(string invalidToken)
    {
        // Arrange
        var refreshRequest = new RefreshTokenRequestDto(invalidToken);

        // Act
        var response = await _client.PostAsJsonAsync("/auth/refresh", refreshRequest, _cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(cancellationToken: _cancellationToken);
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.Error.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Prueba que un refresh token ya usado (después de rotación) no puede usarse nuevamente.
    /// </summary>
    [Fact]
    public async Task RefreshToken_WithUsedToken_ReturnsUnauthorized()
    {
        // Arrange - Login y obtener refresh token
        var loginRequest = new LoginRequestDto("admin@example.com", "AdminPass123!");
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
        var originalRefreshToken = loginContent!.Data!.RefreshToken!;

        // Act 1 - Usar el refresh token por primera vez (debe funcionar)
        var firstRefreshRequest = new RefreshTokenRequestDto(originalRefreshToken);
        var firstRefreshResponse = await _client.PostAsJsonAsync("/auth/refresh", firstRefreshRequest, _cancellationToken);
        firstRefreshResponse.EnsureSuccessStatusCode();

        // Act 2 - Intentar usar el mismo refresh token nuevamente (debe fallar)
        var secondRefreshRequest = new RefreshTokenRequestDto(originalRefreshToken);
        var secondRefreshResponse = await _client.PostAsJsonAsync("/auth/refresh", secondRefreshRequest, _cancellationToken);

        // Assert
        secondRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await secondRefreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>(cancellationToken: _cancellationToken);
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.Error.Should().NotBeNullOrWhiteSpace();
    }
}



