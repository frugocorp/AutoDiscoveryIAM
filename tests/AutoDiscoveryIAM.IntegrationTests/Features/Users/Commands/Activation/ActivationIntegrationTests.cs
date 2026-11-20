using System.Net;
using System.Net.Http.Json;
using System.Text;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Notifications.Mail;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Users.Commands.Activation;

[Collection("GlobalTestSetup")]
public class ActivationIntegrationTests(CustomWebApplicationFactory<Program> factory)
  : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();
  private readonly CustomWebApplicationFactory<Program> _factory = factory;
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(20)).Token;

  [Fact]
  public async Task ActivationConfirm_WithValidToken_ActivatesUser()
  {
    using var realFactory = CreateFactoryWithRealEmailService();
    var realClient = realFactory.CreateClient();

    var (email, token) = await CreatePendingUserAndGetTokenAsync(realClient);

    var response = await _client.GetAsync($"/users/activation/confirm?token={Uri.EscapeDataString(token)}", _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);
    user.IsActive.Should().BeTrue();
    user.IsEmailVerified.Should().BeTrue();
    user.EmailVerificationToken.Should().BeNull();
    user.EmailVerificationTokenExpiration.Should().BeNull();
  }

  [Fact]
  public async Task ActivationConfirm_WithInvalidToken_ReturnsNotFound()
  {
    var response = await _client.GetAsync("/users/activation/confirm?token=invalid-token", _cancellationToken);
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  [Fact]
  public async Task ActivationResend_Public_ReturnsNeutralResponse()
  {
    var response = await _client.PostAsJsonAsync("/users/activation/resend", new ActivationResendRequestDto("unknown@test.local"), _cancellationToken);
    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact]
  public async Task ActivationApprove_Admin_ActivatesUser()
  {
    await _client.AuthenticateUserAsync(UserType.SuAdmin, _cancellationToken);

    var (email, _) = await CreatePendingUserAndGetTokenAsync();
    var publicId = await GetUserPublicIdByEmailAsync(email);

    var response = await _client.PostAsync($"/users/{publicId}/approve", null, _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  [Fact]
  public async Task ActivationReject_Admin_RejectsUser()
  {
    await _client.AuthenticateUserAsync(UserType.SuAdmin, _cancellationToken);

    var (email, _) = await CreatePendingUserAndGetTokenAsync();
    var publicId = await GetUserPublicIdByEmailAsync(email);

    var response = await _client.PostAsync($"/users/{publicId}/reject", null, _cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);
    user.IsActive.Should().BeFalse();
  }

  [Fact]
  public async Task ActivationTestEmail_Admin_ReturnsOk_WhenSmtpConfigured()
  {
    using var realFactory = CreateFactoryWithRealEmailService();
    var realClient = realFactory.CreateClient();

    await realClient.AuthenticateUserAsync(UserType.SuAdmin, _cancellationToken);

    var response = await realClient.PostAsJsonAsync(
"/users/activation/test-email",
new ActivationTestEmailRequestDto("SMTP test", "Correo de prueba desde integration test."),
_cancellationToken);

    response.StatusCode.Should().Be(HttpStatusCode.OK);
  }

  private WebApplicationFactory<Program> CreateFactoryWithRealEmailService()
    => _factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureServices(services =>
        {
          services.RemoveAll<IActivationEmailService>();
          services.AddScoped<IActivationEmailService, IntegrationToDevActivationEmailService>();
        });
    });

  private async Task<(string email, string token)> CreatePendingUserAndGetTokenAsync(HttpClient? client = null)
  {
    var effectiveClient = client ?? _client;
    await effectiveClient.AuthenticateUserAsync(UserType.SuAdmin, _cancellationToken);

    var suffix = Guid.NewGuid().ToString("N");
    var email = $"activation-{suffix}@example.com";
    var username = $"activation_{suffix}";

    var request = new CreateUserRequestDto(
      username,
      email,
      "TestPass123*");

    var createResponse = await effectiveClient.PostAsJsonAsync("/users", request, _cancellationToken);
    createResponse.EnsureSuccessStatusCode();

    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);

    user.EmailVerificationToken.Should().NotBeNullOrWhiteSpace();
    user.EmailVerificationTokenExpiration.Should().NotBeNull();
    System.Text.RegularExpressions.Regex.IsMatch(user.EmailVerificationToken!, "^[0-9A-F]{64}$").Should().BeTrue("en la base de datos solo queda el hash del token");

    // El token en claro solo viajó por correo: la prueba fija uno conocido para poder usarlo.
    var token = $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
    user.EmailVerificationToken = OneTimeTokenHasher.Hash(token);
    await db.SaveChangesAsync(_cancellationToken);

    return (email, token);
  }

  private async Task<Guid> GetUserPublicIdByEmailAsync(string email)
  {
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
    var user = await db.Users.FirstAsync(u => u.Email == email, _cancellationToken);
    return user.PublicId;
  }

  private sealed class IntegrationToDevActivationEmailService(
    IMailSender mailSender,
    IConfiguration configuration) : IActivationEmailService
  {
    private readonly IMailSender _mailSender = mailSender;
    private readonly IConfiguration _configuration = configuration;

    public Task<OperationResult<bool>> SendActivationEmailAsync(User user, string token, CancellationToken cancellationToken, bool fakeMail = false)
    {
      if (fakeMail)
      {
        return Task.FromResult(OperationResult.Ok(true));
      }

      var recipient = ResolveToDevRecipient();
      if (recipient == null)
      {
        return Task.FromResult(OperationResult.Fail<bool>(ErrorInfo.Status400Validation, "Mail:ToDev:Email es requerido para pruebas de integracion."));
      }

      const string subject = "Activa tu cuenta";
      var body = new StringBuilder()
        .Append("<p>Prueba de activacion (IntegrationTests)</p>")
        .Append("<p>Usuario origen: ").Append(WebUtility.HtmlEncode(user.Email)).Append("</p>")
        .Append("<p>Token: ").Append(WebUtility.HtmlEncode(token)).Append("</p>")
        .ToString();

      return _mailSender.SendAsync(recipient, subject, body, cancellationToken);
    }

    public Task<OperationResult<bool>> SendAccountActivatedEmailAsync(User user, CancellationToken cancellationToken, bool fakeMail = false)
    {
      if (fakeMail)
      {
        return Task.FromResult(OperationResult.Ok(true));
      }

      var recipient = ResolveToDevRecipient();
      if (recipient == null)
      {
        return Task.FromResult(OperationResult.Fail<bool>(ErrorInfo.Status400Validation, "Mail:ToDev:Email es requerido para pruebas de integracion."));
      }

      const string subject = "Tu cuenta ha sido activada";
      var body = new StringBuilder()
        .Append("<p>Prueba de cuenta activada (IntegrationTests)</p>")
        .Append("<p>Usuario origen: ").Append(WebUtility.HtmlEncode(user.Email)).Append("</p>")
        .ToString();

      return _mailSender.SendAsync(recipient, subject, body, cancellationToken);
    }

    public Task<OperationResult<bool>> SendRegistrationAttemptNoticeAsync(string email, CancellationToken cancellationToken, bool fakeMail = false)
      => Task.FromResult(OperationResult.Ok(true));

    public Task<OperationResult<bool>> SendTestEmailAsync(string? subject, string? body, CancellationToken cancellationToken, bool fakeMail = false)
    {
      if (fakeMail)
      {
        return Task.FromResult(OperationResult.Ok(true));
      }

      var recipient = ResolveToDevRecipient();
      if (recipient == null)
      {
        return Task.FromResult(OperationResult.Fail<bool>(ErrorInfo.Status400Validation, "Mail:ToDev:Email es requerido para pruebas de integracion."));
      }

      var effectiveSubject = string.IsNullOrWhiteSpace(subject) ? "SMTP test" : subject.Trim();
      var effectiveBody = string.IsNullOrWhiteSpace(body)
        ? "Correo de prueba desde IntegrationTests"
        : body;

      return _mailSender.SendAsync(recipient, effectiveSubject, effectiveBody, cancellationToken);
    }

    private string? ResolveToDevRecipient()
      => _configuration["Mail:ToDev:Email"]?.Trim();
  }
}
