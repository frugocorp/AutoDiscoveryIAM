using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserRoles;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.CreateUserRoles;

public class CreateUserRolesCommandValidatorTests
{
    private readonly CreateUserRolesCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenUserPublicIdIsEmpty_ShouldReturnExpectedValidationError()
    {
        var command = new CreateUserRolesCommand(Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == UserRoleAssignmentValidationTypes.UserPublicIdRequired.Message);
    }

    [Fact]
    public void Validate_WhenRolePublicIdIsEmpty_ShouldReturnExpectedValidationError()
    {
        var command = new CreateUserRolesCommand(Guid.NewGuid(), Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == UserRoleAssignmentValidationTypes.RolePublicIdRequired.Message);
    }

    [Fact]
    public void Validate_WhenIdsAreValid_ShouldPassValidation()
    {
        var command = new CreateUserRolesCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}



