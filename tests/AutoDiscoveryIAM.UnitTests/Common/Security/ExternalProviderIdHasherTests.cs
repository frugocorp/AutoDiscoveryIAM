// -----------------------------------------------------------------------
// <copyright file="ExternalProviderIdHasherTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Common.Security;

public class ExternalProviderIdHasherTests
{
  [Fact]
  public void Hash_ShouldBeDeterministic()
  {
    var first = ExternalProviderIdHasher.Hash("sub-active");
    var second = ExternalProviderIdHasher.Hash("sub-active");

    first.Should().Be(second);
  }

  [Fact]
  public void Hash_ShouldReturnLowercaseHexOf64Characters()
  {
    var hash = ExternalProviderIdHasher.Hash("110169484474386276334");

    hash.Length.Should().Be(64);
    Regex.IsMatch(hash, "^[0-9a-f]{64}$").Should().BeTrue();
  }

  [Fact]
  public void Hash_ShouldDifferForDifferentInputs()
  {
    var first = ExternalProviderIdHasher.Hash("sub-active");
    var second = ExternalProviderIdHasher.Hash("sub-pending");

    first.Should().NotBe(second);
  }

  [Fact]
  public void Hash_ShouldNeverReturnThePlaintextInput()
  {
    const string plaintext = "sub-active";

    var hash = ExternalProviderIdHasher.Hash(plaintext);

    hash.Should().NotBe(plaintext);
    hash.Should().NotContain(plaintext);
  }
}
