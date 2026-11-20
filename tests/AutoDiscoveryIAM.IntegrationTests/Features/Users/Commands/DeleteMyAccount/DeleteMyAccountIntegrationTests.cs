// -----------------------------------------------------------------------
// <copyright file="DeleteMyAccountIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.DeleteMyAccount;

/// <summary>
/// Issue #076: valida que un usuario autenticado pueda eliminar (soft-delete) su propia cuenta
/// contra el servidor y la BD reales. Usa un usuario dedicado creado directamente en BD.
/// </summary>
[Collection("GlobalTestSetup")]
public class DeleteMyAccountIntegrationTests(CustomWebApplicationFactory<Program> factory)
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
  public async Task DeleteMyAccount_WithAuthenticatedUser_ShouldSoftDeleteAndInvalidateFutureRequests()
  {
    var (email, publicId) = await CreateActiveUserAsync();
    using var userClient = await CreateAuthenticatedClientAsync(email, TestPassword);

    using var request = new HttpRequestMessage(HttpMethod.Delete, "/users/me")
    {
      Content = JsonContent.Create(new DeleteMyAccountRequestDto(TestPassword)),
    };
    var response = await userClient.SendAsync(request, _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var payload = await response.Content.ReadFromJsonAsync<DeleteMyAccountResponse>(cancellationToken: _cancellationToken);
    payload.Should().NotBeNull();
    payload!.Success.Should().BeTrue();
    payload.Data!.PublicId.Should().Be(publicId);

    using (var scope = factory.Services.CreateScope())
    {
      var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
      var persisted = await dbContext.Users.IgnoreQueryFilters().AsNoTracking().FirstAsync(u => u.PublicId == publicId, _cancellationToken);
      persisted.IsDeleted.Should().BeTrue();
    }

    // El mismo token ya no debe resolver un usuario valido (soft-deleted queda fuera del query
    // filter) en cualquier endpoint que SI pase por la resolucion de usuario del middleware
    // (a diferencia de /users/me/permissions, marcado [PermissionsNotRequired]).
    var followUpResponse = await userClient.GetAsync("/users?pageSize=1", _cancellationToken);
    followUpResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Theory(DisplayName = "DELETE /users/me sin la contraseña actual correcta no elimina la cuenta")]
  [InlineData(null)]
  [InlineData("WrongPass123*")]
  public async Task DeleteMyAccount_WithoutCorrectPassword_ShouldNotDelete(string? password)
  {
    var (email, publicId) = await CreateActiveUserAsync();
    using var userClient = await CreateAuthenticatedClientAsync(email, TestPassword);

    using var request = new HttpRequestMessage(HttpMethod.Delete, "/users/me");
    if (password is not null)
    {
      request.Content = JsonContent.Create(new DeleteMyAccountRequestDto(password));
    }

    var response = await userClient.SendAsync(request, _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    using var scope = factory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var persisted = await dbContext.Users.IgnoreQueryFilters().AsNoTracking().FirstAsync(u => u.PublicId == publicId, _cancellationToken);
    persisted.IsDeleted.Should().BeFalse("un access token robado no alcanza para borrar la cuenta");
  }

  [Fact]
  public async Task DeleteMyAccount_WhenNotAuthenticated_ShouldReturnUnauthorized()
  {
    var anonClient = factory.CreateClient();

    var response = await anonClient.DeleteAsync("/users/me", _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }
}
