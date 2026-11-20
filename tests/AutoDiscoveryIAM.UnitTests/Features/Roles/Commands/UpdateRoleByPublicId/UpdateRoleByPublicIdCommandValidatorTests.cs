using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.UpdateRoleByPublicId;

public class UpdateRoleByPublicIdCommandValidatorTests
{
  private readonly UpdateRoleByPublicIdCommandValidator _validator = new();

  [Fact]
  public void Validate_WhenPublicIdIsEmpty_ShouldHaveError()
  {
    var command = new UpdateRoleByPublicIdCommand(
      Guid.Empty,
      new UpdateRoleByPublicIdBodyDto("Admin", "Descripcion valida"));

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.PublicId)
      .WithErrorMessage(RoleValidationTypes.PublicIdRequired.Message);
  }

  [Fact]
  public void Validate_WhenNameHasInvalidChars_ShouldHaveError()
  {
    var command = new UpdateRoleByPublicIdCommand(
      Guid.NewGuid(),
      new UpdateRoleByPublicIdBodyDto("<Admin>", "Descripcion valida"));

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.Request.Name)
      .WithErrorMessage(RoleValidationTypes.NameInvalidChars.Message);
  }

  [Fact]
  public void Validate_WhenDescriptionExceedsMaxLength_ShouldHaveError()
  {
    var command = new UpdateRoleByPublicIdCommand(
      Guid.NewGuid(),
      new UpdateRoleByPublicIdBodyDto("Admin", new string('d', 501)));

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.Request.Description)
      .WithErrorMessage(RoleValidationTypes.DescriptionMaxLength.Message);
  }

  [Fact]
  public void Validate_WhenInputIsValid_ShouldPass()
  {
    var command = new UpdateRoleByPublicIdCommand(
      Guid.NewGuid(),
      new UpdateRoleByPublicIdBodyDto("Admin", "Rol administrador"));

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeTrue();
  }
}
