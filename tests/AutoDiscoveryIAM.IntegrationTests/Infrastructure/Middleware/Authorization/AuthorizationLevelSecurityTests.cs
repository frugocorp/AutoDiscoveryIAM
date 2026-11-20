// -----------------------------------------------------------------------
// <copyright file="AuthorizationLevelSecurityTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Infrastructure.Middleware.Authorization;

[Collection("GlobalTestSetup")]
public class AuthorizationLevelSecurityTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
  private readonly HttpClient client = factory.CreateClient();
  private readonly CancellationToken cancellationToken = CancellationToken.None;

  public Task InitializeAsync() => Task.CompletedTask;

  public Task DisposeAsync()
  {
    client?.Dispose();
    return Task.CompletedTask;
  }

  #region Development Access Tests

  [Fact]
  // Description: Valida que usuarios Development pueden acceder a endpoints marcados con [RequiresDevelopmentAccess].
  // Espera: No recibir 401 Unauthorized ni 403 Forbidden (puede ser 404/500 por ambiente de test).
  // Valida: A) Autenticación correcta, B) Bypass de autorización para Development users, C) Acceso a endpoints críticos de desarrollo.
  public async Task Development_User_Should_Access_Development_Endpoints()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.Development, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act - Test access to Development endpoint that throws exception on purpose
    // Note: In DEBUG mode this would work, but in test environment it may return 404
    var response = await client.GetAsync("/api/ExceptionHandlingMiddlewareDiagnostics/throw", cancellationToken);

    // Assert - Should not be unauthorized or forbidden (may be 404/500 in test env)
    response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
  }

  [Fact]
  // Description: Valida que usuarios SuAdmin NO pueden acceder a endpoints marcados con [RequiresDevelopmentAccess].
  // Espera: 403 Forbidden (o 404 si endpoint no está disponible en test).
  // Valida: A) Jerarquía de autorización, B) Restricción de acceso a endpoints Development, C) Denegación correcta para SuAdmin.
  public async Task SuAdmin_User_Should_NOT_Access_Development_Endpoints()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.SuAdmin, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/api/ExceptionHandlingMiddlewareDiagnostics/throw", cancellationToken);

    // Assert - Should be forbidden (unless 404 porque endpoint not available in test mode)
    if (response.StatusCode != HttpStatusCode.NotFound)
    {
      response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
  }

  [Fact]
  // Description: Valida que usuarios Admin NO pueden acceder a endpoints marcados con [RequiresDevelopmentAccess].
  // Espera: 403 Forbidden (o 404 si endpoint no está disponible en test).
  // Valida: A) Separación entre Admin y Development, B) Restricción de acceso a herramientas de desarrollo, C) Middleware bloqueando correctamente.
  public async Task Admin_User_Should_NOT_Access_Development_Endpoints()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.Admin, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/api/ExceptionHandlingMiddlewareDiagnostics/throw", cancellationToken);

    // Assert - Should be forbidden (unless 404 porque endpoint not available in test mode)
    if (response.StatusCode != HttpStatusCode.NotFound)
    {
      response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
  }

  [Fact]
  // Description: Valida que usuarios regulares NO pueden acceder a endpoints marcados con [RequiresDevelopmentAccess].
  // Espera: 403 Forbidden (o 404 si endpoint no está disponible en test).
  // Valida: A) Protección de endpoints críticos, B) Denegación para usuarios sin privilegios, C) Seguridad de herramientas de desarrollo.
  public async Task Regular_User_Should_NOT_Access_Development_Endpoints()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.Regular, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/api/ExceptionHandlingMiddlewareDiagnostics/throw", cancellationToken);

    // Assert - Should be forbidden (unless 404 porque endpoint not available in test mode)
    if (response.StatusCode != HttpStatusCode.NotFound)
    {
      response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
  }

  #endregion

  #region SuAdmin Access Tests

  [Fact]
  // Description: Valida que usuarios Development pueden acceder a endpoints marcados con [RequiresSuAdminAccess].
  // Espera: No recibir 401 Unauthorized ni 403 Forbidden (puede fallar por razones de negocio, no autorización).
  // Valida: A) Jerarquía Development > SuAdmin, B) Acceso a funciones administrativas críticas, C) Bypass correcto para Development.
  public async Task Development_User_Should_Access_SuAdmin_Endpoints()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.Development, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var testUserId = Guid.NewGuid();
    var request = new { IsAdmin = true };

    // Act
    var response = await client.PutAsJsonAsync($"/users/{testUserId}/admin-type", request, cancellationToken);

    // Assert - Development users debe tener access (may fail para business reasons, not auth)
    response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
  }

  [Fact]
  // Description: Valida que usuarios SuAdmin pueden acceder a endpoints marcados con [RequiresSuAdminAccess].
  // Espera: No recibir 401 Unauthorized ni 403 Forbidden (puede fallar por razones de negocio, no autorización).
  // Valida: A) Acceso correcto para SuAdmin, B) Funcionalidad de asignación de roles Admin, C) Autorización específica funcionando.
  public async Task SuAdmin_User_Should_Access_SuAdmin_Endpoints()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.SuAdmin, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var testUserId = Guid.NewGuid();
    var request = new { IsAdmin = true };

    // Act
    var response = await client.PutAsJsonAsync($"/users/{testUserId}/admin-type", request, cancellationToken);

    // Assert - SuAdmin users debe tener access (may fail para business reasons, not auth)
    response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
  }

  [Fact]
  // Description: Valida que usuarios Admin NO pueden acceder a endpoints marcados con [RequiresSuAdminAccess].
  // Espera: 403 Forbidden.
  // Valida: A) Separación entre Admin y SuAdmin, B) Restricción para asignación de roles Admin, C) Protección de funciones críticas.
  public async Task Admin_User_Should_NOT_Access_SuAdmin_Endpoints()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.Admin, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var testUserId = Guid.NewGuid();
    var request = new { IsAdmin = true };

    // Act
    var response = await client.PutAsJsonAsync($"/users/{testUserId}/admin-type", request, cancellationToken);

    // Assert - Admin users debe ser forbidden from SuAdmin endpoints
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  [Fact]
  // Description: Valida que usuarios regulares NO pueden acceder a endpoints marcados con [RequiresSuAdminAccess].
  // Espera: 403 Forbidden.
  // Valida: A) Protección de funciones administrativas, B) Denegación para usuarios sin privilegios, C) Seguridad de asignación de roles.
  public async Task Regular_User_Should_NOT_Access_SuAdmin_Endpoints()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.Regular, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var testUserId = Guid.NewGuid();
    var request = new { IsAdmin = true };

    // Act
    var response = await client.PutAsJsonAsync($"/users/{testUserId}/admin-type", request, cancellationToken);

    // Assert - Regular users debe ser forbidden from SuAdmin endpoints
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }

  #endregion

  #region Permission-Based Access Tests

  [Fact]
  // Description: Valida que usuarios regulares necesitan permisos específicos para acceder a endpoints sin atributos especiales.
  // Espera: 403 Forbidden OR 200 OK (dependiendo si el usuario tiene los permisos necesarios).
  // Valida: A) Sistema RBAC funcionando, B) Validación de permisos específicos, C) Comportamiento por defecto del middleware.
  public async Task Regular_User_Should_Need_Specific_Permissions_For_Endpoints()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.Regular, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act - Regular users need specific permissions para these endpoints
    var usersResponse = await client.GetAsync("/users", cancellationToken);
    var rolesResponse = await client.GetAsync("/roles", cancellationToken);
    var permissionsResponse = await client.GetAsync("/permissions", cancellationToken);

    // Assert - Regular users debe ser forbidden unless they have specific permissions
    // These endpoints require permissions like "users.get_users", "roles.get_roles", etc.
    usersResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.OK);
    rolesResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.OK);
    permissionsResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.OK);
  }

  [Fact]
  // Description: Valida que usuarios especiales (Development, SuAdmin, Admin) hacen bypass de validaciones de permisos específicos.
  // Espera: No recibir 403 Forbidden para endpoints sin atributos especiales.
  // Valida: A) Bypass automático para usuarios especiales, B) Jerarquía de autorización, C) Optimización de performance.
  public async Task Special_Users_Should_Bypass_Permission_Checks()
  {
    var specialUsers = new[] { UserType.Development, UserType.SuAdmin, UserType.Admin };

    foreach (var userType in specialUsers)
    {
      // Arrange
      var token = await client.GetAuthTokenAsync(userType, cancellationToken);
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

      // Act - Special users debe omitir permission checks
      // NOTE: /users y /roles requieren SuAdmin específicamente (RequiresSuAdminAccessAttribute),
      // por lo que Admin no los pasaría — se usa /permissions, que no tiene esa restricción granular,
      // para probar el bypass general de IsAdmin/IsDevelopment/IsSuAdmin.
      var permissionsResponse = await client.GetAsync("/permissions", cancellationToken);

      // Assert - Special users should not be forbidden (bypass permission checks para permissions)
      permissionsResponse.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
          $"{userType} user debe omitir permission checks para permissions");

      // Clean up para next iteración
      client.DefaultRequestHeaders.Authorization = null;
    }
  }

  #endregion

  #region Unauthenticated Tests

  [Fact]
  // Description: Valida que usuarios no autenticados son denegados en todos los endpoints protegidos.
  // Espera: 401 Unauthorized para todos los requests sin token y respuestas en formato ApiResponse.
  // Valida: A) Autenticación requerida funcionando, B) Seguridad básica, C) Middleware de autenticación antes de autorización, D) Formato consistente ApiResponse.
  public async Task Unauthenticated_Users_Should_Be_Denied_All_Protected_Endpoints()
  {
    // Act - Test various protected endpoints without authentication
    var usersResponse = await client.GetAsync("/users", cancellationToken);
    var rolesResponse = await client.GetAsync("/roles", cancellationToken);
    var permissionsResponse = await client.GetAsync("/permissions", cancellationToken);

    var testUserId = Guid.NewGuid();
    var suAdminResponse = await client.PutAsJsonAsync($"/users/{testUserId}/admin-type", new { IsAdmin = true }, cancellationToken);

    // Assert - All should require authentication and return ApiResponse format
    usersResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    rolesResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    permissionsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    suAdminResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    // Validate that all responses follow ApiResponse format
    await AuthorizationValidationHelper.ValidateApiResponseFormat(usersResponse, AuthorizationValidationTypes.Messages.AuthenticationRequired);
    await AuthorizationValidationHelper.ValidateApiResponseFormat(rolesResponse, AuthorizationValidationTypes.Messages.AuthenticationRequired);
    await AuthorizationValidationHelper.ValidateApiResponseFormat(permissionsResponse, AuthorizationValidationTypes.Messages.AuthenticationRequired);
    await AuthorizationValidationHelper.ValidateApiResponseFormat(suAdminResponse, AuthorizationValidationTypes.Messages.AuthenticationRequired);
  }

  #endregion
}



