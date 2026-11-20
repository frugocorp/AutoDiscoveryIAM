// -----------------------------------------------------------------------
// <copyright file="EmailChangeRequestFlowIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ExecuteEmailChangeRequest;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.EmailChangeRequestFlow;

/// <summary>
/// Issue #075: valida el flujo completo de cambio de email de extremo a extremo — solicitud
/// (usuario) → revision (Admin) → ejecucion (SuAdmin) — contra el servidor y la base de datos
/// reales, no mocks. Usa un usuario dedicado creado directamente en BD (no las cuentas fijas de
/// <see cref="TestUsersHelper"/>) porque el flujo cambia el email de forma permanente y esas
/// cuentas son compartidas por el resto de la suite.
/// </summary>
[Collection("GlobalTestSetup")]
public class EmailChangeRequestFlowIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private const string TestPassword = "TestPass123*";
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  private async Task<(string Email, Guid PublicId)> CreateActiveVerifiedUserAsync()
  {
    using var scope = factory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    var email = $"flow-{Guid.NewGuid():N}@example.com";
    var user = new User
    {
      Username = $"flow-{Guid.NewGuid():N}",
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
  public async Task FullFlow_RequestReviewApproveExecute_ShouldChangeEmailAndResetVerification()
  {
    var (originalEmail, _) = await CreateActiveVerifiedUserAsync();
    var newEmail = $"cambiado-{Guid.NewGuid():N}@example.com";

    using var userClient = await CreateAuthenticatedClientAsync(originalEmail, TestPassword);

    var requestResponse = await userClient.PostAsJsonAsync(
        "/users/me/email-change-requests",
        new RequestEmailChangeRequestDto(newEmail),
        _cancellationToken);
    requestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var requestPayload = await requestResponse.Content.ReadFromJsonAsync<RequestEmailChangeResponse>(cancellationToken: _cancellationToken);
    var requestPublicId = requestPayload!.Data!.RequestPublicId;

    using var adminClient = factory.CreateClient();
    await adminClient.AuthenticateUserAsync(UserType.Admin, _cancellationToken);

    var reviewResponse = await adminClient.PutAsJsonAsync(
        $"/users/email-change-requests/{requestPublicId}/review",
        new ReviewEmailChangeRequestBodyDto(true, null),
        _cancellationToken);
    reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    using var suAdminClient = factory.CreateClient();
    await suAdminClient.AuthenticateUserAsync(UserType.SuAdmin, _cancellationToken);

    var executeResponse = await suAdminClient.PostAsync(
        $"/users/email-change-requests/{requestPublicId}/execute",
        null,
        _cancellationToken);
    executeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var executePayload = await executeResponse.Content.ReadFromJsonAsync<ExecuteEmailChangeRequestResponse>(cancellationToken: _cancellationToken);
    executePayload!.Data!.NewEmail.Should().Be(newEmail);

    using var scope = factory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var updatedUser = await dbContext.Users.FirstAsync(u => u.Email == newEmail, _cancellationToken);
    updatedUser.IsEmailVerified.Should().BeFalse();

    // El email anterior ya no debe permitir autenticacion.
    var oldEmailLogin = await factory.CreateClient().PostAsJsonAsync(
        "/auth/login",
        new LoginRequestDto(originalEmail, TestPassword),
        _cancellationToken);
    oldEmailLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  [Fact]
  public async Task ReviewRejected_ShouldNotAllowExecution()
  {
    var (originalEmail, _) = await CreateActiveVerifiedUserAsync();
    var newEmail = $"rechazado-{Guid.NewGuid():N}@example.com";

    using var userClient = await CreateAuthenticatedClientAsync(originalEmail, TestPassword);

    var requestResponse = await userClient.PostAsJsonAsync(
        "/users/me/email-change-requests",
        new RequestEmailChangeRequestDto(newEmail),
        _cancellationToken);
    requestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var requestPayload = await requestResponse.Content.ReadFromJsonAsync<RequestEmailChangeResponse>(cancellationToken: _cancellationToken);
    var requestPublicId = requestPayload!.Data!.RequestPublicId;

    using var adminClient = factory.CreateClient();
    await adminClient.AuthenticateUserAsync(UserType.Admin, _cancellationToken);

    var reviewResponse = await adminClient.PutAsJsonAsync(
        $"/users/email-change-requests/{requestPublicId}/review",
        new ReviewEmailChangeRequestBodyDto(false, "correo sospechoso"),
        _cancellationToken);
    reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    using var suAdminClient = factory.CreateClient();
    await suAdminClient.AuthenticateUserAsync(UserType.SuAdmin, _cancellationToken);

    var executeResponse = await suAdminClient.PostAsync(
        $"/users/email-change-requests/{requestPublicId}/execute",
        null,
        _cancellationToken);

    executeResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
  }

  [Fact]
  public async Task Execute_WhenCallerIsNotSuAdmin_ShouldReturnForbidden()
  {
    var (originalEmail, _) = await CreateActiveVerifiedUserAsync();
    var newEmail = $"noautorizado-{Guid.NewGuid():N}@example.com";

    using var userClient = await CreateAuthenticatedClientAsync(originalEmail, TestPassword);

    var requestResponse = await userClient.PostAsJsonAsync(
        "/users/me/email-change-requests",
        new RequestEmailChangeRequestDto(newEmail),
        _cancellationToken);
    var requestPayload = await requestResponse.Content.ReadFromJsonAsync<RequestEmailChangeResponse>(cancellationToken: _cancellationToken);
    var requestPublicId = requestPayload!.Data!.RequestPublicId;

    using var adminClient = factory.CreateClient();
    await adminClient.AuthenticateUserAsync(UserType.Admin, _cancellationToken);
    await adminClient.PutAsJsonAsync(
        $"/users/email-change-requests/{requestPublicId}/review",
        new ReviewEmailChangeRequestBodyDto(true, null),
        _cancellationToken);

    var executeResponse = await adminClient.PostAsync(
        $"/users/email-change-requests/{requestPublicId}/execute",
        null,
        _cancellationToken);

    executeResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }
}
