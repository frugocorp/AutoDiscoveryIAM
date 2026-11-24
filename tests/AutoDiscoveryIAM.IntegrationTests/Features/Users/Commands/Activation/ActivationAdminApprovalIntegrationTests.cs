#pragma warning disable CS8602
using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.Activation;

/// <summary>
/// Pruebas de integración de la aprobación obligatoria (<c>Registration:RequireAdminApproval</c>) y de
/// que una cuenta rechazada no se reactive sola con el enlace de activación.
/// </summary>
[Collection("GlobalTestSetup")]
public class ActivationAdminApprovalIntegrationTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string Password = "TestPass123*";
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  [Fact(DisplayName = "Con aprobación obligatoria: confirmar el correo no activa; el login falla hasta que un admin aprueba")]
  public async Task RequireAdminApproval_ConfirmThenApprove_ActivatesOnlyAfterApproval()
  {
    using var approvalFactory = factory.WithWebHostBuilder(builder => builder.UseSetting("Registration:RequireAdminApproval", "true"));
    var client = approvalFactory.CreateClient();
    var (email, token) = await RegisterAndGetTokenAsync(approvalFactory, client);

    var confirm = await client.PostAsJsonAsync("/users/activation/confirm", new ActivationConfirmRequestDto(token), _cancellationToken);
    confirm.StatusCode.Should().Be(HttpStatusCode.OK);
    var confirmBody = await confirm.Content.ReadFromJsonAsync<ActivationResponse>(_cancellationToken);
    confirmBody!.Data.IsActivated.Should().BeFalse();

    var loginBefore = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, Password), _cancellationToken);
    loginBefore.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    var loginBeforeBody = await loginBefore.Content.ReadFromJsonAsync<LoginResponse>(_cancellationToken);
    loginBeforeBody!.ErrorInfo!.Key.Should().Be(ErrorInfo.AccountPendingOrInactive.Key);

    using var adminClient = approvalFactory.CreateClient();
    await adminClient.AuthenticateUserAsync(UserType.Admin, _cancellationToken);
    var publicId = await GetUserAsync(approvalFactory, email, u => u.PublicId);
    (await adminClient.PostAsync($"/users/{publicId}/approve", null, _cancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);

    var loginAfter = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, Password), _cancellationToken);
    loginAfter.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact(DisplayName = "Aprobar una cuenta sin correo confirmado la activa, pero el correo sigue sin verificar")]
  public async Task Approve_WithoutEmailConfirmation_KeepsEmailUnverified()
  {
    using var approvalFactory = factory.WithWebHostBuilder(builder => builder.UseSetting("Registration:RequireAdminApproval", "true"));
    var client = approvalFactory.CreateClient();
    var (email, _) = await RegisterAndGetTokenAsync(approvalFactory, client);

    using var adminClient = approvalFactory.CreateClient();
    await adminClient.AuthenticateUserAsync(UserType.Admin, _cancellationToken);
    var publicId = await GetUserAsync(approvalFactory, email, u => u.PublicId);
    (await adminClient.PostAsync($"/users/{publicId}/approve", null, _cancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);

    (await GetUserAsync(approvalFactory, email, u => u.IsActive)).Should().BeTrue();
    (await GetUserAsync(approvalFactory, email, u => u.IsEmailVerified)).Should().BeFalse("el administrador no prueba de quién es el correo");
  }

  [Fact(DisplayName = "Con aprobación obligatoria el registro nace en pending_approval")]
  public async Task RequireAdminApproval_Register_StartsInPendingApproval()
  {
    using var approvalFactory = factory.WithWebHostBuilder(builder => builder.UseSetting("Registration:RequireAdminApproval", "true"));
    var client = approvalFactory.CreateClient();

    var (email, _) = await RegisterAndGetTokenAsync(approvalFactory, client);

    (await GetUserAsync(approvalFactory, email, u => u.Status)).Should().Be(UserStatusCatalog.PendingApproval);
  }

  [Fact(DisplayName = "Una cuenta rechazada no recibe enlace nuevo por el reenvío público ni se activa con el anterior")]
  public async Task RejectedUser_CannotReactivateThroughPublicResend()
  {
    var client = factory.CreateClient();
    var (email, token) = await RegisterAndGetTokenAsync(factory, client);
    using var adminClient = factory.CreateClient();
    await adminClient.AuthenticateUserAsync(UserType.Admin, _cancellationToken);
    var publicId = await GetUserAsync(factory, email, u => u.PublicId);
    (await adminClient.PostAsync($"/users/{publicId}/reject", null, _cancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);

    (await client.PostAsJsonAsync("/users/activation/resend", new ActivationResendRequestDto(email), _cancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    var confirm = await client.PostAsJsonAsync("/users/activation/confirm", new ActivationConfirmRequestDto(token), _cancellationToken);

    confirm.IsSuccessStatusCode.Should().BeFalse();
    (await GetUserAsync(factory, email, u => u.EmailVerificationToken)).Should().BeNull("el reenvío público no debe emitir un enlace nuevo a una cuenta rechazada");
    (await GetUserAsync(factory, email, u => u.IsActive)).Should().BeFalse();
  }

  private async Task<(string Email, string Token)> RegisterAndGetTokenAsync(WebApplicationFactory<Program> appFactory, HttpClient client)
  {
    var suffix = Guid.NewGuid().ToString("N")[..12];
    var email = $"approval_{suffix}@example.com";
    var create = await client.PostAsJsonAsync("/users", new CreateUserRequestDto($"approval_{suffix}", email, Password), _cancellationToken);
    create.EnsureSuccessStatusCode();

    // El token en claro solo viaja por correo: la prueba fija uno conocido.
    var token = $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
    using var scope = appFactory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);
    user.EmailVerificationToken = OneTimeTokenHasher.Hash(token);
    await db.SaveChangesAsync(_cancellationToken);
    return (email, token);
  }

  private async Task<T> GetUserAsync<T>(WebApplicationFactory<Program> appFactory, string email, System.Linq.Expressions.Expression<Func<FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities.User, T>> selector)
  {
    using var scope = appFactory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    return await db.Users.AsNoTracking().Where(u => u.Email == email).Select(selector).SingleAsync(_cancellationToken);
  }
}
