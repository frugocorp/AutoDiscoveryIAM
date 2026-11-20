using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandValidatorTests
{
  private readonly CreateRoleCommandValidator _validator = new();

  [Fact]
  public void Validate_WhenNameIsEmpty_ShouldHaveError()
  {
    var command = new CreateRoleCommand(string.Empty, "Descripcion valida", true);

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.Name)
      .WithErrorMessage(RoleValidationTypes.NameRequired.Message);
  }

  [Fact]
  public void Validate_WhenNameHasInvalidChars_ShouldHaveError()
  {
    var command = new CreateRoleCommand("Admin@", "Descripcion valida", true);

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.Name)
      .WithErrorMessage(RoleValidationTypes.NameInvalidChars.Message);
  }

  [Fact]
  public void Validate_WhenDescriptionExceedsMaxLength_ShouldHaveError()
  {
    var command = new CreateRoleCommand("Admin", new string('d', 501), true);

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.Description)
      .WithErrorMessage(RoleValidationTypes.DescriptionMaxLength.Message);
  }

  [Fact]
  public void Validate_WhenInputIsValid_ShouldPass()
  {
    var command = new CreateRoleCommand("Admin", "Rol administrador", true);

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeTrue();
  }
}
