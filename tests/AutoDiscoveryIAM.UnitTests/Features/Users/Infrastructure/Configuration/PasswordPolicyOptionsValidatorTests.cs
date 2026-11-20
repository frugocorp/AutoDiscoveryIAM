// -----------------------------------------------------------------------
// <copyright file="PasswordPolicyOptionsValidatorTests.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Infrastructure.Configuration;

public class PasswordPolicyOptionsValidatorTests
{
  private readonly PasswordPolicyOptionsValidator _validator = new();

  [Fact]
  public void Validate_WithNoRegex_ShouldSucceed()
  {
    var options = new PasswordPolicyOptions();

    var result = _validator.Validate(null, options);

    result.Succeeded.Should().BeTrue();
  }

  [Fact]
  public void Validate_WithValidRegex_ShouldSucceed()
  {
    var options = new PasswordPolicyOptions { Regex = "^(?=.*[A-Z]).+$" };

    var result = _validator.Validate(null, options);

    result.Succeeded.Should().BeTrue();
  }

  [Fact]
  public void Validate_WithInvalidRegex_ShouldFail()
  {
    var options = new PasswordPolicyOptions { Regex = "(unbalanced" };

    var result = _validator.Validate(null, options);

    result.Failed.Should().BeTrue();
    result.Failures.Should().Contain(f => f.Contains("PasswordStruct:regex"));
  }
}
