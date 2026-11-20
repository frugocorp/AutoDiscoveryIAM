using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OtpNet;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Commands.TwoFactor.VerifyTwoFactor;

[Collection("GlobalTestSetup")]
public class VerifyTwoFactorIntegrationTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string UserPassword = "TestPass123!";
  private const string UserPasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$OOwzesn4eOhertDDWp7BEQ$q3C9LZDkbhpIUeH8c1Sxi2ailXMeVnP3EEQ2rs3K7dU";

  private readonly CustomWebApplicationFactory<Program> _factory = factory;
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token;

  [Fact]
  public async Task LoginAndVerify_EmailMethod_FullFlow_ReturnsRealTokens()
  {
    using var stubFactory = CreateFactoryWithStubTwoFactorEmailService();
    var client = stubFactory.CreateClient();

    var email = $"verify2fa-email-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserWithEmailTwoFactorAsync(email);

    var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, UserPassword), _cancellationToken);
    loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
    loginContent!.Data!.RequiresTwoFactor.Should().BeTrue();
    loginContent.Data.Token.Should().BeNull();
    var challengeToken = loginContent.Data.TwoFactorChallengeToken;
    challengeToken.Should().NotBeNullOrWhiteSpace();

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);
    var code = user.TwoFactorCode;
    code.Should().NotBeNullOrWhiteSpace();

    var wrongVerify = await client.PostAsJsonAsync(
      "/auth/2fa/verify",
      new VerifyTwoFactorRequestDto(challengeToken!, "000000"),
      _cancellationToken);
    wrongVerify.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    var verifyResponse = await client.PostAsJsonAsync(
      "/auth/2fa/verify",
      new VerifyTwoFactorRequestDto(challengeToken!, code!),
      _cancellationToken);
    verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var verifyContent = await verifyResponse.Content.ReadFromJsonAsync<VerifyTwoFactorResponse>(cancellationToken: _cancellationToken);
    verifyContent!.Data!.Token.Should().NotBeNullOrWhiteSpace();
    verifyContent.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();

    var reusedVerify = await client.PostAsJsonAsync(
      "/auth/2fa/verify",
      new VerifyTwoFactorRequestDto(challengeToken!, code!),
      _cancellationToken);
    reusedVerify.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task LoginAndVerify_AuthenticatorMethod_FullFlow_ReturnsRealTokens()
  {
    var email = $"verify2fa-authenticator-{Guid.NewGuid():N}@example.com";
    var secret = await CreateActiveUserWithAuthenticatorTwoFactorAsync(email);
    var client = _factory.CreateClient();

    var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, UserPassword), _cancellationToken);
    loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
    loginContent!.Data!.RequiresTwoFactor.Should().BeTrue();
    var challengeToken = loginContent.Data.TwoFactorChallengeToken;

    var code = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();

    var verifyResponse = await client.PostAsJsonAsync(
      "/auth/2fa/verify",
      new VerifyTwoFactorRequestDto(challengeToken!, code),
      _cancellationToken);
    verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var verifyContent = await verifyResponse.Content.ReadFromJsonAsync<VerifyTwoFactorResponse>(cancellationToken: _cancellationToken);
    verifyContent!.Data!.Token.Should().NotBeNullOrWhiteSpace();
  }

  [Fact(DisplayName = "Authenticator: un código ya usado no sirve para un segundo login, aunque siga dentro de su ventana")]
  public async Task LoginAndVerify_AuthenticatorCodeReused_ReturnsUnauthorized()
  {
    var email = $"verify2fa-replay-{Guid.NewGuid():N}@example.com";
    var secret = await CreateActiveUserWithAuthenticatorTwoFactorAsync(email);
    var client = _factory.CreateClient();
    var code = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();

    var firstVerify = await client.PostAsJsonAsync(
      "/auth/2fa/verify",
      new VerifyTwoFactorRequestDto(await StartChallengeAsync(client, email), code),
      _cancellationToken);
    firstVerify.StatusCode.Should().Be(HttpStatusCode.OK);

    var replayedVerify = await client.PostAsJsonAsync(
      "/auth/2fa/verify",
      new VerifyTwoFactorRequestDto(await StartChallengeAsync(client, email), code),
      _cancellationToken);
    replayedVerify.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "quien observe un código no debe poder reutilizarlo");
  }

  [Fact(DisplayName = "Authenticator: el secreto se guarda cifrado en la base de datos")]
  public async Task CreateUser_AuthenticatorSecret_IsStoredEncrypted()
  {
    var email = $"verify2fa-encrypted-{Guid.NewGuid():N}@example.com";
    var secret = await CreateActiveUserWithAuthenticatorTwoFactorAsync(email);

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var stored = await db.Users.Where(u => u.Email == email).Select(u => u.TwoFactorSecret).FirstAsync(_cancellationToken);

    stored.Should().NotBeNullOrWhiteSpace();
    stored.Should().NotContain(secret);
  }

  private async Task<string> StartChallengeAsync(HttpClient client, string email)
  {
    var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, UserPassword), _cancellationToken);
    loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
    return loginContent!.Data!.TwoFactorChallengeToken!;
  }

  [Fact]
  public async Task Verify_WithUnknownChallengeToken_ReturnsConflict()
  {
    var client = _factory.CreateClient();

    var response = await client.PostAsJsonAsync(
      "/auth/2fa/verify",
      new VerifyTwoFactorRequestDto("unknown-challenge-token", "123456"),
      _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  private WebApplicationFactory<Program> CreateFactoryWithStubTwoFactorEmailService()
    => _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureServices(services =>
      {
        services.RemoveAll<ITwoFactorEmailService>();
        services.AddScoped<ITwoFactorEmailService, StubTwoFactorEmailService>();
      });
    });

  private async Task CreateActiveUserWithEmailTwoFactorAsync(string email)
  {
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

    db.Users.Add(new User
    {
      PublicId = Guid.NewGuid(),
      Username = $"verify2fa-{Guid.NewGuid():N}",
      Email = email,
      PasswordHash = UserPasswordHash,
      Status = "active",
      IsActive = true,
      IsDeleted = false,
      TwoFactorEnabled = true,
      TwoFactorMethod = FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs.TwoFactorMethodCatalog.Email,
    });

    await db.SaveChangesAsync(_cancellationToken);
  }

  private async Task<string> CreateActiveUserWithAuthenticatorTwoFactorAsync(string email)
  {
    var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

    db.Users.Add(new User
    {
      PublicId = Guid.NewGuid(),
      Username = $"verify2fa-{Guid.NewGuid():N}",
      Email = email,
      PasswordHash = UserPasswordHash,
      Status = "active",
      IsActive = true,
      IsDeleted = false,
      TwoFactorEnabled = true,
      TwoFactorMethod = FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs.TwoFactorMethodCatalog.Authenticator,
      TwoFactorSecret = scope.ServiceProvider.GetRequiredService<ITotpService>().ProtectSecret(secret),
    });

    await db.SaveChangesAsync(_cancellationToken);
    return secret;
  }

  private sealed class StubTwoFactorEmailService : ITwoFactorEmailService
  {
    public Task<OperationResult<bool>> SendCodeAsync(User user, string code, CancellationToken cancellationToken, bool fakeMail = false)
      => Task.FromResult(OperationResult.Ok(true));
  }
}
