// -----------------------------------------------------------------------
// <copyright file="AutoDiscoveryIAMRateLimitDefaultsTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.RateLimit;

public class AutoDiscoveryIAMRateLimitDefaultsTests
{
  [Fact(DisplayName = "Sin sección RateLimiting: se agregan todas las reglas por defecto")]
  public void Apply_WithoutConsumerRules_AddsAllDefaults()
  {
    var options = new RateLimitingSettings();

    AutoDiscoveryIAMRateLimitDefaults.Apply(options);

    options.Rules.Should().HaveCount(AutoDiscoveryIAMRateLimitDefaults.Rules.Count);
  }

  [Fact(DisplayName = "Una regla del consumidor con la misma ruta y periodo reemplaza a la de la librería")]
  public void Apply_ConsumerRuleWithSameEndpointAndPeriod_ReplacesDefault()
  {
    var options = new RateLimitingSettings
    {
      Rules =
      [
        new RateLimitRule { Endpoint = "POST:/auth/login*", Period = "1m", Limit = 50 },
        new RateLimitRule { Endpoint = "*", Period = "1m", Limit = 100 },
      ],
    };

    AutoDiscoveryIAMRateLimitDefaults.Apply(options);

    var loginPerMinute = options.Rules.Where(r =>
      string.Equals(r.Endpoint, "post:/auth/login*", StringComparison.OrdinalIgnoreCase) && r.Period == "1m").ToList();
    loginPerMinute.Should().ContainSingle();
    loginPerMinute[0].Limit.Should().Be(50);

    options.Rules.Should().Contain(r => r.Endpoint == "post:/auth/login*" && r.Period == "1h", "solo se reemplaza el periodo que el consumidor declaró");
    options.Rules.Should().Contain(r => r.Endpoint == "*" && r.Limit == 100, "las reglas propias del consumidor se conservan");
  }

  [Fact(DisplayName = "Aplicar las reglas no modifica la lista compartida de reglas por defecto")]
  public void Apply_DoesNotShareRuleInstancesWithDefaults()
  {
    var options = new RateLimitingSettings();

    AutoDiscoveryIAMRateLimitDefaults.Apply(options);
    options.Rules[0].Limit = 999_999;

    AutoDiscoveryIAMRateLimitDefaults.Rules[0].Limit.Should().NotBe(999_999);
  }

  [Fact(DisplayName = "Las reglas por defecto pasan su propia validación")]
  public void Defaults_AreValid()
  {
    var options = new RateLimitingSettings();
    AutoDiscoveryIAMRateLimitDefaults.Apply(options);

    new RateLimitingSettingsValidator().Validate(null, options).Succeeded.Should().BeTrue();
  }
}
