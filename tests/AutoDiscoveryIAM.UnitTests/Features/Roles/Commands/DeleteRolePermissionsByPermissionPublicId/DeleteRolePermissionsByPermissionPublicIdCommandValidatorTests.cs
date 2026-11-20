using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;

public class DeleteRolePermissionsByPermissionPublicIdCommandValidatorTests
{
  private readonly DeleteRolePermissionsByPermissionPublicIdCommandValidator _validator = new();

  [Fact]
  public void Validate_WhenRolePublicIdIsEmpty_ShouldHaveError()
  {
    var command = new DeleteRolePermissionsByPermissionPublicIdCommand(Guid.Empty, Guid.NewGuid());

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.RolePublicId)
      .WithErrorMessage(RolePermissionAssignmentValidationTypes.RolePublicIdRequired.Message);
  }

  [Fact]
  public void Validate_WhenPermissionPublicIdIsEmpty_ShouldHaveError()
  {
    var command = new DeleteRolePermissionsByPermissionPublicIdCommand(Guid.NewGuid(), Guid.Empty);

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.PermissionPublicId)
      .WithErrorMessage(RolePermissionAssignmentValidationTypes.PermissionPublicIdRequired.Message);
  }

  [Fact]
  public void Validate_WhenInputIsValid_ShouldPass()
  {
    var command = new DeleteRolePermissionsByPermissionPublicIdCommand(Guid.NewGuid(), Guid.NewGuid());

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeTrue();
  }
}
