// -----------------------------------------------------------------------
// <copyright file="JwtSettingsValidatorTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.Auth;

/// <summary>
/// Antes solo validaba que <see cref="JwtSettings.Key"/> no estuviera vacío, no su longitud: una
/// clave de 1-2 caracteres pasaba la validación de arranque y quedaba forzable por fuerza bruta
/// offline contra la firma HMAC de todos los tokens emitidos.
/// </summary>
[Trait("Category", "Security")]
public class JwtSettingsValidatorTests
{
  private readonly JwtSettingsValidator _validator = new();

  private static JwtSettings CreateValidSettings(string key = "ThisIsAVeryLongSecretKeyWithAtLeast32Chars!!")
    => new()
    {
      Key = key,
      Issuer = "TestIssuer",
      Audience = "TestAudience",
      ExpiryMinutes = 15,
    };

  [Fact(DisplayName = "Validate acepta una Key de 32 caracteres o mas")]
  public void Validate_KeyAtMinimumLength_Succeeds()
  {
    var settings = CreateValidSettings(new string('a', 32));

    var result = _validator.Validate(null, settings);

    result.Succeeded.Should().BeTrue();
  }

  [Theory(DisplayName = "Validate rechaza una Key mas corta que el minimo, aunque no este vacia")]
  [InlineData("short")]
  [InlineData("exactly-31-characters-long-abc")]
  public void Validate_KeyShorterThanMinimum_Fails(string shortKey)
  {
    var settings = CreateValidSettings(shortKey);

    var result = _validator.Validate(null, settings);

    result.Failed.Should().BeTrue();
    result.FailureMessage.Should().Contain("32 bytes");
  }

  [Fact(DisplayName = "Validate reporta faltante cuando Key es null o vacia, sin mencionar longitud")]
  public void Validate_MissingKey_ReportsMissingNotLength()
  {
    var settings = CreateValidSettings(string.Empty);

    var result = _validator.Validate(null, settings);

    result.Failed.Should().BeTrue();
    result.FailureMessage.Should().Contain(nameof(JwtSettings.Key));
    result.FailureMessage.Should().NotContain("32 bytes");
  }

  [Theory(DisplayName = "Validate rechaza ExpiryMinutes fuera de 1 a 1440")]
  [InlineData(0)]
  [InlineData(-5)]
  [InlineData(1441)]
  public void Validate_ExpiryOutOfRange_Fails(int expiryMinutes)
  {
    var settings = CreateValidSettings();
    settings.ExpiryMinutes = expiryMinutes;

    var result = _validator.Validate(null, settings);

    result.Failed.Should().BeTrue();
    result.FailureMessage.Should().Contain(nameof(JwtSettings.ExpiryMinutes));
  }

  [Fact(DisplayName = "La clave se mide y se usa en bytes UTF-8, sin perder los caracteres no ASCII")]
  public void SigningKey_UsesUtf8Bytes()
  {
    var settings = CreateValidSettings(new string('ñ', 16));

    settings.GetSigningKeyBytes().Should().HaveCount(32);
    _validator.Validate(null, settings).Succeeded.Should().BeTrue();
  }
}
