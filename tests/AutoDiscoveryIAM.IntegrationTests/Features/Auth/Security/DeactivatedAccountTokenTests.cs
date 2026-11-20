// -----------------------------------------------------------------------
// <copyright file="DeactivatedAccountTokenTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Security;

/// <summary>
/// Los endpoints self-service (<c>[PermissionsNotRequired]</c> + <c>RequireAuthorization()</c>) no pasan
/// por el RBAC, que es donde se comprobaba que la cuenta siguiera activa. Estas pruebas confirman que
/// el access token de una cuenta desactivada o eliminada deja de valer también ahí, sin esperar a
/// que expire.
/// </summary>
[Collection("GlobalTestSetup")]
[Trait("Category", "Security")]
public class DeactivatedAccountTokenTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string UserPassword = "TestPass123!";
  private const string UserPasswordHash = "$argon2id$v=19$m=65536,t=3,p=1$OOwzesn4eOhertDDWp7BEQ$q3C9LZDkbhpIUeH8c1Sxi2ailXMeVnP3EEQ2rs3K7dU";

  private readonly CustomWebApplicationFactory<Program> _factory = factory;
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  [Theory(DisplayName = "Token de una cuenta desactivada o eliminada: rechazado en un endpoint self-service")]
  [InlineData(false)]
  [InlineData(true)]
  public async Task SelfServiceEndpoint_AfterAccountDeactivatedOrDeleted_Returns401(bool deleteInsteadOfDeactivate)
  {
    var email = $"deactivated-{Guid.NewGuid():N}@example.com";
    await CreateActiveUserAsync(email);
    var client = _factory.CreateClient();

    var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, UserPassword), _cancellationToken);
    var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: _cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginContent!.Data!.Token);

    var beforeResponse = await client.GetAsync(new Uri("/users/me/permissions", UriKind.Relative), _cancellationToken);
    beforeResponse.StatusCode.Should().Be(HttpStatusCode.OK, "la cuenta todavía está activa");

    await DisableUserAsync(email, deleteInsteadOfDeactivate);

    var afterResponse = await client.GetAsync(new Uri("/users/me/permissions", UriKind.Relative), _cancellationToken);
    afterResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "el token de una cuenta desactivada o eliminada no debe seguir valiendo");
  }

  private async Task CreateActiveUserAsync(string email)
  {
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();

    db.Users.Add(new User
    {
      PublicId = Guid.NewGuid(),
      Username = $"deactivated-{Guid.NewGuid():N}",
      Email = email,
      PasswordHash = UserPasswordHash,
      Status = "active",
      IsActive = true,
      IsDeleted = false,
    });

    await db.SaveChangesAsync(_cancellationToken);
  }

  private async Task DisableUserAsync(string email, bool delete)
  {
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);

    if (delete)
    {
      user.IsDeleted = true;
    }
    else
    {
      user.IsActive = false;
    }

    await db.SaveChangesAsync(_cancellationToken);
  }
}
