// -----------------------------------------------------------------------
// <copyright file="RefreshTokenConcurrencyIntegrationTests.cs" company="FRUGO Corp.">
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
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Issue #075: a diferencia de <see cref="RefreshTokenIntegrationTests.RefreshToken_WithUsedToken_ReturnsUnauthorized"/>
/// (secuencial: espera la primera respuesta antes de disparar la segunda), esta prueba dispara
/// dos requests /auth/refresh REALMENTE simultáneos con el mismo refresh token (<c>Task.WhenAll</c>)
/// contra el servidor y la base de datos reales, para confirmar que la detección de reuso resiste
/// una carrera genuina y no solo el caso secuencial.
/// </summary>
[Collection("GlobalTestSetup")]
public class RefreshTokenConcurrencyIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string TestPassword = "TestPass123*";
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  [Fact]
  public async Task RefreshToken_WithSameTokenUsedConcurrently_ShouldSucceedExactlyOnce()
  {
    using var scope = factory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    var email = $"concurrency-{Guid.NewGuid():N}@example.com";
    var user = new User
    {
      Username = $"concurrency-{Guid.NewGuid():N}",
      Email = email,
      PasswordHash = passwordHasher.HashPassword(TestPassword),
      IsActive = true,
      IsEmailVerified = true,
      Status = "active",
    };
    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync(_cancellationToken);

    using var loginClient = factory.CreateClient();
    var loginResponse = await loginClient.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, TestPassword), _cancellationToken);
    loginResponse.EnsureSuccessStatusCode();
    var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
    var refreshToken = loginContent!.Data!.RefreshToken!;

    using var clientA = factory.CreateClient();
    using var clientB = factory.CreateClient();
    var requestBody = new RefreshTokenRequestDto(refreshToken);

    var taskA = clientA.PostAsJsonAsync("/auth/refresh", requestBody, _cancellationToken);
    var taskB = clientB.PostAsJsonAsync("/auth/refresh", requestBody, _cancellationToken);
    var responses = await Task.WhenAll(taskA, taskB);

    var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
    var unauthorizedCount = responses.Count(r => r.StatusCode == HttpStatusCode.Unauthorized);

    successCount.Should().Be(1, "solo una de las dos peticiones concurrentes con el mismo refresh token debe tener exito");
    unauthorizedCount.Should().Be(1, "la otra peticion concurrente debe ser rechazada, nunca las dos exitosas");

    foreach (var response in responses)
    {
      response.Dispose();
    }
  }
}
