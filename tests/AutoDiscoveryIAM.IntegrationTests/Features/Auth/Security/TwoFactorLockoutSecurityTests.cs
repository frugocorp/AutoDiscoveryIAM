// -----------------------------------------------------------------------
// <copyright file="TwoFactorLockoutSecurityTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Security;

/// <summary>
/// <c>VerifyTwoFactorHandler</c> ya implementa su propio límite de intentos
/// (<c>RegisterFailedAttemptAsync</c>/<see cref="FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.LockoutSettings"/>),
/// independiente del que aplica <c>LoginHandler</c> a la contraseña -- pero no existía ningún test
/// que lo confirmara de punta a punta. Este test agota el límite de intentos con códigos TOTP
/// incorrectos y confirma que, incluso enviando después el código CORRECTO, la verificación sigue
/// bloqueada: si no fuera así, un atacante podría intentar fuerza bruta indefinida sobre el
/// código de 6 dígitos.
/// </summary>
[Collection("GlobalTestSetup")]
[Trait("Category", "Security")]
public class TwoFactorLockoutSecurityTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string UserPassword = "TestPass123!";
  private const string UserPasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$OOwzesn4eOhertDDWp7BEQ$q3C9LZDkbhpIUeH8c1Sxi2ailXMeVnP3EEQ2rs3K7dU";
  private const int MaxFailedAttempts = 5;

  private readonly CustomWebApplicationFactory<Program> _factory = factory;
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  [Fact(DisplayName = "2FA: tras agotar el límite de intentos, ni siquiera el código correcto permite completar la verificación")]
  public async Task VerifyTwoFactor_AfterExhaustingAttempts_RejectsEvenTheCorrectCode()
  {
    var email = $"2fa-lockout-{Guid.NewGuid():N}@example.com";
    var secret = await CreateActiveUserWithAuthenticatorTwoFactorAsync(email);
    var client = _factory.CreateClient();

    var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, UserPassword), _cancellationToken);
    loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
    var challengeToken = loginContent!.Data!.TwoFactorChallengeToken!;

    var correctCode = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();

    for (var attempt = 1; attempt <= MaxFailedAttempts; attempt++)
    {
      var wrongAttempt = await client.PostAsJsonAsync(
        "/auth/2fa/verify",
        new VerifyTwoFactorRequestDto(challengeToken, "000000"),
        _cancellationToken);

      wrongAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized, $"intento {attempt} con código incorrecto debe fallar");
    }

    var afterLockoutResponse = await client.PostAsJsonAsync(
      "/auth/2fa/verify",
      new VerifyTwoFactorRequestDto(challengeToken, correctCode),
      _cancellationToken);

    var afterLockoutContent = await afterLockoutResponse.Content.ReadFromJsonAsync<VerifyTwoFactorResponse>(cancellationToken: _cancellationToken);

    afterLockoutContent!.Success.Should().BeFalse("la cuenta debe estar bloqueada tras agotar el límite de intentos, incluso con el código correcto");
    afterLockoutContent.Data.Should().BeNull("no debe emitirse ningún token mientras la cuenta esté bloqueada");
    afterLockoutContent.Error.Should().Contain("bloqueada");
  }

  [Fact(DisplayName = "2FA: repetir el login con la contraseña correcta no reinicia el contador de códigos fallidos")]
  public async Task VerifyTwoFactor_ReLoginBetweenFailedCodes_StillLocksOut()
  {
    var email = $"2fa-relogin-{Guid.NewGuid():N}@example.com";
    var secret = await CreateActiveUserWithAuthenticatorTwoFactorAsync(email);
    var client = _factory.CreateClient();
    var correctCode = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();
    string? lastChallengeToken = null;

    // Un atacante que ya conoce la contraseña alterna: login nuevo + un código fallido.
    for (var attempt = 1; attempt <= MaxFailedAttempts; attempt++)
    {
      var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, UserPassword), _cancellationToken);
      var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
      if (loginContent?.Data?.TwoFactorChallengeToken is { } token)
      {
        lastChallengeToken = token;
      }

      await client.PostAsJsonAsync(
        "/auth/2fa/verify",
        new VerifyTwoFactorRequestDto(lastChallengeToken!, "000000"),
        _cancellationToken);
    }

    var afterLockoutResponse = await client.PostAsJsonAsync(
      "/auth/2fa/verify",
      new VerifyTwoFactorRequestDto(lastChallengeToken!, correctCode),
      _cancellationToken);
    var afterLockoutContent = await afterLockoutResponse.Content.ReadFromJsonAsync<VerifyTwoFactorResponse>(cancellationToken: _cancellationToken);

    afterLockoutContent!.Success.Should().BeFalse("los logins intermedios no deben reiniciar el contador de intentos del segundo factor");
    afterLockoutContent.Data.Should().BeNull();
    afterLockoutContent.Error.Should().Contain("bloqueada");
  }

  private async Task<string> CreateActiveUserWithAuthenticatorTwoFactorAsync(string email)
  {
    var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

    db.Users.Add(new User
    {
      PublicId = Guid.NewGuid(),
      Username = $"2fa-lockout-{Guid.NewGuid():N}",
      Email = email,
      PasswordHash = UserPasswordHash,
      Status = "active",
      IsActive = true,
      IsDeleted = false,
      TwoFactorEnabled = true,
      TwoFactorMethod = TwoFactorMethodCatalog.Authenticator,
      TwoFactorSecret = scope.ServiceProvider.GetRequiredService<ITotpService>().ProtectSecret(secret),
    });

    await db.SaveChangesAsync(_cancellationToken);
    return secret;
  }
}
