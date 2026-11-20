using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;

public class UpdateBulkUserRolesByRolePublicIdCommandValidatorTests
{
    private readonly UpdateBulkUserRolesByRolePublicIdCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenRolePublicIdIsEmpty_ShouldHaveError()
    {
        var command = new UpdateBulkUserRolesByRolePublicIdCommand(Guid.Empty, [Guid.NewGuid()], false);

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.RolePublicId)
          .WithErrorMessage(UserRoleAssignmentValidationTypes.RolePublicIdRequired.Message);
    }

    [Fact]
    public void Validate_WhenUserPublicIdsIsNull_ShouldHaveError()
    {
        var command = new UpdateBulkUserRolesByRolePublicIdCommand(Guid.NewGuid(), null!, false);

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.UserPublicIds)
          .WithErrorMessage(UserRoleAssignmentValidationTypes.UserPublicIdsRequired.Message);
    }

    [Fact]
    public void Validate_WhenUserPublicIdsContainEmpty_ShouldHaveError()
    {
        var command = new UpdateBulkUserRolesByRolePublicIdCommand(Guid.NewGuid(), [Guid.NewGuid(), Guid.Empty], false);

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.UserPublicIds)
          .WithErrorMessage(UserRoleAssignmentValidationTypes.UserPublicIdsContainEmpty.Message);
    }

    [Fact]
    public void Validate_WhenInputIsValid_ShouldPass()
    {
        var command = new UpdateBulkUserRolesByRolePublicIdCommand(Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()], false);

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeTrue();
    }
}
