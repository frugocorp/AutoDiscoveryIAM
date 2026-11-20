// -----------------------------------------------------------------------
// <copyright file="UpdateMyProfileIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.UpdateMyProfile;

/// <summary>
/// Issue #076: valida que un usuario autenticado pueda actualizar sus propios datos (autoservicio)
/// contra el servidor y la BD reales. Usa un usuario dedicado creado directamente en BD, no las
/// cuentas fijas compartidas por el resto de la suite (mismo patrón que #075).
/// </summary>
[Collection("GlobalTestSetup")]
public class UpdateMyProfileIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string TestPassword = "TestPass123*";
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  private async Task<(string Email, Guid PublicId)> CreateActiveUserAsync()
  {
    using var scope = factory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    var email = $"myprofile-{Guid.NewGuid():N}@example.com";
    var user = new User
    {
      Username = $"myprofile-{Guid.NewGuid():N}",
      Email = email,
      PasswordHash = passwordHasher.HashPassword(TestPassword),
      IsActive = true,
      IsEmailVerified = true,
      Status = "active",
    };
    dbContext.Users.Add(user);
    await dbContext.SaveChangesAsync(_cancellationToken);

    return (email, user.PublicId);
  }

  private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
  {
    var client = factory.CreateClient();
    var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, password), _cancellationToken);
    loginResponse.EnsureSuccessStatusCode();
    var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginContent!.Data!.Token);
    return client;
  }

  [Fact]
  public async Task UpdateMyProfile_WithAuthenticatedUser_ShouldUpdateOwnDataOnly()
  {
    var (email, publicId) = await CreateActiveUserAsync();
    using var userClient = await CreateAuthenticatedClientAsync(email, TestPassword);

    var response = await userClient.PutAsJsonAsync(
        "/users/me",
        new UpdateMyProfileRequestDto("+521111111111"),
        _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<UpdateMyProfileResponse>(cancellationToken: _cancellationToken);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data!.PublicId.Should().Be(publicId);
    payload.Data.PhoneNumber.Should().Be("+521111111111");

    using var scope = factory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var persisted = await dbContext.Users.AsNoTracking().FirstAsync(u => u.PublicId == publicId, _cancellationToken);
    persisted.PhoneNumber.Should().Be("+521111111111");
  }

  [Fact]
  public async Task UpdateMyProfile_WhenNotAuthenticated_ShouldReturnUnauthorized()
  {
    var anonClient = factory.CreateClient();

    var response = await anonClient.PutAsJsonAsync(
        "/users/me",
        new UpdateMyProfileRequestDto(null),
        _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }
}
