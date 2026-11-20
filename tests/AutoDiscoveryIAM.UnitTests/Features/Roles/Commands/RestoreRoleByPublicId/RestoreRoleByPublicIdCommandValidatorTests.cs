using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.RestoreRoleByPublicId;

public class RestoreRoleByPublicIdCommandValidatorTests
{
  private readonly RestoreRoleByPublicIdCommandValidator _validator = new();

  [Fact]
  public void Validate_WhenPublicIdIsEmpty_ShouldHaveError()
  {
    var command = new RestoreRoleByPublicIdCommand(Guid.Empty);

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.PublicId)
      .WithErrorMessage(RoleValidationTypes.PublicIdRequired.Message);
  }

  [Fact]
  public void Validate_WhenPublicIdIsValid_ShouldPass()
  {
    var command = new RestoreRoleByPublicIdCommand(Guid.NewGuid());

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeTrue();
  }
}
