#pragma warning disable CS8602
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using System.Net.Http.Headers;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.CreateUser;

/// <summary>
/// Pruebas de integración para el comando de creación de users.
/// </summary>
/// <remarks>
/// Constructor de la clase de pruebas de integración para creación de users.
/// </remarks>
[Collection("GlobalTestSetup")]
public class CreateUserIntegrationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

  /// <summary>
  /// Realiza login y retorna el token JWT.
  /// </summary>
  private async Task<string> LoginAndGetTokenAsync(CancellationToken cancellationToken)
  {
    var loginRequest = new LoginRequestDto("test@example.com", "TestPass123!");
    var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest, cancellationToken);
    loginResponse.EnsureSuccessStatusCode();
    var loginContent = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
    return loginContent!.Data!.Token!;
  }

  /// <summary>
  /// Verifica que el endpoint POST /users crea un nuevo usuario correctamente.
  /// </summary>
  [Fact(DisplayName = "POST /users crea un nuevo usuario correctamente")]
  public async Task CreateUser_ShouldCreateNewUser()
  {
    // Arrange: Login y obtener token
    var token = await LoginAndGetTokenAsync(_cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Arrange: Crear datos dinámicos para evitar duplicidad
    long ticks = DateTime.UtcNow.Ticks;
    var username = $"integrationuser_{ticks}";
    var email = $"integrationuser_{ticks}@example.com";
    const string password = "TestPass123*";

    var createRequestDto = new CreateUserRequestDto(username, email, password);

    // Act: Ejecutar el comando de creación
    var response = await _client.PostAsJsonAsync("/users", createRequestDto, _cancellationToken);

    // Assert
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<CreateUserResponse>(_cancellationToken);
    result.Should().NotBeNull();
    result.Data.Should().NotBeNull();
    result.Data.Username.Should().Be(username);
    result.Data.Email.Should().Be(email);
  }

  /// <summary>
  /// Verifica que el endpoint POST /users No crea un nuevo usuario si el Email no tiene la estructura correcta.
  /// </summary>
  [Fact(DisplayName = "POST /users con email inválido retorna BadRequest")]
  public async Task CreateUser_InvalidEmail_ReturnsBadRequest()
  {
    // Arrange: Login y obtener token
    var token = await LoginAndGetTokenAsync(_cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Arrange: Crear datos dinámicos para evitar duplicidad
    long ticks = DateTime.UtcNow.Ticks;
    var username = $"integrationuser_{ticks}";
    const string email = "invalid-email";
    const string password = "TestPass123*";

    var createRequestDto = new CreateUserRequestDto(username, email, password);

    // Act: Ejecutar el comando de creación
    var response = await _client.PostAsJsonAsync("/users", createRequestDto, _cancellationToken);

    // Assert
    var result = await response.Content.ReadFromJsonAsync<CreateUserResponse>(_cancellationToken);
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.Data.Should().BeNull();
    var expectedEmailError = UserValidationTypes.EmailInvalid.Message;
    result.Error.Should().Contain(expectedEmailError);
  }

  /// <summary>
  /// Verifica que el endpoint POST /users con un email ya registrado no crea una segunda cuenta y
  /// responde igual que un registro nuevo (no delata qué correos existen).
  /// </summary>
  [Fact(DisplayName = "POST /users con email duplicado responde igual que un registro nuevo y no crea otra cuenta")]
  public async Task CreateUser_DuplicateEmail_ReturnsNeutralResponseWithoutCreatingAccount()
  {
    // Arrange: Login y obtener token
    var token = await LoginAndGetTokenAsync(_cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Arrange: Crear datos únicos
    long ticks = DateTime.UtcNow.Ticks;
    var username = $"integrationuser_{ticks}";
    var email = $"integrationuser_{ticks}@example.com";
    const string password = "TestPass123*";
    var createRequestDto = new CreateUserRequestDto(username, email, password);

    // Act: Crear el usuario por primera vez (debe ser exitoso)
    var response1 = await _client.PostAsJsonAsync("/users", createRequestDto, _cancellationToken);
    response1.EnsureSuccessStatusCode();

    var firstResult = await response1.Content.ReadFromJsonAsync<CreateUserResponse>(_cancellationToken);

    // Act: Intentar crear el usuario nuevamente con el mismo email
    var username2 = $"integrationuser_{ticks}_dup";
    var createRequestDtoDup = new CreateUserRequestDto(username2, email, password);
    var response2 = await _client.PostAsJsonAsync("/users", createRequestDtoDup, _cancellationToken);

    // Assert: misma forma de respuesta que un registro nuevo, sin crear una segunda cuenta.
    response2.StatusCode.Should().Be(response1.StatusCode);
    var result = await response2.Content.ReadFromJsonAsync<CreateUserResponse>(_cancellationToken);

    result.Should().NotBeNull();
    result!.Success.Should().BeTrue();
    result.Data!.Email.Should().Be(email);
    result.Data.Status.Should().Be(firstResult!.Data!.Status);
    result.Data.PublicId.Should().NotBe(firstResult.Data.PublicId, "no debe exponer el PublicId de la cuenta existente");

    using var scope = factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    (await db.Users.CountAsync(u => u.Email == email, _cancellationToken)).Should().Be(1);
    (await db.Users.AnyAsync(u => u.Username == username2, _cancellationToken)).Should().BeFalse();
  }

  [Fact(DisplayName = "POST /users con username duplicado retorna 409 Conflict (no 500)")]
  public async Task CreateUser_DuplicateUsername_ReturnsConflict()
  {
    long ticks = DateTime.UtcNow.Ticks;
    var username = $"dupusername_{ticks}";
    const string password = "TestPass123*";

    var response1 = await _client.PostAsJsonAsync("/users", new CreateUserRequestDto(username, $"dupusername_{ticks}@example.com", password), _cancellationToken);
    response1.EnsureSuccessStatusCode();

    var response2 = await _client.PostAsJsonAsync("/users", new CreateUserRequestDto(username, $"dupusername_{ticks}_2@example.com", password), _cancellationToken);
    var result = await response2.Content.ReadFromJsonAsync<CreateUserResponse>(_cancellationToken);

    response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    result!.Success.Should().BeFalse();
    result.Error.Should().Contain(UserValidationTypes.UsernameExists.Message);
  }

  /// <summary>
  /// Verifica que el endpoint POST /users no crea un nuevo usuario si el username está vacío.
  /// </summary>
  [Fact(DisplayName = "POST /users con username vacío retorna BadRequest")]
  public async Task CreateUser_EmptyUsername_ReturnsBadRequest()
  {
    // Arrange
    var token = await LoginAndGetTokenAsync(_cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    long ticks = DateTime.UtcNow.Ticks;
    const string username = "";
    var email = $"integrationuser_{ticks}@example.com";
    const string password = "TestPass123*";
    var createRequestDto = new CreateUserRequestDto(username, email, password);

    // Act
    var response = await _client.PostAsJsonAsync("/users", createRequestDto, _cancellationToken);
    var result = await response.Content.ReadFromJsonAsync<CreateUserResponse>(_cancellationToken);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.Data.Should().BeNull();
    var expectedError = UserValidationTypes.UsernameRequired.Message;
    result.Error.Should().Contain(expectedError);
  }

  /// <summary>
  /// Verifica que el endpoint POST /users no crea un nuevo usuario si el password está vacío.
  /// </summary>
  [Fact(DisplayName = "POST /users con password vacío retorna BadRequest")]
  public async Task CreateUser_EmptyPassword_ReturnsBadRequest()
  {
    // Arrange
    var token = await LoginAndGetTokenAsync(_cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    long ticks = DateTime.UtcNow.Ticks;
    var username = $"integrationuser_{ticks}";
    var email = $"integrationuser_{ticks}@example.com";
    const string password = "";
    var createRequestDto = new CreateUserRequestDto(username, email, password);

    // Act
    var response = await _client.PostAsJsonAsync("/users", createRequestDto, _cancellationToken);
    var result = await response.Content.ReadFromJsonAsync<CreateUserResponse>(_cancellationToken);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.Data.Should().BeNull();
    var expectedError = UserValidationTypes.PasswordRequired.Message;
    result.Error.Should().Contain(expectedError);
  }

  /// <summary>
  /// Verifica que el endpoint POST /users no crea un nuevo usuario si el username es demasiado corto.
  /// </summary>
  [Fact(DisplayName = "POST /users con username muy corto retorna BadRequest")]
  public async Task CreateUser_UsernameTooShort_ReturnsBadRequest()
  {
    // Arrange
    var token = await LoginAndGetTokenAsync(_cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    long ticks = DateTime.UtcNow.Ticks;
    const string username = "ab";
    var email = $"integrationuser_{ticks}@example.com";
    const string password = "TestPass123*";
    var createRequestDto = new CreateUserRequestDto(username, email, password);

    // Act
    var response = await _client.PostAsJsonAsync("/users", createRequestDto, _cancellationToken);
    var result = await response.Content.ReadFromJsonAsync<CreateUserResponse>(_cancellationToken);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.Data.Should().BeNull();
    var expectedError = UserValidationTypes.UsernameMinLength.Message;
    result.Error.Should().Contain(expectedError);
  }

  /// <summary>
  /// Verifica que el endpoint POST /users no crea un nuevo usuario si el username es demasiado largo.
  /// </summary>
  [Fact(DisplayName = "POST /users con username muy largo retorna BadRequest")]
  public async Task CreateUser_UsernameTooLong_ReturnsBadRequest()
  {
    // Arrange
    var token = await LoginAndGetTokenAsync(_cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    long ticks = DateTime.UtcNow.Ticks;
    var username = "a" + new string('b', 100);
    var email = $"integrationuser_{ticks}@example.com";
    const string password = "TestPass123*";
    var createRequestDto = new CreateUserRequestDto(username, email, password);

    // Act
    var response = await _client.PostAsJsonAsync("/users", createRequestDto, _cancellationToken);
    var result = await response.Content.ReadFromJsonAsync<CreateUserResponse>(_cancellationToken);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.Data.Should().BeNull();
    var expectedError = UserValidationTypes.UsernameMaxLength.Message;
    result.Error.Should().Contain(expectedError);
  }

  /// <summary>
  /// Verifica que el endpoint POST /users no crea un nuevo usuario si el username contiene caracteres inválidos.
  /// </summary>
  [Fact(DisplayName = "POST /users con username inválido retorna BadRequest")]
  public async Task CreateUser_UsernameInvalidChars_ReturnsBadRequest()
  {
    // Arrange
    var token = await LoginAndGetTokenAsync(_cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    long ticks = DateTime.UtcNow.Ticks;
    const string username = "usuario!@#";
    var email = $"integrationuser_{ticks}@example.com";
    const string password = "TestPass123*";
    var createRequestDto = new CreateUserRequestDto(username, email, password);

    // Act
    var response = await _client.PostAsJsonAsync("/users", createRequestDto, _cancellationToken);
    var result = await response.Content.ReadFromJsonAsync<CreateUserResponse>(_cancellationToken);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.Data.Should().BeNull();
    var expectedError = UserValidationTypes.UsernameInvalidChars.Message;
    result.Error.Should().Contain(expectedError);
  }

  /// <summary>
  /// Verifica que el endpoint POST /users no crea un nuevo usuario si el email es demasiado largo.
  /// </summary>
  [Fact(DisplayName = "POST /users con email muy largo retorna BadRequest")]
  public async Task CreateUser_EmailTooLong_ReturnsBadRequest()
  {
    // Arrange
    var token = await LoginAndGetTokenAsync(_cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    const string username = "integrationuser";
    var email = new string('a', 151) + "@example.com";
    const string password = "TestPass123*";
    var createRequestDto = new CreateUserRequestDto(username, email, password);

    // Act
    var response = await _client.PostAsJsonAsync("/users", createRequestDto, _cancellationToken);
    var result = await response.Content.ReadFromJsonAsync<CreateUserResponse>(_cancellationToken);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.Data.Should().BeNull();
    var expectedError = UserValidationTypes.EmailMaxLength.Message;
    result.Error.Should().Contain(expectedError);
  }

  /// <summary>
  /// Verifica que el endpoint POST /users no crea un nuevo usuario si el password es demasiado corto.
  /// </summary>
  [Fact(DisplayName = "POST /users con password muy corto retorna BadRequest")]
  public async Task CreateUser_PasswordTooShort_ReturnsBadRequest()
  {
    // Arrange
    var token = await LoginAndGetTokenAsync(_cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    long ticks = DateTime.UtcNow.Ticks;
    const string username = "integrationuser";
    var email = $"integrationuser_{ticks}@example.com";
    const string password = "abc";
    var createRequestDto = new CreateUserRequestDto(username, email, password);

    // Act
    var response = await _client.PostAsJsonAsync("/users", createRequestDto, _cancellationToken);
    var result = await response.Content.ReadFromJsonAsync<CreateUserResponse>(_cancellationToken);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.Data.Should().BeNull();
    var expectedError = UserValidationTypes.PasswordMinLength.Message;
    result.Error.Should().Contain(expectedError);
  }

  /// <summary>
  /// Verifica que el endpoint POST /users no crea un nuevo usuario si el password es demasiado largo.
  /// </summary>
  [Fact(DisplayName = "POST /users con password muy largo retorna BadRequest")]
  public async Task CreateUser_PasswordTooLong_ReturnsBadRequest()
  {
    // Arrange
    var token = await LoginAndGetTokenAsync(_cancellationToken);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    long ticks = DateTime.UtcNow.Ticks;
    const string username = "integrationuser";
    var email = $"integrationuser_{ticks}@example.com";
    var password = new string('a', 257);
    var createRequestDto = new CreateUserRequestDto(username, email, password);

    // Act
    var response = await _client.PostAsJsonAsync("/users", createRequestDto, _cancellationToken);
    var result = await response.Content.ReadFromJsonAsync<CreateUserResponse>(_cancellationToken);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeFalse();
    result.Data.Should().BeNull();
    var expectedError = UserValidationTypes.PasswordMaxLength.Message;
    result.Error.Should().Contain(expectedError);
  }
}
