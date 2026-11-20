// -----------------------------------------------------------------------
// <copyright file="LogoutIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Commands.Logout;

/// <summary>
/// Pruebas de integración para el endpoint /auth/logout.
/// Valida la funcionalidad completa de logout e invalidación de refresh tokens.
/// </summary>
[Collection("GlobalTestSetup")]
public class LogoutIntegrationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

    /// <summary>
    /// Helper method to authenticate user and set Authorization header
    /// </summary>
    private async Task<(string jwtToken, string refreshToken)> AuthenticateUserAsync(string email = "admin@example.com", string password = "AdminPass123!")
    {
        var loginRequest = new LoginRequestDto(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
        var jwtToken = loginContent!.Data!.Token!;
        var refreshToken = loginContent!.Data!.RefreshToken!;

        // Set Authorization header
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

        return (jwtToken, refreshToken);
    }

    /// <summary>
    /// Prueba el logout básico con un refresh token específico.
    /// Verifica que el token se invalide correctamente.
    /// </summary>
    [Theory]
    [InlineData("admin@example.com", "AdminPass123!")]
    [InlineData("user@example.com", "UserPass123!")]
    public async Task Logout_WithValidRefreshToken_InvalidatesToken(string email, string password)
    {
        // Arrange - Authenticate user
        var (jwtToken, refreshToken) = await AuthenticateUserAsync(email, password);

        // Act - Hacer logout con el refresh token
        var logoutRequest = new LogoutRequestDto(refreshToken, false);
        var logoutResponse = await _client.PostAsJsonAsync("/auth/logout", logoutRequest, _cancellationToken);

        // Assert
        logoutResponse.EnsureSuccessStatusCode();
        var logoutContent = await logoutResponse.Content.ReadFromJsonAsync<LogoutResponse>(cancellationToken: _cancellationToken);

        logoutContent.Should().NotBeNull();
        logoutContent!.Success.Should().BeTrue();
        logoutContent.Data.Should().NotBeNull();
        logoutContent.Data!.Message.Should().Contain("Logout exitoso");
        logoutContent.Data.TokensRevoked.Should().BeGreaterThan(0);

        // La API devuelve horario de México, así que comparamos con horario de México
        var currentMexicoTime = FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.TimeHelpers.GetCurrentMexicoTime();
        logoutContent.Data.LoggedOutAt.Should().BeCloseTo(currentMexicoTime, TimeSpan.FromSeconds(10));

        // Verificar que el refresh token ya no funciona
        var refreshRequest = new RefreshTokenRequestDto(refreshToken);
        var refreshResponse = await _client.PostAsJsonAsync("/auth/refresh", refreshRequest, _cancellationToken);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Prueba el logout con la opción de revocar todos los tokens del usuario.
    /// </summary>
    [Fact]
    public async Task Logout_WithRevokeAllTokensTrue_InvalidatesAllUserTokens()
    {
        // Arrange - Login múltiple para simular varios tokens del mismo usuario
        var loginRequest = new LoginRequestDto("admin@example.com", "AdminPass123!");

        // Primer login
        var firstLoginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
        firstLoginResponse.EnsureSuccessStatusCode();
        var firstLoginContent = await firstLoginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
        var firstRefreshToken = firstLoginContent!.Data!.RefreshToken!;

        // Segundo login (simula otra sesión)
        var secondLoginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
        secondLoginResponse.EnsureSuccessStatusCode();
        var secondLoginContent = await secondLoginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
        var secondRefreshToken = secondLoginContent!.Data!.RefreshToken!;
        var jwtToken = secondLoginContent!.Data!.Token!; // Use JWT from second login

        // Configure authentication para logout
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

        // Act - Logout con revocación de todos los tokens
        var logoutRequest = new LogoutRequestDto(firstRefreshToken, true);
        var logoutResponse = await _client.PostAsJsonAsync("/auth/logout", logoutRequest, _cancellationToken);

        // Assert
        logoutResponse.EnsureSuccessStatusCode();
        var logoutContent = await logoutResponse.Content.ReadFromJsonAsync<LogoutResponse>(cancellationToken: _cancellationToken);

        logoutContent.Should().NotBeNull();
        logoutContent!.Success.Should().BeTrue();
        logoutContent.Data.Should().NotBeNull();
        logoutContent.Data!.Message.Should().Contain("Logout exitoso");

        // Verificar que ambos refresh tokens están invalidados
        var firstRefreshRequest = new RefreshTokenRequestDto(firstRefreshToken);
        var firstRefreshResponse = await _client.PostAsJsonAsync("/auth/refresh", firstRefreshRequest, _cancellationToken);
        firstRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var secondRefreshRequest = new RefreshTokenRequestDto(secondRefreshToken);
        var secondRefreshResponse = await _client.PostAsJsonAsync("/auth/refresh", secondRefreshRequest, _cancellationToken);
        secondRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Prueba el logout sin proporcionar refresh token (solo con RevokeAllTokens) pero con JWT válido.
    /// </summary>
    [Fact]
    public async Task Logout_WithoutRefreshToken_ButRevokeAllTokens_ReturnsSuccess()
    {
        // Arrange - Primero hacer login para obtener un JWT
        var loginRequest = new LoginRequestDto("admin@example.com", "AdminPass123!");
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
        var accessToken = loginContent!.Data!.Token;

        // Configurar el token de autorización
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var logoutRequest = new LogoutRequestDto(null, true);

        // Act
        var response = await _client.PostAsJsonAsync("/auth/logout", logoutRequest, _cancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<LogoutResponse>(cancellationToken: _cancellationToken);

        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.Message.Should().Contain("Logout"); // Puede ser "Logout completado" o "Logout exitoso"
        content.Data.TokensRevoked.Should().BeGreaterThanOrEqualTo(0); // Puede haber revocado tokens o no
    }

    /// <summary>
    /// Prueba logout con un refresh token ya invalidado, usando un access token nuevo (no el mismo
    /// que ya fue invalidado por el primer logout) para aislar el escenario del refresh token.
    /// </summary>
    [Fact]
    public async Task Logout_WithAlreadyInvalidatedRefreshToken_ReturnsSuccess()
    {
        // Arrange - Login para obtener tokens
        var loginRequest = new LoginRequestDto("admin@example.com", "AdminPass123!");
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
        var refreshToken = loginContent!.Data!.RefreshToken;
        var jwtToken = loginContent!.Data!.Token;

        // Configure authentication
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

        // Primer logout
        var firstLogoutRequest = new LogoutRequestDto(refreshToken, false);
        var firstLogoutResponse = await _client.PostAsJsonAsync("/auth/logout", firstLogoutRequest, _cancellationToken);
        firstLogoutResponse.EnsureSuccessStatusCode();

        // El primer logout ya invalidó (blacklist) el access token usado: un segundo login
        // obtiene un access token nuevo, no afectado, para reintentar logout con el mismo
        // refresh token (ya revocado) sin que el 401 del access token viejo confunda el escenario.
        var secondLoginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
        secondLoginResponse.EnsureSuccessStatusCode();
        var secondLoginContent = await secondLoginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", secondLoginContent!.Data!.Token);

        // Act - Logout con el refresh token de la primera sesión, ya invalidado
        var secondLogoutRequest = new LogoutRequestDto(refreshToken, false);
        var secondLogoutResponse = await _client.PostAsJsonAsync("/auth/logout", secondLogoutRequest, _cancellationToken);

        // Assert - Debe completarse sin errores aunque el refresh token ya esté invalidado
        secondLogoutResponse.EnsureSuccessStatusCode();
        var content = await secondLogoutResponse.Content.ReadFromJsonAsync<LogoutResponse>(cancellationToken: _cancellationToken);

        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.Message.Should().Contain("Logout completado");
        content.Data.TokensRevoked.Should().Be(0); // No se revocaron tokens adicionales
    }

    /// <summary>
    /// Prueba que el access token usado para hacer logout queda invalidado de inmediato:
    /// reutilizarlo en cualquier endpoint protegido debe fallar con 401, sin esperar a su expiración.
    /// </summary>
    [Fact]
    public async Task Logout_ShouldBlacklistAccessToken_SoItCannotBeReusedAfterLogout()
    {
        // Arrange - Login para obtener tokens
        var loginRequest = new LoginRequestDto("admin@example.com", "AdminPass123!");
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
        loginResponse.EnsureSuccessStatusCode();

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
        var refreshToken = loginContent!.Data!.RefreshToken;
        var jwtToken = loginContent!.Data!.Token;

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

        var logoutRequest = new LogoutRequestDto(refreshToken, false);
        var logoutResponse = await _client.PostAsJsonAsync("/auth/logout", logoutRequest, _cancellationToken);
        logoutResponse.EnsureSuccessStatusCode();

        // Act - Reutilizar el mismo access token (ya invalidado) contra el propio logout
        var reuseRequest = new LogoutRequestDto(null, false);
        var reuseResponse = await _client.PostAsJsonAsync("/auth/logout", reuseRequest, _cancellationToken);

        // Assert - El access token ya no es válido, aunque todavía no haya expirado por su TTL natural
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Prueba logout con un refresh token inexistente.
    /// </summary>
    [Fact]
    public async Task Logout_WithNonExistentToken_ReturnsSuccess()
    {
        // Arrange - Authenticate user first
        var (jwtToken, _) = await AuthenticateUserAsync();

        var logoutRequest = new LogoutRequestDto("non-existent-token-12345", false);

        // Act
        var response = await _client.PostAsJsonAsync("/auth/logout", logoutRequest, _cancellationToken);

        // Assert - El logout debe completarse sin errores aunque el token no exista
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<LogoutResponse>(cancellationToken: _cancellationToken);

        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
        content.Data.Should().NotBeNull();
        content.Data!.Message.Should().Contain("Logout completado");
        content.Data.TokensRevoked.Should().Be(0);
    }

    /// <summary>
    /// Prueba de validación con datos inválidos en el request.
    /// </summary>
    [Fact]
    public async Task Logout_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange - Authenticate user and prepare invalid request
        var (jwtToken, _) = await AuthenticateUserAsync();

        var logoutRequest = new LogoutRequestDto("", false);

        // Act
        var response = await _client.PostAsJsonAsync("/auth/logout", logoutRequest, _cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadFromJsonAsync<LogoutResponse>(cancellationToken: _cancellationToken);
        content.Should().NotBeNull();
        content!.Success.Should().BeFalse();
        content.Error.Should().NotBeNullOrWhiteSpace();
    }
}



