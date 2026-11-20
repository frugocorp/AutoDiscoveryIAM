#pragma warning disable CS8602
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Commands.Login
{

    /// <summary>
    /// Pruebas de integración para el endpoint /auth/login.
    /// </summary>
    /// <remarks>
    /// Inicializa una nueva instancia de la clase LoginIntegrationTests.
    /// </remarks>
    /// <param name="factory">Fábrica personalizada para pruebas de integración.</param>
    [Collection("GlobalTestSetup")]
    public class LoginIntegrationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

        /// <summary>
        /// Prueba que verifica que el endpoint /auth/login retorna 200 y un token válido para credenciales correctas.
        /// </summary>
        [Theory]
        [InlineData("admin@example.com", "AdminPass123!")]
        [InlineData("user@example.com", "UserPass123!")]
        public async Task Login_ReturnsOk_And_ValidToken(string username, string password)
        {
            // Arrange
            var loginRequest = new LoginRequestDto(username, password);

            // Act
            var response = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);

            // Assert
            response.EnsureSuccessStatusCode();
            var loginContent = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
            loginContent.Should().NotBeNull();
            loginContent!.Success.Should().BeTrue();
            loginContent.Data.Should().NotBeNull();
            loginContent.Data.Token.Should().NotBeNullOrWhiteSpace();
        }
    }
}



