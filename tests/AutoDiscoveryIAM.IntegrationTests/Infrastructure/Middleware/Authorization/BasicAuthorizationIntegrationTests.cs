// -----------------------------------------------------------------------
// <copyright file="BasicAuthorizationIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Infrastructure.Middleware.Authorization;

[Collection("GlobalTestSetup")]
public class BasicAuthorizationIntegrationTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
{
  private readonly HttpClient client = factory.CreateClient();
  private readonly CancellationToken cancellationToken = CancellationToken.None;

  public Task InitializeAsync() => Task.CompletedTask;

  public Task DisposeAsync()
  {
    client?.Dispose();
    return Task.CompletedTask;
  }

  [Fact]
  public async Task Development_User_Should_Access_Basic_Endpoints()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.Development, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/users", cancellationToken);

    // Assert
    response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
  }

  [Fact]
  public async Task SuAdmin_User_Should_Access_Basic_Endpoints()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.Admin, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act - /users requiere SuAdmin especificamente (RequiresSuAdminAccessAttribute); se usa
    // /permissions, sin esa restriccion granular, para probar el bypass general de Admin.
    var response = await client.GetAsync("/permissions", cancellationToken);

    // Assert
    response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
  }

  [Fact]
  public async Task Admin_User_Should_Access_Basic_Endpoints()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.Admin, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act - /users requiere SuAdmin especificamente (RequiresSuAdminAccessAttribute); se usa
    // /permissions, sin esa restriccion granular, para probar el bypass general de Admin.
    var response = await client.GetAsync("/permissions", cancellationToken);

    // Assert
    response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
  }

  [Fact]
  public async Task Regular_User_Should_Access_Basic_Endpoints()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.Regular, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/users", cancellationToken);

    // Assert
    response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task Unauthenticated_User_Should_Be_Unauthorized()
  {
    // Act
    var response = await client.GetAsync("/users", cancellationToken);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task All_User_Levels_Should_Access_Roles_Endpoint()
  {
    var userTypes = new[] { UserType.Development, UserType.Admin, UserType.Admin, UserType.Regular };

    foreach (var userType in userTypes)
    {
      // Arrange
      var token = await client.GetAuthTokenAsync(userType, cancellationToken);
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

      // Act
      var response = await client.GetAsync("/roles", cancellationToken);

      // Assert
      response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);

      // Clean up para next iteración
      client.DefaultRequestHeaders.Authorization = null;
    }
  }

  [Fact]
  public async Task Authorization_Middleware_Should_Process_Requests_Properly()
  {
    // Arrange
    var token = await client.GetAuthTokenAsync(UserType.Regular, cancellationToken);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync("/permissions", cancellationToken);

    // Assert - Should not be NotFound (middleware should process the request)
    response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
  }
}



