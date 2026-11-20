using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateMyProfile;

public class UpdateMyProfileCommandValidatorTests
{
  private readonly UpdateMyProfileCommandValidator _validator = new();

  [Fact]
  public void Validate_WhenNoFieldsProvided_ShouldHaveError()
  {
    var command = new UpdateMyProfileCommand(Guid.NewGuid(), null);

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
  }

  [Fact]
  public void Validate_WhenPhoneNumberProvided_ShouldPass()
  {
    var command = new UpdateMyProfileCommand(Guid.NewGuid(), "+521234567890");

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeTrue();
  }
}
