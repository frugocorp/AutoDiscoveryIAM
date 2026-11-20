using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.DeleteUserRolesByPublicId;

public class DeleteUserRolesByPublicIdCommandValidatorTests
{
    private readonly DeleteUserRolesByPublicIdCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenUserPublicIdIsEmpty_ShouldReturnExpectedValidationError()
    {
        var command = new DeleteUserRolesByPublicIdCommand(Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == UserRoleAssignmentValidationTypes.UserPublicIdRequired.Message);
    }

    [Fact]
    public void Validate_WhenRolePublicIdIsEmpty_ShouldReturnExpectedValidationError()
    {
        var command = new DeleteUserRolesByPublicIdCommand(Guid.NewGuid(), Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == UserRoleAssignmentValidationTypes.RolePublicIdRequired.Message);
    }

    [Fact]
    public void Validate_WhenIdsAreValid_ShouldPassValidation()
    {
        var command = new DeleteUserRolesByPublicIdCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}


