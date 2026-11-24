#pragma warning disable CS8602
using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.CreateUserByAdmin;

/// <summary>
/// Pruebas de integración de <c>POST /users/by-admin</c>: alta de usuarios por un administrador.
/// </summary>
[Collection("GlobalTestSetup")]
public class CreateUserByAdminIntegrationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string Route = "/users/by-admin";
  private const string ValidPassword = "TestPass123*";
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  private async Task<HttpClient> CreateAdminClientAsync()
  {
    var client = factory.CreateClient();
    await client.AuthenticateUserAsync(UserType.Admin, _cancellationToken);
    return client;
  }

  private static (string Username, string Email) NewIdentity(string prefix)
  {
    var suffix = Guid.NewGuid().ToString("N")[..12];
    return ($"{prefix}_{suffix}", $"{prefix}_{suffix}@example.com");
  }

  [Theory(DisplayName = "POST /users/by-admin crea al usuario con el rol por defecto, activo o pendiente según activate")]
  [InlineData(true)]
  [InlineData(false)]
  public async Task CreateUserByAdmin_Valid_CreatesUserWithDefaultRole(bool activate)
  {
    using var client = await CreateAdminClientAsync();
    var (username, email) = NewIdentity("byadmin");

    var response = await client.PostAsJsonAsync(Route, new CreateUserByAdminRequestDto(username, email, ValidPassword, activate), _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<CreateUserByAdminResponse>(_cancellationToken);
    result!.Success.Should().BeTrue();
    result.Data.Email.Should().Be(email);
    result.Data.Status.Should().Be(activate ? UserStatusCatalog.Active : UserStatusCatalog.Pending);
    result.Data.ActivationEmailSent.Should().Be(!activate);

    using var scope = factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).SingleAsync(u => u.Email == email, _cancellationToken);
    user.IsActive.Should().Be(activate);
    user.IsEmailVerified.Should().BeFalse("el administrador no prueba de quién es el correo");
    user.MustChangePassword.Should().BeTrue("con contraseña del administrador, por defecto se cambia al entrar");
    (user.EmailVerificationToken != null).Should().Be(!activate);
    user.UserRoles.Count.Should().Be(1);
    user.UserRoles.Single().Role.IsDefaultForNewUser.Should().BeTrue();
  }

  [Fact(DisplayName = "POST /users/by-admin con activate=true: el usuario inicia sesión de inmediato")]
  public async Task CreateUserByAdmin_Activated_UserCanLogIn()
  {
    using var client = await CreateAdminClientAsync();
    var (username, email) = NewIdentity("byadminlogin");
    (await client.PostAsJsonAsync(Route, new CreateUserByAdminRequestDto(username, email, ValidPassword, true), _cancellationToken)).EnsureSuccessStatusCode();

    var token = await TestUsersHelper.GetAuthTokenAsync(factory.CreateClient(), email, ValidPassword, _cancellationToken);

    token.Should().NotBeNullOrWhiteSpace();
  }

  [Fact(DisplayName = "POST /users/by-admin funciona aunque el registro público esté apagado")]
  public async Task CreateUserByAdmin_WhenPublicRegistrationDisabled_StillCreates()
  {
    using var disabledFactory = factory.WithWebHostBuilder(builder => builder.UseSetting("Registration:EmailPasswordEnabled", "false"));
    using var client = disabledFactory.CreateClient();
    await client.AuthenticateUserAsync(UserType.Admin, _cancellationToken);
    var (username, email) = NewIdentity("byadminoff");

    var response = await client.PostAsJsonAsync(Route, new CreateUserByAdminRequestDto(username, email, ValidPassword, true), _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact(DisplayName = "POST /users/by-admin con correo ya registrado responde 409 EMAIL_EXISTS")]
  public async Task CreateUserByAdmin_DuplicateEmail_ReturnsConflict()
  {
    using var client = await CreateAdminClientAsync();
    var (username, email) = NewIdentity("byadmindup");
    (await client.PostAsJsonAsync(Route, new CreateUserByAdminRequestDto(username, email, ValidPassword, true), _cancellationToken)).EnsureSuccessStatusCode();

    var response = await client.PostAsJsonAsync(Route, new CreateUserByAdminRequestDto(username + "x", email, ValidPassword, true), _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    var result = await response.Content.ReadFromJsonAsync<CreateUserByAdminResponse>(_cancellationToken);
    result!.Error.Should().Be(UserValidationTypes.EmailExists.Message);
  }

  [Fact(DisplayName = "POST /users/by-admin con nombre de usuario ya registrado responde 409 USERNAME_EXISTS")]
  public async Task CreateUserByAdmin_DuplicateUsername_ReturnsConflict()
  {
    using var client = await CreateAdminClientAsync();
    var (username, email) = NewIdentity("byadminuser");
    (await client.PostAsJsonAsync(Route, new CreateUserByAdminRequestDto(username, email, ValidPassword, true), _cancellationToken)).EnsureSuccessStatusCode();

    var response = await client.PostAsJsonAsync(Route, new CreateUserByAdminRequestDto(username, "otro_" + email, ValidPassword, true), _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    var result = await response.Content.ReadFromJsonAsync<CreateUserByAdminResponse>(_cancellationToken);
    result!.Error.Should().Be(UserValidationTypes.UsernameExists.Message);
  }

  [Fact(DisplayName = "POST /users/by-admin sin autenticación responde 401")]
  public async Task CreateUserByAdmin_Anonymous_ReturnsUnauthorized()
  {
    using var client = factory.CreateClient();
    var (username, email) = NewIdentity("byadminanon");

    var response = await client.PostAsJsonAsync(Route, new CreateUserByAdminRequestDto(username, email, ValidPassword, true), _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact(DisplayName = "POST /users/by-admin con un usuario sin el permiso responde 403 y no crea nada")]
  public async Task CreateUserByAdmin_WithoutPermission_ReturnsForbidden()
  {
    using var client = factory.CreateClient();
    await client.AuthenticateUserAsync(UserType.Regular, _cancellationToken);
    var (username, email) = NewIdentity("byadminnoperm");

    var response = await client.PostAsJsonAsync(Route, new CreateUserByAdminRequestDto(username, email, ValidPassword, true), _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    using var scope = factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, _cancellationToken)).Should().BeFalse();
  }

  public static TheoryData<string, string?, string?, string> InvalidRequests()
  {
    const string valid = "valido";
    return new TheoryData<string, string?, string?, string>
    {
      { string.Empty, null, ValidPassword, UserValidationTypes.UsernameRequired.Message },
      { "ab", null, ValidPassword, UserValidationTypes.UsernameMinLength.Message },
      { new string('a', 101), null, ValidPassword, UserValidationTypes.UsernameMaxLength.Message },
      { "con espacio", null, ValidPassword, UserValidationTypes.UsernameInvalidChars.Message },
      { valid, string.Empty, ValidPassword, UserValidationTypes.EmailRequired.Message },
      { valid, "no-es-correo", ValidPassword, UserValidationTypes.EmailInvalid.Message },
      { valid, new string('a', 140) + "@example.com", ValidPassword, UserValidationTypes.EmailMaxLength.Message },
      { valid, null, string.Empty, UserValidationTypes.PasswordRequired.Message },
      { valid, null, "Ab1*", UserValidationTypes.PasswordMinLength.Message },
      { valid, null, "Ab1*" + new string('a', 260), UserValidationTypes.PasswordMaxLength.Message },
    };
  }

  [Theory(DisplayName = "POST /users/by-admin rechaza cada regla de validación con 400 y su mensaje")]
  [MemberData(nameof(InvalidRequests))]
  public async Task CreateUserByAdmin_InvalidField_ReturnsBadRequest(string username, string? email, string? password, string expectedMessage)
  {
    using var client = await CreateAdminClientAsync();
    var (generatedUsername, generatedEmail) = NewIdentity("byadmininv");
    var request = new CreateUserByAdminRequestDto(
      username == "valido" ? generatedUsername : username,
      email ?? generatedEmail,
      password ?? ValidPassword,
      true);

    var response = await client.PostAsJsonAsync(Route, request, _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    var result = await response.Content.ReadFromJsonAsync<CreateUserByAdminResponse>(_cancellationToken);
    result!.Success.Should().BeFalse();
    result.ErrorInfo!.Key.Should().Be(ErrorInfo.Status400Validation.Key);
    result.Error.Should().Contain(expectedMessage);
  }
}
