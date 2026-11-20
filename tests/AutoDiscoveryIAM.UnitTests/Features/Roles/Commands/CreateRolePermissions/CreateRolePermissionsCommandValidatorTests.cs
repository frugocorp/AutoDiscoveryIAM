using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.CreateRolePermissions;

public class CreateRolePermissionsCommandValidatorTests
{
    private readonly CreateRolePermissionsCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenRolePublicIdIsEmpty_ShouldReturnExpectedValidationError()
    {
        var command = new CreateRolePermissionsCommand(Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == RolePermissionAssignmentValidationTypes.RolePublicIdRequired.Message);
    }

    [Fact]
    public void Validate_WhenPermissionPublicIdIsEmpty_ShouldReturnExpectedValidationError()
    {
        var command = new CreateRolePermissionsCommand(Guid.NewGuid(), Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == RolePermissionAssignmentValidationTypes.PermissionPublicIdRequired.Message);
    }

    [Fact]
    public void Validate_WhenIdsAreValid_ShouldPassValidation()
    {
        var command = new CreateRolePermissionsCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}


