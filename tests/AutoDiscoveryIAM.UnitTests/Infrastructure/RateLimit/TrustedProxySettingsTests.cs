// -----------------------------------------------------------------------
// <copyright file="TrustedProxySettingsTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.RateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Infrastructure.RateLimit;

public class TrustedProxySettingsTests
{
  [Fact(DisplayName = "Sin sección ForwardedHeaders: se confía en la red bridge de Docker")]
  public void Apply_WithoutSettings_TrustsDockerBridgeNetwork()
  {
    var options = new ForwardedHeadersOptions();

    TrustedProxySettings.Apply(new TrustedProxySettings(), options);

    options.KnownIPNetworks.Should().Contain(System.Net.IPNetwork.Parse(TrustedProxySettings.DefaultNetwork));
    options.ForwardedHeaders.Should().Be(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
  }

  [Fact(DisplayName = "Con proxies declarados: reemplazan a la red por defecto")]
  public void Apply_WithDeclaredProxies_ReplacesDefaultNetwork()
  {
    var options = new ForwardedHeadersOptions();

    TrustedProxySettings.Apply(new TrustedProxySettings { KnownNetworks = ["10.0.0.0/8"], KnownProxies = ["192.0.2.10"] }, options);

    options.KnownIPNetworks.Should().Contain(System.Net.IPNetwork.Parse("10.0.0.0/8"));
    options.KnownIPNetworks.Should().NotContain(System.Net.IPNetwork.Parse(TrustedProxySettings.DefaultNetwork));
    options.KnownProxies.Should().Contain(IPAddress.Parse("192.0.2.10"));
  }

  [Theory(DisplayName = "Un valor inválido detiene el arranque")]
  [InlineData("no-es-una-red", null)]
  [InlineData(null, "999.1.1.1")]
  public void Apply_InvalidValue_Throws(string? network, string? proxy)
  {
    var settings = new TrustedProxySettings
    {
      KnownNetworks = network is null ? null : [network],
      KnownProxies = proxy is null ? null : [proxy],
    };

    var act = () => TrustedProxySettings.Apply(settings, new ForwardedHeadersOptions());

    act.Should().Throw<InvalidOperationException>();
  }
}
