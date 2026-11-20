using System.Net;
using System.Net.Http.Json;
using System.Web;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AwesomeAssertions;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Commands.Login;

/// <summary>
/// Cubre <see cref="ExternalLoginSettings.ExternalLoginRedirectUri"/> configurado -- el callback de
/// Google debe redirigir (302) a esa URL en vez de responder JSON directo: en éxito con un código
/// de un solo uso (<c>#code=...</c>, patrón "authorization code" de OAuth2, canjeable en
/// <c>POST /auth/external-login/exchange</c>), en error con el detalle en el fragmento. Reutiliza
/// <c>FakeGoogleProvider</c> de <see cref="GoogleLoginCallbackIntegrationTests"/> (misma
/// ensambladura, visibilidad interna).
/// </summary>
[Collection("GlobalTestSetup")]
public class GoogleLoginCallbackRedirectIntegrationTests(GoogleAuthRedirectTestWebApplicationFactory factory)
  : IClassFixture<GoogleAuthRedirectTestWebApplicationFactory>
{
    private const string ConfiguredRedirectUri = "https://app.example.com/auth/callback";

    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    private readonly GoogleAuthRedirectTestWebApplicationFactory _factory = factory;
    private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    [Fact]
    public async Task GoogleCallback_WithActiveLinkedUser_AndRedirectConfigured_RedirectsWithExchangeCodeInFragment()
    {
        // "active-code" es uno de los 4 codigos fijos que reconoce FakeGoogleProvider -- siempre
        // resuelve al mismo par (sub-active, active-google-test@example.com), asi que el seed usa
        // exactamente ese par (no uno inventado) para que el usuario realmente coincida.
        await SeedUserWithGoogleLinkAsync("sub-active", "active-google-test@example.com", isActive: true);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/google/callback?code=active-code&state=active-state-redirect-1");
        request.Headers.Add("Cookie", "google_oauth_state=active-state-redirect-1");

        var response = await _client.SendAsync(request, _cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location.Should().NotBeNull().Subject!;
        location.ToString().Should().StartWith($"{ConfiguredRedirectUri}#");
        location.Fragment.Should().Contain("code=");
        // El token/refreshToken reales NUNCA deben aparecer en la URL -- solo el codigo de un solo uso.
        location.Fragment.Should().NotContain("token=").And.NotContain("refreshToken=").And.NotContain("error=");
    }

    [Fact]
    public async Task GoogleCallback_WithStateInvalid_AndRedirectConfigured_RedirectsWithErrorInFragment()
    {
        var response = await _client.GetAsync("/auth/google/callback?code=active-code&state=wrong-state", _cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location.Should().NotBeNull().Subject!;
        location.ToString().Should().StartWith($"{ConfiguredRedirectUri}#");
        location.Fragment.Should().Contain("error=Status401Unauthorized").And.Contain("errorDescription=");
        location.Fragment.Should().NotContain("code=");
    }

    [Fact]
    public async Task ExchangeLoginCode_WithCodeFromRedirect_ReturnsRealTokenAndConsumesCode()
    {
        // Mismo par fijo que reconoce FakeGoogleProvider para "active-code" -- el seed es idempotente
        // (SeedUserWithGoogleLinkAsync busca por email antes de crear), seguro de llamar de nuevo.
        await SeedUserWithGoogleLinkAsync("sub-active", "active-google-test@example.com", isActive: true);

        using var callbackRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/google/callback?code=active-code&state=active-state-redirect-2");
        callbackRequest.Headers.Add("Cookie", "google_oauth_state=active-state-redirect-2");
        var callbackResponse = await _client.SendAsync(callbackRequest, _cancellationToken);
        var location = callbackResponse.Headers.Location.Should().NotBeNull().Subject!;
        var plaintextCode = HttpUtility.ParseQueryString(location.Fragment.TrimStart('#'))["code"]!;

        var exchangeResponse = await _client.PostAsJsonAsync(
          "/auth/external-login/exchange",
          new ExchangeLoginCodeRequestDto(plaintextCode),
          _cancellationToken);

        exchangeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await exchangeResponse.Content.ReadFromJsonAsync<ExchangeLoginCodeResponse>(cancellationToken: _cancellationToken);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Token.Should().NotBeNullOrWhiteSpace();
        payload.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // De un solo uso: reintentar el mismo codigo ya consumido falla.
        var secondAttempt = await _client.PostAsJsonAsync(
          "/auth/external-login/exchange",
          new ExchangeLoginCodeRequestDto(plaintextCode),
          _cancellationToken);
        secondAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExchangeLoginCode_WithUnknownCode_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
          "/auth/external-login/exchange",
          new ExchangeLoginCodeRequestDto("codigo-que-nunca-existio"),
          _cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var payload = await response.Content.ReadFromJsonAsync<ExchangeLoginCodeResponse>(cancellationToken: _cancellationToken);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ExchangeLoginCode_WithEmptyCode_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
          "/auth/external-login/exchange",
          new ExchangeLoginCodeRequestDto(string.Empty),
          _cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task SeedUserWithGoogleLinkAsync(string providerId, string email, bool isActive)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

        var existingUser = await db.Users.FirstOrDefaultAsync(x => x.Email == email, _cancellationToken);

        if (existingUser == null)
        {
            existingUser = new User
            {
                Username = $"google_int_{Guid.NewGuid():N}"[..20],
                Email = email,
                PasswordHash = "external-login-only",
                IsEmailVerified = true,
            };

            db.Users.Add(existingUser);
        }

        existingUser.Status = isActive ? UserStatusCatalog.Active : UserStatusCatalog.Pending;
        existingUser.IsActive = isActive;
        existingUser.IsEmailVerified = true;

        await db.SaveChangesAsync(_cancellationToken);

        var hashedProviderId = ExternalProviderIdHasher.Hash(providerId);

        var existingLink = await db.ExternalLogins
          .FirstOrDefaultAsync(
            x => (x.Provider == ExternalLoginProviders.Google && x.ProviderId == hashedProviderId) ||
                 (x.UserId == existingUser.Id && x.Provider == ExternalLoginProviders.Google),
            _cancellationToken);

        if (existingLink == null)
        {
            db.ExternalLogins.Add(new ExternalLogin
            {
                UserId = existingUser.Id,
                Provider = ExternalLoginProviders.Google,
                ProviderId = hashedProviderId,
            });
        }
        else
        {
            existingLink.UserId = existingUser.Id;
            existingLink.Provider = ExternalLoginProviders.Google;
            existingLink.ProviderId = hashedProviderId;
            existingLink.IsDeleted = false;
        }

        await db.SaveChangesAsync(_cancellationToken);
    }
}

public class GoogleAuthRedirectTestWebApplicationFactory : GoogleAuthTestWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.Configure<ExternalLoginSettings>(options =>
              options.ExternalLoginRedirectUri = "https://app.example.com/auth/callback");
        });
    }
}
