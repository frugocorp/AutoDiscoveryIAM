// -----------------------------------------------------------------------
// <copyright file="RefreshTokenGracePeriodIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Ventana de gracia del refresh token (<c>RefreshTokenSettings:ReuseGracePeriodSeconds</c>): reusar
/// un token recién rotado devuelve el mismo par en vez de revocar la sesión.
/// </summary>
[Collection("GlobalTestSetup")]
public class RefreshTokenGracePeriodIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string TestPassword = "TestPass123*";
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  private WebApplicationFactory<Program> CreateGraceFactory() => factory.WithWebHostBuilder(builder =>
    builder.ConfigureServices(services =>
      services.PostConfigure<RefreshTokenSettings>(settings => settings.ReuseGracePeriodSeconds = 30)));

  private static async Task<string> LoginAsync(WebApplicationFactory<Program> appFactory, CancellationToken cancellationToken)
  {
    var email = $"grace-{Guid.NewGuid():N}@example.com";
    using (var scope = appFactory.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
      var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
      dbContext.Users.Add(new User
      {
        Username = $"grace-{Guid.NewGuid():N}",
        Email = email,
        PasswordHash = passwordHasher.HashPassword(TestPassword),
        IsActive = true,
        IsEmailVerified = true,
        Status = "active",
      });
      await dbContext.SaveChangesAsync(cancellationToken);
    }

    using var client = appFactory.CreateClient();
    var login = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, TestPassword), cancellationToken);
    login.EnsureSuccessStatusCode();
    var body = await login.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
    return body!.Data!.RefreshToken!;
  }

  [Fact(DisplayName = "Ventana de gracia: el reintento con el token recién rotado recibe el mismo par y la sesión sigue viva")]
  public async Task RetryWithRotatedToken_ReturnsSamePair_AndSessionStaysAlive()
  {
    using var graceFactory = CreateGraceFactory();
    var original = await LoginAsync(graceFactory, _cancellationToken);
    using var client = graceFactory.CreateClient();

    var first = await client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(original), _cancellationToken);
    first.EnsureSuccessStatusCode();
    var firstPair = (await first.Content.ReadFromJsonAsync<RefreshTokenResponse>(_cancellationToken))!.Data!;

    // La respuesta "se perdió": el cliente reintenta con el token original.
    var retry = await client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(original), _cancellationToken);
    retry.StatusCode.Should().Be(HttpStatusCode.OK);
    var retryPair = (await retry.Content.ReadFromJsonAsync<RefreshTokenResponse>(_cancellationToken))!.Data!;
    retryPair.RefreshToken.Should().Be(firstPair.RefreshToken);
    retryPair.AccessToken.Should().Be(firstPair.AccessToken);

    // El token nuevo sigue sirviendo: no hubo revocación de la sesión.
    var next = await client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(firstPair.RefreshToken), _cancellationToken);
    next.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact(DisplayName = "Ventana de gracia: el token recién rotado usado desde otro cliente revoca toda la sesión")]
  public async Task RotatedTokenFromAnotherClient_RevokesSession()
  {
    using var graceFactory = CreateGraceFactory();
    var original = await LoginAsync(graceFactory, _cancellationToken);
    using var client = graceFactory.CreateClient();
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Navegador/1.0");

    var first = await client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(original), _cancellationToken);
    first.EnsureSuccessStatusCode();
    var firstPair = (await first.Content.ReadFromJsonAsync<RefreshTokenResponse>(_cancellationToken))!.Data!;

    // Alguien más copió el token original y lo usa dentro de la ventana, desde otro cliente.
    using var attacker = graceFactory.CreateClient();
    attacker.DefaultRequestHeaders.UserAgent.ParseAdd("OtroBot/2.0");
    var stolen = await attacker.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(original), _cancellationToken);
    stolen.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    // Se cortó toda la sesión: tampoco sirve el token que recibió el dueño.
    var next = await client.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(firstPair.RefreshToken), _cancellationToken);
    next.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact(DisplayName = "Ventana de gracia: dos refresh simultáneos con el mismo token reciben el mismo par")]
  public async Task ConcurrentRefresh_BothSucceedWithSamePair()
  {
    using var graceFactory = CreateGraceFactory();
    var original = await LoginAsync(graceFactory, _cancellationToken);
    using var clientA = graceFactory.CreateClient();
    using var clientB = graceFactory.CreateClient();

    var responses = await Task.WhenAll(
      clientA.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(original), _cancellationToken),
      clientB.PostAsJsonAsync("/auth/refresh", new RefreshTokenRequestDto(original), _cancellationToken));

    responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(2);
    var pairs = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<RefreshTokenResponse>(_cancellationToken)));
    pairs[0]!.Data!.RefreshToken.Should().Be(pairs[1]!.Data!.RefreshToken);
    foreach (var response in responses)
    {
      response.Dispose();
    }
  }
}
