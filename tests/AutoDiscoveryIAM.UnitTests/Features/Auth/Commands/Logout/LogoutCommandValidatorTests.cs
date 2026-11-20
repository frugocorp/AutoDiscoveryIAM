using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Logout;

public class LogoutCommandValidatorTests
{
  private readonly LogoutCommandValidator _validator = new();

  [Fact]
  public void Validate_WhenRevokeAllTokensIsFalseAndRefreshTokenMissing_ShouldHaveError()
  {
    var command = new LogoutCommand(RefreshToken: null, UserId: null, RevokeAllTokens: false);

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
  }

  [Fact]
  public void Validate_WhenRevokeAllTokensIsTrueAndUserIdMissing_ShouldHaveError()
  {
    var command = new LogoutCommand(RefreshToken: null, UserId: null, RevokeAllTokens: true);

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.UserId);
  }

  [Fact]
  public void Validate_WhenRefreshTokenIsTooShort_ShouldHaveLengthError()
  {
    var command = new LogoutCommand(RefreshToken: "short", UserId: null, RevokeAllTokens: false);

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.RefreshToken)
      .WithErrorMessage("El refresh token debe tener entre 10 y 500 caracteres.");
  }

  [Fact]
  public void Validate_WhenInputIsValid_ShouldPass()
  {
    var command = new LogoutCommand(
      RefreshToken: "refresh-token-12345",
      UserId: 10,
      RevokeAllTokens: false,
      IpAddress: "127.0.0.1",
      UserAgent: "UnitTestAgent/1.0");

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeTrue();
  }
}
