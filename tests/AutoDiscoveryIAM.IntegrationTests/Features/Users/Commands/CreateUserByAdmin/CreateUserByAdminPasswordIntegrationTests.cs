#pragma warning disable CS8602
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.CreateUserByAdmin;

/// <summary>
/// Pruebas de integración de la contraseña en las altas de un administrador: cambio obligatorio al
/// entrar e invitación para definirla.
/// </summary>
[Collection("GlobalTestSetup")]
public class CreateUserByAdminPasswordIntegrationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string Route = "/users/by-admin";
  private const string InitialPassword = "TestPass123*";
  private const string NewPassword = "NewPass456*";
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  private static (string Username, string Email) NewIdentity(string prefix)
  {
    var suffix = Guid.NewGuid().ToString("N")[..12];
    return ($"{prefix}_{suffix}", $"{prefix}_{suffix}@example.com");
  }

  private async Task CreateByAdminAsync(CreateUserByAdminRequestDto request, WebApplicationFactory<Program>? appFactory = null)
  {
    using var admin = (appFactory ?? factory).CreateClient();
    await admin.AuthenticateUserAsync(UserType.Admin, _cancellationToken);
    (await admin.PostAsJsonAsync(Route, request, _cancellationToken)).EnsureSuccessStatusCode();
  }

  [Fact(DisplayName = "mustChangePassword: el login avisa, RBAC responde 403 AUTH_PASSWORD_CHANGE_REQUIRED y cambiarla lo libera")]
  public async Task MustChangePassword_BlocksRbacUntilPasswordChanged()
  {
    var (username, email) = NewIdentity("mustchange");
    await CreateByAdminAsync(new CreateUserByAdminRequestDto(username, email, InitialPassword, true, MustChangePassword: true));
    using var client = factory.CreateClient();

    var login = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, InitialPassword), _cancellationToken);
    var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>(_cancellationToken);
    loginBody!.Data.PasswordChangeRequired.Should().BeTrue();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Data.Token);

    var blocked = await client.GetAsync("/users", _cancellationToken);
    blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    (await blocked.Content.ReadAsStringAsync(_cancellationToken)).Should().Contain(ErrorInfo.PasswordChangeRequired.Key);

    var change = await client.PostAsJsonAsync("/users/me/password", new ChangePasswordRequestDto(InitialPassword, NewPassword), _cancellationToken);
    change.StatusCode.Should().Be(HttpStatusCode.OK);

    var relogin = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, NewPassword), _cancellationToken);
    var reloginBody = await relogin.Content.ReadFromJsonAsync<LoginResponse>(_cancellationToken);
    reloginBody!.Data.PasswordChangeRequired.Should().BeFalse();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reloginBody.Data.Token);
    // Ya sin la marca: el 403 que quede es el de RBAC normal (este usuario no tiene el permiso), no el de la contraseña.
    var afterChange = await client.GetAsync("/users", _cancellationToken);
    (await afterChange.Content.ReadAsStringAsync(_cancellationToken)).Should().NotContain(ErrorInfo.PasswordChangeRequired.Key);
  }

  [Fact(DisplayName = "Sin contraseña: el usuario define la suya con el enlace, confirma su correo y entra")]
  public async Task Invitation_SetPasswordWithLink_ConfirmsEmailAndAllowsLogin()
  {
    var (username, email) = NewIdentity("invite");
    using var stubFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
    {
      services.RemoveAll<IPasswordResetEmailService>();
      services.AddScoped<IPasswordResetEmailService, StubPasswordResetEmailService>();
    }));
    await CreateByAdminAsync(new CreateUserByAdminRequestDto(username, email, null, false), stubFactory);

    // El token en claro solo viaja por correo: la prueba fija uno conocido.
    var token = $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
    using (var scope = factory.Services.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
      var user = await db.Users.SingleAsync(u => u.Email == email, _cancellationToken);
      user.IsActive.Should().BeTrue();
      user.IsEmailVerified.Should().BeFalse();
      user.PasswordResetToken.Should().NotBeNull();
      user.PasswordResetToken = OneTimeTokenHasher.Hash(token);
      await db.SaveChangesAsync(_cancellationToken);
    }

    using var client = factory.CreateClient();
    var confirm = await client.PostAsJsonAsync("/users/password/reset/confirm", new ConfirmPasswordResetRequestDto(token, NewPassword), _cancellationToken);
    confirm.StatusCode.Should().Be(HttpStatusCode.OK);

    var login = await client.PostAsJsonAsync("/auth/login", new LoginRequestDto(email, NewPassword), _cancellationToken);
    login.StatusCode.Should().Be(HttpStatusCode.OK);
    using var verifyScope = factory.Services.CreateScope();
    var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    (await verifyDb.Users.AsNoTracking().SingleAsync(u => u.Email == email, _cancellationToken)).IsEmailVerified.Should().BeTrue();
  }

  [Fact(DisplayName = "mustChangePassword sin contraseña inicial responde 400 MUST_CHANGE_PASSWORD_REQUIRES_PASSWORD")]
  public async Task MustChangePassword_WithoutPassword_ReturnsBadRequest()
  {
    using var admin = factory.CreateClient();
    await admin.AuthenticateUserAsync(UserType.Admin, _cancellationToken);
    var (username, email) = NewIdentity("mustnopass");

    var response = await admin.PostAsJsonAsync(Route, new CreateUserByAdminRequestDto(username, email, null, true, MustChangePassword: true), _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var result = await response.Content.ReadFromJsonAsync<CreateUserByAdminResponse>(_cancellationToken);
    result!.Error.Should().Contain(UserValidationTypes.MustChangePasswordRequiresPassword.Message);
  }

  private sealed class StubPasswordResetEmailService : IPasswordResetEmailService
  {
    public Task<OperationResult<bool>> SendPasswordResetEmailAsync(User user, string token, CancellationToken cancellationToken, bool fakeMail = false)
      => Task.FromResult(OperationResult.Ok(true));

    public Task<OperationResult<bool>> SendPasswordSetupEmailAsync(User user, string token, CancellationToken cancellationToken, bool fakeMail = false)
      => Task.FromResult(OperationResult.Ok(true));
  }
}
