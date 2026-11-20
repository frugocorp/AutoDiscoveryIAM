using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.ClearBulkRoleHiddenRelations;

public class ClearBulkRoleHiddenRelationsCommandValidatorTests
{
  private readonly ClearBulkRoleHiddenRelationsCommandValidator _validator = new();

  [Fact]
  public void Validate_WhenRolePublicIdIsEmpty_ShouldHaveError()
  {
    var command = new ClearBulkRoleHiddenRelationsCommand(Guid.Empty);

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.RolePublicId)
      .WithErrorMessage(RolePermissionAssignmentValidationTypes.RolePublicIdRequired.Message);
  }

  [Fact]
  public void Validate_WhenRolePublicIdIsValid_ShouldPass()
  {
    var command = new ClearBulkRoleHiddenRelationsCommand(Guid.NewGuid());

    var result = _validator.TestValidate(command);

    result.IsValid.Should().BeTrue();
  }
}
