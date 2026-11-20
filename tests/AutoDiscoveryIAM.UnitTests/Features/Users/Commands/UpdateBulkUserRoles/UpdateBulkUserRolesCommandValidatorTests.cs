using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateBulkUserRoles;

public class UpdateBulkUserRolesCommandValidatorTests
{
    private readonly UpdateBulkUserRolesCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenUserPublicIdIsEmpty_ShouldHaveError()
    {
        var command = new UpdateBulkUserRolesCommand(Guid.Empty, [Guid.NewGuid()], false);

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.UserPublicId)
          .WithErrorMessage(UserRoleAssignmentValidationTypes.UserPublicIdRequired.Message);
    }

    [Fact]
    public void Validate_WhenRolePublicIdsIsNull_ShouldHaveError()
    {
        var command = new UpdateBulkUserRolesCommand(Guid.NewGuid(), null!, false);

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.RolePublicIds)
          .WithErrorMessage(UserRoleAssignmentValidationTypes.RolePublicIdsRequired.Message);
    }

    [Fact]
    public void Validate_WhenRolePublicIdsContainEmpty_ShouldHaveError()
    {
        var command = new UpdateBulkUserRolesCommand(Guid.NewGuid(), [Guid.NewGuid(), Guid.Empty], false);

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.RolePublicIds)
          .WithErrorMessage(UserRoleAssignmentValidationTypes.RolePublicIdsContainEmpty.Message);
    }

    [Fact]
    public void Validate_WhenInputIsValid_ShouldPass()
    {
        var command = new UpdateBulkUserRolesCommand(Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()], false);

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeTrue();
    }
}
