using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login.External.ExchangeLoginCode;

public class ExchangeLoginCodeCommandValidatorTests
{
  private readonly ExchangeLoginCodeCommandValidator _validator = new();

  [Fact]
  public void Validate_WhenCodeIsEmpty_ShouldHaveError()
  {
    var command = new ExchangeLoginCodeCommand(string.Empty);

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.Code)
      .WithErrorMessage("El código de intercambio es requerido.");
  }

  [Fact]
  public void Validate_WhenCodeIsProvided_ShouldPass()
  {
    var command = new ExchangeLoginCodeCommand("some-opaque-code");

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeTrue();
  }
}
