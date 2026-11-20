using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.PasswordManagement;

[Collection("GlobalTestSetup")]
public class PasswordManagementIntegrationTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string UserPassword = "UserPass123!";
  private const string UserPasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$EAbZ0dVvIvPEE2cf4RvOBg$aekIoccqSoBj4bPhbY2UmwSJ4nRki6TwOq3nhowvVXc";

  private readonly HttpClient _client = factory.CreateClient();
  private readonly CustomWebApplicationFactory<Program> _factory = factory;
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token;

  [Fact]
  public async Task ChangeMyPassword_WhenNoToken_ReturnsUnauthorized()
  {
    _client.RemoveAuthentication();

    var response = await _client.PostAsJsonAsync(
      "/users/me/password",
      new ChangePasswordRequestDto(UserPassword, "NewPass456!"),
      _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task ChangeMyPassword_InvalidNewPassword_ReturnsBadRequest()
  {
    var email = $"change-pwd-invalid-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);
    await AuthenticateAsync(email);

    var response = await _client.PostAsJsonAsync(
      "/users/me/password",
      new ChangePasswordRequestDto(UserPassword, "short"),
      _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task ChangeMyPassword_WrongCurrentPassword_ReturnsUnauthorized()
  {
    var email = $"change-pwd-wrong-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);
    await AuthenticateAsync(email);

    var response = await _client.PostAsJsonAsync(
      "/users/me/password",
      new ChangePasswordRequestDto("WrongPass999!", "NewPass456!"),
      _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task ChangeMyPassword_ValidCurrentPassword_UpdatesPasswordAndRevokesRefreshToken()
  {
    var email = $"change-pwd-ok-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);
    var oldRefreshToken = await LoginAndGetRefreshTokenAsync(email, UserPassword);

    var response = await _client.PostAsJsonAsync(
      "/users/me/password",
      new ChangePasswordRequestDto(UserPassword, "NewPass456!"),
      _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var refreshResponse = await _client.PostAsJsonAsync(
      "/auth/refresh",
      new RefreshTokenRequestDto(oldRefreshToken),
      _cancellationToken);
    refreshResponse.StatusCode.Should().NotBe(HttpStatusCode.OK);

    _client.RemoveAuthentication();
    var newLoginResponse = await _client.PostAsJsonAsync(
      "/auth/login",
      new LoginRequestDto(email, "NewPass456!"),
      _cancellationToken);
    newLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact]
  public async Task ChangeMyPassword_ShouldImmediatelyInvalidateAccessTokenFromOtherDevice()
  {
    // Escenario: PC1 tiene una sesión abierta (access token vigente); PC2 cambia la contraseña
    // (p. ej. porque le robaron el acceso). El access token de PC1 debe dejar de servir de
    // inmediato, sin esperar a que expire por su cuenta.
    var email = $"change-pwd-multi-device-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);

    using var pc1Client = _factory.CreateClient();
    var pc1Token = await pc1Client.GetAuthTokenAsync(email, UserPassword, _cancellationToken);
    pc1Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pc1Token);

    using var pc2Client = _factory.CreateClient();
    var pc2Token = await pc2Client.GetAuthTokenAsync(email, UserPassword, _cancellationToken);
    pc2Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pc2Token);

    // Antes del cambio, ambos tokens funcionan.
    var pc1BeforeChange = await pc1Client.GetAsync("/users/me/permissions", _cancellationToken);
    pc1BeforeChange.StatusCode.Should().Be(HttpStatusCode.OK);

    var changeResponse = await pc2Client.PostAsJsonAsync(
      "/users/me/password",
      new ChangePasswordRequestDto(UserPassword, "NewPass456!"),
      _cancellationToken);
    changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // Después del cambio, el token de PC1 (nunca usado para el cambio) ya no debe ser válido.
    var pc1AfterChange = await pc1Client.GetAsync("/users/me/permissions", _cancellationToken);
    pc1AfterChange.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task RequestPasswordReset_UnknownEmail_ReturnsNeutralOk()
  {
    var response = await _client.PostAsJsonAsync(
      "/users/password/reset/request",
      new RequestPasswordResetRequestDto($"unknown-{Guid.NewGuid():N}@example.com"),
      _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact]
  public async Task RequestPasswordReset_InvalidEmailFormat_ReturnsBadRequest()
  {
    var response = await _client.PostAsJsonAsync(
      "/users/password/reset/request",
      new RequestPasswordResetRequestDto("not-an-email"),
      _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
  }

  [Fact]
  public async Task RequestPasswordReset_KnownActiveEmail_GeneratesResetTokenInDatabase()
  {
    using var stubFactory = CreateFactoryWithStubEmailService();
    var stubClient = stubFactory.CreateClient();

    var email = $"reset-request-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);

    var response = await stubClient.PostAsJsonAsync(
      "/users/password/reset/request",
      new RequestPasswordResetRequestDto(email),
      _cancellationToken);
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);

    user.PasswordResetToken.Should().NotBeNullOrWhiteSpace();
    user.PasswordResetTokenExpiration.Should().NotBeNull();

    // En la base de datos queda solo el hash del token que viajó por correo.
    var sentToken = StubPasswordResetEmailService.SentTokens[email];
    user.PasswordResetToken.Should().NotBe(sentToken);
    user.PasswordResetToken.Should().Be(OneTimeTokenHasher.Hash(sentToken));
  }

  [Fact]
  public async Task RequestPasswordReset_InactiveUser_DoesNotGenerateResetToken()
  {
    using var stubFactory = CreateFactoryWithStubEmailService();
    var stubClient = stubFactory.CreateClient();

    var email = $"reset-inactive-{Guid.NewGuid():N}@example.com";
    await CreateInactiveUserAsync(email);

    var response = await stubClient.PostAsJsonAsync(
      "/users/password/reset/request",
      new RequestPasswordResetRequestDto(email),
      _cancellationToken);
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);

    user.PasswordResetToken.Should().BeNull();
  }

  [Fact]
  public async Task ConfirmPasswordReset_WithInvalidToken_ReturnsConflict()
  {
    var response = await _client.PostAsJsonAsync(
      "/users/password/reset/confirm",
      new ConfirmPasswordResetRequestDto("invalid-token", "NewPass456!"),
      _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task ConfirmPasswordReset_WithExpiredToken_ReturnsConflict()
  {
    var email = $"reset-expired-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);
    var token = $"expired-token-{Guid.NewGuid():N}";
    await SetResetTokenAsync(email, token, expiresInMinutes: -5);

    var response = await _client.PostAsJsonAsync(
      "/users/password/reset/confirm",
      new ConfirmPasswordResetRequestDto(token, "NewPass456!"),
      _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task ConfirmPasswordReset_WithValidToken_UpdatesPasswordAndRevokesRefreshToken()
  {
    var email = $"reset-confirm-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);
    var oldRefreshToken = await LoginAndGetRefreshTokenAsync(email, UserPassword);

    var token = $"reset-token-{Guid.NewGuid():N}";
    await SetResetTokenAsync(email, token, expiresInMinutes: 30);

    var response = await _client.PostAsJsonAsync(
      "/users/password/reset/confirm",
      new ConfirmPasswordResetRequestDto(token, "NewPass456!"),
      _cancellationToken);
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var secondAttempt = await _client.PostAsJsonAsync(
      "/users/password/reset/confirm",
      new ConfirmPasswordResetRequestDto(token, "AnotherPass789!"),
      _cancellationToken);
    secondAttempt.StatusCode.Should().Be(HttpStatusCode.Conflict);

    var refreshResponse = await _client.PostAsJsonAsync(
      "/auth/refresh",
      new RefreshTokenRequestDto(oldRefreshToken),
      _cancellationToken);
    refreshResponse.StatusCode.Should().NotBe(HttpStatusCode.OK);

    _client.RemoveAuthentication();
    var newLoginResponse = await _client.PostAsJsonAsync(
      "/auth/login",
      new LoginRequestDto(email, "NewPass456!"),
      _cancellationToken);
    newLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact]
  public async Task ConfirmPasswordReset_WithValidToken_ResetsLockoutState()
  {
    var email = $"reset-unlock-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);

    var token = $"reset-token-{Guid.NewGuid():N}";
    await SetResetTokenAsync(email, token, expiresInMinutes: 30);
    await SetLockoutStateAsync(email, failedLoginAttempts: 4, lockoutEnd: TimeHelpers.GetCurrentMexicoTime().AddMinutes(30));

    var response = await _client.PostAsJsonAsync(
      "/users/password/reset/confirm",
      new ConfirmPasswordResetRequestDto(token, "NewPass456!"),
      _cancellationToken);
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);

    user.FailedLoginAttempts.Should().Be(0);
    user.LockoutEnd.Should().BeNull();
  }

  private async Task SetLockoutStateAsync(string email, int failedLoginAttempts, DateTime lockoutEnd)
  {
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);
    user.FailedLoginAttempts = failedLoginAttempts;
    user.LockoutEnd = lockoutEnd;
    await db.SaveChangesAsync(_cancellationToken);
  }

  private WebApplicationFactory<Program> CreateFactoryWithStubEmailService()
    => _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureServices(services =>
      {
        services.RemoveAll<IPasswordResetEmailService>();
        services.AddScoped<IPasswordResetEmailService, StubPasswordResetEmailService>();
      });
    });

  private async Task CreateActiveUserAsync(string email)
  {
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

    db.Users.Add(new User
    {
      PublicId = Guid.NewGuid(),
      Username = $"pwd-mgmt-{Guid.NewGuid():N}",
      Email = email,
      PasswordHash = UserPasswordHash,
      Status = "active",
      IsActive = true,
      IsDeleted = false,
    });

    await db.SaveChangesAsync(_cancellationToken);
  }

  private async Task CreateInactiveUserAsync(string email)
  {
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

    db.Users.Add(new User
    {
      PublicId = Guid.NewGuid(),
      Username = $"pwd-mgmt-inactive-{Guid.NewGuid():N}",
      Email = email,
      PasswordHash = UserPasswordHash,
      Status = "pending",
      IsActive = false,
      IsDeleted = false,
    });

    await db.SaveChangesAsync(_cancellationToken);
  }

  private async Task SetResetTokenAsync(string email, string token, int expiresInMinutes)
  {
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);
    user.PasswordResetToken = OneTimeTokenHasher.Hash(token);
    user.PasswordResetTokenExpiration = TimeHelpers.GetCurrentMexicoTime().AddMinutes(expiresInMinutes);
    await db.SaveChangesAsync(_cancellationToken);
  }

  private async Task<string> LoginAndGetRefreshTokenAsync(string email, string password)
  {
    var response = await _client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, password), _cancellationToken);
    response.EnsureSuccessStatusCode();

    var content = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", content!.Data!.Token);

    return content.Data.RefreshToken!;
  }

  private async Task AuthenticateAsync(string email)
  {
    var token = await _client.GetAuthTokenAsync(email, UserPassword, _cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
  }

  private sealed class StubPasswordResetEmailService : IPasswordResetEmailService
  {
    public static ConcurrentDictionary<string, string> SentTokens { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Task<OperationResult<bool>> SendPasswordResetEmailAsync(User user, string token, CancellationToken cancellationToken, bool fakeMail = false)
    {
      SentTokens[user.Email] = token;
      return Task.FromResult(OperationResult.Ok(true));
    }
  }
}
