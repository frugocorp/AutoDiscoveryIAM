using System.Net.Http.Json;
using System.Text.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Authorization.Improved;

/// <summary>
/// Tests de autorización mejorados que implementan las 4 mejoras solicitadas:
/// 1. Sin hardcoding de AuthCodes - usando helpers especializados
/// 2. Matriz completa de usuarios - validando todos los tipos incluidos regulares
/// 3. Validación de 3 aserciones - StatusCode + AuthCode + Message
/// 4. Helper centralizado para usuarios - usando TestUsersHelper
/// </summary>
[Collection("GlobalTestSetup")]
public class ImprovedAuthorizationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
  private readonly HttpClient _client = factory.CreateClient();
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  // JsonSerializerOptions estático para evitar CA1869
  private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

  public async Task InitializeAsync()
  {
    // Setup común si es necesario
    await Task.Delay(1); // Evitar CS1998
  }

  public async Task DisposeAsync()
  {
    _client.Dispose();
    await Task.Delay(1); // Evitar CS1998
  }

  /// <summary>
  /// ✅ MEJORA 1 y 3: Test que valida endpoint SuAdmin con validaciones completas sin hardcoding
  /// Valida que usuarios regulares NO pueden acceder a endpoints SuAdmin con las 3 aserciones.
  /// </summary>
  [Fact(DisplayName = "Usuarios regulares deben ser denegados en endpoints SuAdmin con error específico")]
  public async Task Regular_Users_Should_NOT_Access_SuAdmin_Endpoints_With_Complete_Validation()
  {
    // Arrange - ✅ MEJORA 4: Usando helper centralizado para usuarios
    var regularUser = TestUsersHelper.GetUserByType(UserType.Regular);
    await _client.AuthenticateUserAsync(regularUser, _cancellationToken);

    // Act - Endpoint que sigue exigiendo SuAdmin (consultas y asignaciones de roles pasaron a RBAC, issue 103)
    var response = await _client.DeleteAsync($"/roles/{Guid.NewGuid()}/hidden-relations", _cancellationToken);
    var content = await response.Content.ReadAsStringAsync(_cancellationToken);
    var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOptions);

    // Assert - ✅ MEJORA 1: Sin hardcoding, usando helper especializado
    // ✅ MEJORA 3: Validando las 3 aserciones automáticamente
    // NOTE: con un id inexistente, un SuAdmin pasa la autorización y recibe 404 del handler
    AuthorizationValidationHelper.ValidateSuperAdminAccessRequired(
        response, apiResponse, "Limpieza administrativa de relaciones ocultas de rol");
  }

  /// <summary>
  /// ✅ MEJORA 2 y 4: Test que valida matriz completa de usuarios vs endpoint SuAdmin
  /// Incluye TODOS los tipos de usuarios, incluyendo regulares que faltaban.
  /// </summary>
  [Theory(DisplayName = "Matriz completa: Usuarios vs Endpoint SuAdmin")]
  [InlineData(UserType.Anonymous, false)] // Debe ser denegado - 401
  [InlineData(UserType.Regular, false)]   // Debe ser denegado - 403
  [InlineData(UserType.Admin, false)]     // Debe ser denegado - 403
  [InlineData(UserType.SuAdmin, true)]    // Debe permitir acceso
  [InlineData(UserType.Development, true)] // Debe permitir acceso
  public async Task Matrix_All_Users_Vs_SuAdmin_Endpoint_Complete_Validation(UserType userType, bool shouldHaveAccess)
  {
    // Arrange - ✅ MEJORA 4: Helper centralizado para cualquier tipo de usuario
    var user = TestUsersHelper.GetUserByType(userType);
    await _client.AuthenticateUserAsync(user, _cancellationToken);

    // Act
    var response = await _client.DeleteAsync($"/roles/{Guid.NewGuid()}/hidden-relations", _cancellationToken);
    var content = await response.Content.ReadAsStringAsync(_cancellationToken);
    var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOptions);

    // Assert - ✅ MEJORA 3: Siempre validar las 3 aserciones según expectativa
    if (shouldHaveAccess)
    {
      AuthorizationValidationHelper.ValidateSuccessfulAccess(response, apiResponse);
    }
    else
    {
      // ✅ MEJORA 1: Sin hardcoding, helper determina qué tipo de error esperar
      AuthorizationValidationHelper.ValidateExpectedResponse(
          response,
          apiResponse,
          userType,
          EndpointAuthorizationType.RequiresSuAdminAccess,
          "roles.clear_bulk_role_hidden_relations",
          suAdminReason: "Limpieza administrativa de relaciones ocultas de rol");
    }
  }

  /// <summary>
  /// ✅ MEJORA 2: Test específico para usuarios regulares vs endpoints Development
  /// Valida que usuarios regulares NO pueden acceder a endpoints Development.
  /// </summary>
  [Fact(DisplayName = "Usuarios regulares deben ser denegados en endpoints Development")]
  public async Task Regular_Users_Should_NOT_Access_Development_Endpoints()
  {
    // Arrange
    var regularUser = TestUsersHelper.GetUserByType(UserType.Regular);
    await _client.AuthenticateUserAsync(regularUser, _cancellationToken);

    // Act - Endpoint que requiere Development access
    var response = await _client.GetAsync("/api/ExceptionHandlingMiddlewareDiagnostics/throw", _cancellationToken);

    // Assert - ✅ Validación completa específica para Development access
    // NOTE: Endpoint de diagnóstico devuelve 403 sin JSON body cuando es denegado
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden,
        "porque regular users no debe acceder a development endpoints");
  }

  /// <summary>
  /// ✅ MEJORA 2: Test que valida que usuarios Admin tampoco pueden acceder a endpoints Development
  /// Crucial: Los Admin no deberían poder acceder a funcionalidades de Development.
  /// </summary>
  [Fact(DisplayName = "Usuarios Admin deben ser denegados en endpoints Development")]
  public async Task Admin_Users_Should_NOT_Access_Development_Endpoints()
  {
    // Arrange
    var adminUser = TestUsersHelper.GetUserByType(UserType.Admin);
    await _client.AuthenticateUserAsync(adminUser, _cancellationToken);

    // Act - Endpoint que requiere Development access
    var response = await _client.GetAsync("/api/ExceptionHandlingMiddlewareDiagnostics/throw", _cancellationToken);

    // Assert
    // NOTE: Endpoint de diagnóstico devuelve 403 sin JSON body cuando es denegado
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden,
        "porque admin users no debe acceder a development endpoints");
  }

  /// <summary>
  /// ✅ MEJORA 2 y 4: Test matriz completa para endpoints PermissionsNotRequired
  /// Valida que TODOS los tipos de usuario pueden acceder.
  /// </summary>
  [Theory(DisplayName = "Matriz: Todos los usuarios pueden acceder a endpoints PermissionsNotRequired")]
  [InlineData(UserType.Anonymous)]
  [InlineData(UserType.Regular)]
  [InlineData(UserType.Admin)]
  [InlineData(UserType.SuAdmin)]
  [InlineData(UserType.Development)]
  public async Task Matrix_All_Users_Should_Access_PermissionsNotRequired_Endpoints(UserType userType)
  {
    // Arrange
    var user = TestUsersHelper.GetUserByType(userType);
    await _client.AuthenticateUserAsync(user, _cancellationToken);

    // Act - Endpoint público como login
    var loginRequest = new { Email = "test@example.com", Password = "test" };
    var response = await _client.PostAsJsonAsync("/auth/login", loginRequest, _cancellationToken);
    var content = await response.Content.ReadAsStringAsync(_cancellationToken);
    var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOptions);

    // Assert - No debe recibir errores de autorización (puede recibir errores de validación de negocio)
    response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.Unauthorized);

    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden && apiResponse?.ErrorInfo?.Key != null)
    {
      // Si hay 403, no debe ser por autorización
      apiResponse.ErrorInfo.Key.StartsWith("AUTH_", StringComparison.Ordinal).Should().BeFalse(
        $"User {userType} debe acceder a PermissionsNotRequired endpoint but got authorization error: {apiResponse.ErrorInfo.Key}");
    }
  }

  /// <summary>
  /// ✅ MEJORA 1, 2, 3: Test completo usando todos los helpers y validaciones
  /// Demuestra el uso completo de las mejoras implementadas.
  /// </summary>
  [Fact(DisplayName = "Test completo con todas las mejoras implementadas")]
  public async Task Complete_Test_With_All_Improvements_Demonstrated()
  {
    // Arrange - Usar Development user que tiene bypass de permisos para demostrar acceso exitoso
    var devUser = TestUsersHelper.GetUserByType(UserType.Development);
    await _client.AuthenticateUserAsync(devUser, _cancellationToken);

    // Act - Probar endpoint que requiere permisos específicos
    var response = await _client.GetAsync("/users", _cancellationToken);
    var content = await response.Content.ReadAsStringAsync(_cancellationToken);
    var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOptions);

    // Assert - Development user debería tener acceso exitoso
    AuthorizationValidationHelper.ValidateSuccessfulAccess(response, apiResponse);

    response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK, "porque Development user bypasses permission checks");
    apiResponse.Should().NotBeNull();
    apiResponse!.Success.Should().BeTrue("porque the request debe ser exitoso");
  }

  /// <summary>
  /// ✅ MEJORA 3: Test que demuestra importancia de validar Message además de StatusCode
  /// Simula caso donde backend retorna 200 OK pero con mensaje de error interno.
  /// </summary>
  [Fact(DisplayName = "Validación de Message previene respuestas inconsistentes")]
  public async Task Message_Validation_Prevents_Inconsistent_Responses()
  {
    // Arrange
    var regularUser = TestUsersHelper.GetUserByType(UserType.Regular);
    await _client.AuthenticateUserAsync(regularUser, _cancellationToken);

    // Act - Endpoint que requiere SuAdmin access
    // NOTE: DELETE /roles/{id}/hidden-relations sigue siendo [RequiresSuAdminAccess]
    var response = await _client.DeleteAsync($"/roles/{Guid.NewGuid()}/hidden-relations", _cancellationToken);
    var content = await response.Content.ReadAsStringAsync(_cancellationToken);
    var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, JsonOptions);

    // Assert - Validación completa previene casos como:
    // - StatusCode = 200 pero Message = "Error de autorización"
    // - StatusCode = 403 pero AuthCode = "SUCCESS"
    // - StatusCode correcto pero Message hardcodeado incorrecto
    AuthorizationValidationHelper.ValidateSuperAdminAccessRequired(
        response, apiResponse, "Limpieza administrativa de relaciones ocultas de rol");

    // Validaciones adicionales para demostrar importancia
    response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden, "porque regular users debe ser denegado");
    apiResponse?.ErrorInfo?.Key.Should().Be("AUTH_SUPER_ADMIN_REQUIRED", "porque el código de error debe ser consistente");
    apiResponse?.Error.Should().Contain("SuperAdmin", "porque el mensaje de error debe referenciar el nivel de acceso requerido");
  }
}



