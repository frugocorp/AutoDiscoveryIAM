using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.Contracts;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AwesomeAssertions;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Commands.Login;

[Collection("GlobalTestSetup")]
public class GoogleLoginCallbackIntegrationTests(GoogleAuthTestWebApplicationFactory factory)
  : IClassFixture<GoogleAuthTestWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly GoogleAuthTestWebApplicationFactory _factory = factory;
    private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    [Fact]
    public async Task GoogleCallback_WithStateInvalid_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/auth/google/callback?code=active-code&state=wrong-state", _cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        payload.Error.Should().Contain("State invalido");
    }

    [Fact]
    public async Task GoogleCallback_WithPendingUser_ReturnsUnauthorized()
    {
        await SeedUserWithGoogleLinkAsync("sub-pending", "pending-google-test@example.com", isActive: false);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/google/callback?code=pending-code&state=pending-state");
        request.Headers.Add("Cookie", "google_oauth_state=pending-state");

        var response = await _client.SendAsync(request, _cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeFalse();
        payload.Error.Should().Contain("pendiente de activacion o inactiva");
    }

    [Fact]
    public async Task GoogleCallback_WithActiveLinkedUser_ReturnsToken()
    {
        await SeedUserWithGoogleLinkAsync("sub-active", "active-google-test@example.com", isActive: true);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/google/callback?code=active-code&state=active-state");
        request.Headers.Add("Cookie", "google_oauth_state=active-state");

        var response = await _client.SendAsync(request, _cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
        payload.Should().NotBeNull();
        payload!.Success.Should().BeTrue();
        payload.Data.Should().NotBeNull();
        payload.Data!.Token.Should().NotBeNullOrWhiteSpace();
        payload.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GoogleLink_WithAuthenticatedUser_CreatesExternalLink()
    {
        await CleanupGoogleLinksForEmailAsync("admin@example.com");

        var token = await _client.GetAuthTokenAsync("admin@example.com", "AdminPass123!", _cancellationToken);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/auth/google/link", new { code = "link-code" }, _cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var admin = await db.Users.FirstAsync(x => x.Email == "admin@example.com", _cancellationToken);
        // Issue #075: ExternalLogin.ProviderId ahora guarda el hash del sub, no el valor en claro.
        var hashedProviderId = ExternalProviderIdHasher.Hash("sub-link");
        var link = await db.ExternalLogins
          .FirstOrDefaultAsync(x => x.UserId == admin.Id && x.Provider == ExternalLoginProviders.Google && x.ProviderId == hashedProviderId, _cancellationToken);

        link.Should().NotBeNull();
    }

    [Fact]
    public async Task GoogleUnlink_WithAuthenticatedUser_RemovesExternalLink()
    {
        await CleanupGoogleLinksForEmailAsync("admin@example.com");

        var token = await _client.GetAuthTokenAsync("admin@example.com", "AdminPass123!", _cancellationToken);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var linkResponse = await _client.PostAsJsonAsync("/auth/google/link", new { code = "unlink-code" }, _cancellationToken);
        linkResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.DeleteAsync("/auth/google/unlink", _cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var admin = await db.Users.FirstAsync(x => x.Email == "admin@example.com", _cancellationToken);
        // Issue #075: ExternalLogin ahora tiene HasQueryFilter(!IsDeleted) — el vinculo soft-deleted
        // ya no aparece en una consulta normal, por eso se usa IgnoreQueryFilters() para verificarlo.
        // ProviderId ahora guarda el hash del sub, no el valor en claro.
        var hashedProviderId = ExternalProviderIdHasher.Hash("sub-unlink");
        var link = await db.ExternalLogins
          .IgnoreQueryFilters()
          .FirstOrDefaultAsync(x => x.UserId == admin.Id && x.Provider == ExternalLoginProviders.Google && x.ProviderId == hashedProviderId, _cancellationToken);

        link.Should().NotBeNull();
        link!.IsDeleted.Should().BeTrue();
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

        // Issue #075: ExternalLogin.ProviderId ahora guarda el hash del sub, nunca el valor en claro —
        // el seed debe hashear igual que ExternalLoginOrchestrator para que el lookup real coincida.
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

    private async Task CleanupGoogleLinksForEmailAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email, _cancellationToken);
        if (user == null)
        {
            return;
        }

        // Issue #075 (hallazgo, no introducido por este issue): AutoDiscoveryIAMDbContext.ApplyAuditInfoByState
        // convierte SIEMPRE un EntityState.Deleted en soft-delete (IsDeleted=true), nunca un DELETE real —
        // por eso RemoveRange()+SaveChangesAsync() nunca liberaba la fila para el indice unico
        // IX_ExternalLogins_UserId_Provider, dejando huerfanos que rompian la siguiente corrida contra una
        // base persistente. ExecuteDeleteAsync() emite un DELETE real, sin pasar por SaveChanges.
        await db.ExternalLogins.IgnoreQueryFilters()
          .Where(x => x.UserId == user.Id && x.Provider == ExternalLoginProviders.Google)
          .ExecuteDeleteAsync(_cancellationToken);
    }
}

public class GoogleAuthTestWebApplicationFactory : CustomWebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            var providerDescriptors = services
          .Where(x => x.ServiceType == typeof(IExternalLoginProvider))
          .ToList();

            foreach (var descriptor in providerDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IExternalLoginProvider, FakeGoogleProvider>();
        });
    }
}

internal sealed class FakeGoogleProvider : IExternalLoginProvider
{
    public string ProviderName => ExternalLoginProviders.Google;

    public string BuildAuthorizationUrl(string state)
    {
        return $"https://fake.google/authorize?state={state}";
    }

    public Task<OperationResult<ExternalIdentityProfile>> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        var profile = code switch
        {
            "active-code" => new ExternalIdentityProfile(
              ExternalLoginProviders.Google,
              "sub-active",
              "active-google-test@example.com",
              true),
            "pending-code" => new ExternalIdentityProfile(
              ExternalLoginProviders.Google,
              "sub-pending",
              "pending-google-test@example.com",
              true),
            "link-code" => new ExternalIdentityProfile(
              ExternalLoginProviders.Google,
              "sub-link",
              "admin@example.com",
              true),
            "unlink-code" => new ExternalIdentityProfile(
              ExternalLoginProviders.Google,
              "sub-unlink",
              "admin@example.com",
              true),
            _ => null,
        };

        if (profile == null)
        {
            return Task.FromResult(OperationResult.Fail<ExternalIdentityProfile>(
              ErrorInfo.Status424Dependency,
              "Codigo de prueba no soportado por FakeGoogleProvider."));
        }

        return Task.FromResult(OperationResult.Ok(profile));
    }
}
