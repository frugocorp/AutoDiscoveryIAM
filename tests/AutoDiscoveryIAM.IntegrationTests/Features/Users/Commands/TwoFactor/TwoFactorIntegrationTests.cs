using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.TwoFactor;

[Collection("GlobalTestSetup")]
public class TwoFactorIntegrationTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string UserPassword = "TestPass123!";
  private const string UserPasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$OOwzesn4eOhertDDWp7BEQ$q3C9LZDkbhpIUeH8c1Sxi2ailXMeVnP3EEQ2rs3K7dU";

  private readonly HttpClient _client = factory.CreateClient();
  private readonly CustomWebApplicationFactory<Program> _factory = factory;
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token;

  [Fact]
  public async Task EnableEmail_WrongPassword_ReturnsUnauthorized()
  {
    var email = $"twofa-enable-wrong-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);
    await AuthenticateAsync(email);

    var response = await _client.PostAsJsonAsync(
      "/users/me/2fa/email/enable",
      new EnableEmailTwoFactorRequestDto("WrongPassword999!"),
      _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task EnableEmail_CorrectPassword_ActivatesEmailMethod()
  {
    var email = $"twofa-enable-ok-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);
    await AuthenticateAsync(email);

    var response = await _client.PostAsJsonAsync(
      "/users/me/2fa/email/enable",
      new EnableEmailTwoFactorRequestDto(UserPassword),
      _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);

    user.TwoFactorEnabled.Should().BeTrue();
    user.TwoFactorMethod.Should().Be(TwoFactorMethodCatalog.Email);
  }

  [Fact]
  public async Task EnrollAndConfirmAuthenticator_WithValidCode_ActivatesAuthenticator()
  {
    var email = $"twofa-authenticator-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);
    await AuthenticateAsync(email);

    var enrollResponse = await _client.PostAsJsonAsync(
      "/users/me/2fa/authenticator/enroll",
      new EnrollAuthenticatorRequestDto(UserPassword),
      _cancellationToken);
    enrollResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var enrollContent = await enrollResponse.Content.ReadFromJsonAsync<EnrollAuthenticatorResponse>(cancellationToken: _cancellationToken);
    enrollContent!.Data!.Secret.Should().NotBeNullOrWhiteSpace();
    enrollContent.Data.ProvisioningUri.Should().StartWith("otpauth://totp/");

    var validCode = new Totp(Base32Encoding.ToBytes(enrollContent.Data.Secret)).ComputeTotp();

    var confirmResponse = await _client.PostAsJsonAsync(
      "/users/me/2fa/authenticator/confirm",
      new ConfirmAuthenticatorRequestDto(validCode),
      _cancellationToken);
    confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);

    user.TwoFactorEnabled.Should().BeTrue();
    user.TwoFactorMethod.Should().Be(TwoFactorMethodCatalog.Authenticator);
    user.TwoFactorPendingSecret.Should().BeNull();
  }

  [Fact]
  public async Task ConfirmAuthenticator_WithInvalidCode_ReturnsUnauthorized()
  {
    var email = $"twofa-authenticator-badcode-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);
    await AuthenticateAsync(email);

    var enrollResponse = await _client.PostAsJsonAsync(
      "/users/me/2fa/authenticator/enroll",
      new EnrollAuthenticatorRequestDto(UserPassword),
      _cancellationToken);
    enrollResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var confirmResponse = await _client.PostAsJsonAsync(
      "/users/me/2fa/authenticator/confirm",
      new ConfirmAuthenticatorRequestDto("000000"),
      _cancellationToken);

    confirmResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Disable_CorrectPassword_ClearsTwoFactorState()
  {
    var email = $"twofa-disable-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);
    await AuthenticateAsync(email);

    await _client.PostAsJsonAsync("/users/me/2fa/email/enable", new EnableEmailTwoFactorRequestDto(UserPassword), _cancellationToken);

    var response = await _client.PostAsJsonAsync(
      "/users/me/2fa/disable",
      new DisableTwoFactorRequestDto(UserPassword),
      _cancellationToken);
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);

    user.TwoFactorEnabled.Should().BeFalse();
    user.TwoFactorMethod.Should().Be(TwoFactorMethodCatalog.None);
  }

  private async Task CreateActiveUserAsync(string email)
  {
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

    db.Users.Add(new User
    {
      PublicId = Guid.NewGuid(),
      Username = $"twofa-{Guid.NewGuid():N}",
      Email = email,
      PasswordHash = UserPasswordHash,
      Status = "active",
      IsActive = true,
      IsDeleted = false,
    });

    await db.SaveChangesAsync(_cancellationToken);
  }

  private async Task AuthenticateAsync(string email)
  {
    var token = await _client.GetAuthTokenAsync(email, UserPassword, _cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
  }
}
