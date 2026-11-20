// -----------------------------------------------------------------------
// <copyright file="RateLimitingSettingsValidatorTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.RateLimit;

public class RateLimitingSettingsValidatorTests
{
  [Theory(DisplayName = "Una regla mal escrita detiene el arranque")]
  [InlineData("/auth/login", "1m", 10)]
  [InlineData("post:/auth/login*", "1 minuto", 10)]
  [InlineData("post:/auth/login*", "1m", 0)]
  public void Validate_InvalidRule_Fails(string endpoint, string period, int limit)
  {
    var settings = new RateLimitingSettings { Rules = [new RateLimitRule { Endpoint = endpoint, Period = period, Limit = limit }] };

    new RateLimitingSettingsValidator().Validate(null, settings).Failed.Should().BeTrue();
  }

  [Fact(DisplayName = "Una entrada de EndpointWhitelist mal escrita detiene el arranque")]
  public void Validate_InvalidWhitelistEntry_Fails()
  {
    var settings = new RateLimitingSettings { EndpointWhitelist = ["/openapi/*"] };

    new RateLimitingSettingsValidator().Validate(null, settings).Failed.Should().BeTrue();
  }

  [Fact(DisplayName = "Reglas y whitelist bien escritas pasan")]
  public void Validate_ValidSettings_Succeeds()
  {
    var settings = new RateLimitingSettings
    {
      Rules = [new RateLimitRule { Endpoint = "*", Period = "1h", Limit = 1000 }],
      EndpointWhitelist = ["get:/openapi/*"],
    };

    new RateLimitingSettingsValidator().Validate(null, settings).Succeeded.Should().BeTrue();
  }
}
