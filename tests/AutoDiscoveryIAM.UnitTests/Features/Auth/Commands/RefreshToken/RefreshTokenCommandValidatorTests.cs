using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandValidatorTests
{
  private readonly RefreshTokenCommandValidator _validator = new();

  [Fact]
  public void Validate_WhenRefreshTokenIsEmpty_ShouldHaveError()
  {
    var command = new RefreshTokenCommand(string.Empty);

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
      .WithErrorMessage("El refresh token es requerido.");
  }

  [Fact]
  public void Validate_WhenRefreshTokenIsTooShort_ShouldHaveLengthError()
  {
    var command = new RefreshTokenCommand("short");

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
      .WithErrorMessage("El refresh token debe tener entre 10 y 500 caracteres.");
  }

  [Fact]
  public void Validate_WhenIpAddressExceedsMaxLength_ShouldHaveError()
  {
    var command = new RefreshTokenCommand(
      RefreshToken: "refresh-token-valid",
      IpAddress: new string('1', 46));

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.IpAddress)
      .WithErrorMessage("La dirección IP no puede exceder 45 caracteres.");
  }

  [Fact]
  public void Validate_WhenUserAgentExceedsMaxLength_ShouldHaveError()
  {
    var command = new RefreshTokenCommand(
      RefreshToken: "refresh-token-valid",
      UserAgent: new string('a', 501));

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.UserAgent)
      .WithErrorMessage("El User-Agent no puede exceder 500 caracteres.");
  }

  [Fact]
  public void Validate_WhenInputIsValid_ShouldPass()
  {
    var command = new RefreshTokenCommand(
      RefreshToken: "refresh-token-valid",
      IpAddress: "127.0.0.1",
      UserAgent: "UnitTestAgent/1.0");

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeTrue();
  }
}
