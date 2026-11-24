// -----------------------------------------------------------------------
// <copyright file="TwoFactorStepUpIntegrationTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.TwoFactorStepUp;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OtpNet;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.IntegrationTests.Features.Auth.Services;

/// <summary>
/// <see cref="ITwoFactorStepUpService"/> resuelto del contenedor real de la app, contra la base de
/// pruebas: el paso usado y los intentos quedan guardados entre peticiones (scopes) distintas.
/// </summary>
[Collection("GlobalTestSetup")]
public class TwoFactorStepUpIntegrationTests(CustomWebApplicationFactory<Program> factory)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly CancellationToken _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

  private async Task<TwoFactorStepUpResult> VerifyInNewScopeAsync(Guid publicId, string code)
  {
    using var scope = factory.Services.CreateScope();
    return await scope.ServiceProvider.GetRequiredService<ITwoFactorStepUpService>().VerifyAsync(publicId, code, "203.0.113.9", _cancellationToken);
  }

  [Fact(DisplayName = "Step-up: el código válido pasa una vez, su reuso se rechaza y cuenta como intento fallido")]
  public async Task StepUp_PersistsTimeStepAndFailedAttemptsAcrossScopes()
  {
    string secret;
    var publicId = Guid.NewGuid();
    using (var scope = factory.Services.CreateScope())
    {
      var totp = scope.ServiceProvider.GetRequiredService<ITotpService>();
      var db = scope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>();
      secret = totp.GenerateSecret();
      db.Users.Add(new User
      {
        PublicId = publicId,
        Username = $"stepup-{Guid.NewGuid():N}",
        Email = $"stepup-{Guid.NewGuid():N}@example.com",
        PasswordHash = "hash",
        IsActive = true,
        IsEmailVerified = true,
        Status = "active",
        TwoFactorEnabled = true,
        TwoFactorMethod = TwoFactorMethodCatalog.Authenticator,
        TwoFactorSecret = totp.ProtectSecret(secret),
      });
      await db.SaveChangesAsync(_cancellationToken);
    }

    var code = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();

    (await VerifyInNewScopeAsync(publicId, code)).IsVerified.Should().BeTrue();
    (await VerifyInNewScopeAsync(publicId, code)).Status.Should().Be(TwoFactorStepUpStatus.InvalidCode);

    using var verifyScope = factory.Services.CreateScope();
    var stored = await verifyScope.ServiceProvider.GetRequiredService<AutoDiscoveryIAMDbContext>().Users.AsNoTracking().SingleAsync(u => u.PublicId == publicId, _cancellationToken);
    stored.TwoFactorLastUsedTimeStep.Should().NotBeNull();
    stored.FailedLoginAttempts.Should().Be(1);
  }
}
