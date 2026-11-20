using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using AwesomeAssertions;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Commands.UpdateBulkRolePermissions;

public class UpdateBulkRolePermissionsCommandValidatorTests
{
    private readonly UpdateBulkRolePermissionsCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenRolePublicIdIsEmpty_ShouldReturnExpectedValidationError()
    {
        var command = new UpdateBulkRolePermissionsCommand(Guid.Empty, [Guid.NewGuid()], false);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == RolePermissionAssignmentValidationTypes.RolePublicIdRequired.Message);
    }

    [Fact]
    public void Validate_WhenPermissionIdsContainEmptyGuid_ShouldReturnExpectedValidationError()
    {
        var command = new UpdateBulkRolePermissionsCommand(Guid.NewGuid(), [Guid.NewGuid(), Guid.Empty], false);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage == RolePermissionAssignmentValidationTypes.PermissionPublicIdsContainEmpty.Message);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldPassValidation()
    {
        var command = new UpdateBulkRolePermissionsCommand(Guid.NewGuid(), [Guid.NewGuid()], true);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}


