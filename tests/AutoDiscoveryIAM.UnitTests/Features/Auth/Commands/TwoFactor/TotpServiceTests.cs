using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using AwesomeAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OtpNet;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.TwoFactor;

public class TotpServiceTests
{
  private readonly TotpService _sut = CreateSut(new EphemeralDataProtectionProvider());

  private static TotpService CreateSut(IDataProtectionProvider dataProtectionProvider)
    => new(
      Options.Create(new TwoFactorSettings { Issuer = "AutoDiscoveryIAM" }),
      dataProtectionProvider,
      NullLogger<TotpService>.Instance);

  private static string CurrentCode(string secret) => new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();

  [Fact(DisplayName = "GenerateSecret retorna un secreto Base32 valido")]
  public void GenerateSecret_ReturnsValidBase32Secret()
  {
    var secret = _sut.GenerateSecret();

    secret.Should().NotBeNullOrWhiteSpace();
    var decoded = Base32Encoding.ToBytes(secret);
    decoded.Length.Should().BeGreaterThan(0);
  }

  [Fact(DisplayName = "BuildProvisioningUri contiene el issuer, la cuenta y el secreto")]
  public void BuildProvisioningUri_ContainsIssuerAccountAndSecret()
  {
    var secret = _sut.GenerateSecret();

    var uri = _sut.BuildProvisioningUri("user@example.com", secret);

    uri.Should().StartWith("otpauth://totp/");
    uri.Should().Contain("issuer=AutoDiscoveryIAM");
    uri.Should().Contain(Uri.EscapeDataString(secret));
  }

  [Fact(DisplayName = "ProtectSecret no deja el secreto en claro")]
  public void ProtectSecret_DoesNotContainPlaintextSecret()
  {
    var secret = _sut.GenerateSecret();

    var protectedSecret = _sut.ProtectSecret(secret);

    protectedSecret.Should().NotContain(secret);
  }

  [Fact(DisplayName = "ValidateCode acepta el codigo TOTP actual y retorna su paso de tiempo")]
  public void ValidateCode_WithCurrentCode_ReturnsTimeStep()
  {
    var secret = _sut.GenerateSecret();

    var timeStep = _sut.ValidateCode(_sut.ProtectSecret(secret), CurrentCode(secret), lastUsedTimeStep: null);

    timeStep.Should().NotBeNull();
  }

  [Fact(DisplayName = "ValidateCode rechaza un codigo cuyo paso de tiempo ya se uso")]
  public void ValidateCode_WithAlreadyUsedTimeStep_ReturnsNull()
  {
    var secret = _sut.GenerateSecret();
    var protectedSecret = _sut.ProtectSecret(secret);
    var code = CurrentCode(secret);
    var firstUse = _sut.ValidateCode(protectedSecret, code, lastUsedTimeStep: null);

    var replay = _sut.ValidateCode(protectedSecret, code, firstUse);

    firstUse.Should().NotBeNull();
    replay.Should().BeNull();
  }

  [Fact(DisplayName = "ValidateCode rechaza un codigo incorrecto")]
  public void ValidateCode_WithWrongCode_ReturnsNull()
  {
    var secret = _sut.GenerateSecret();
    var wrongCode = CurrentCode(secret) == "000000" ? "111111" : "000000";

    _sut.ValidateCode(_sut.ProtectSecret(secret), wrongCode, lastUsedTimeStep: null).Should().BeNull();
  }

  [Fact(DisplayName = "ValidateCode rechaza un secreto vacio")]
  public void ValidateCode_WithEmptySecret_ReturnsNull()
  {
    _sut.ValidateCode(string.Empty, "123456", lastUsedTimeStep: null).Should().BeNull();
  }

  [Fact(DisplayName = "ValidateCode rechaza un secreto guardado en claro o cifrado con otro anillo de claves")]
  public void ValidateCode_WithUnreadableSecret_ReturnsNull()
  {
    var secret = _sut.GenerateSecret();
    var protectedByOtherKeyRing = CreateSut(new EphemeralDataProtectionProvider()).ProtectSecret(secret);

    _sut.ValidateCode(secret, CurrentCode(secret), lastUsedTimeStep: null).Should().BeNull();
    _sut.ValidateCode(protectedByOtherKeyRing, CurrentCode(secret), lastUsedTimeStep: null).Should().BeNull();
  }
}
