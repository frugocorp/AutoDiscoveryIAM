using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Infrastructure.Validation;

public class RolePermissionAssignmentValidationRulesTests
{
    private readonly RolePermissionAssignmentValidator _validator = new();

    [Fact]
    public void ValidateRolePublicIdForPermissionAssignment_ShouldFail_WhenEmptyGuid()
    {
        var model = CreateValidModel();
        model.RolePublicId = Guid.Empty;

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.RolePublicId);
    }

    [Fact]
    public void ValidatePermissionPublicIdForRoleAssignment_ShouldFail_WhenEmptyGuid()
    {
        var model = CreateValidModel();
        model.PermissionPublicId = Guid.Empty;

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.PermissionPublicId);
    }

    [Fact]
    public void ValidatePermissionPublicIdsForBulkAssignment_ShouldFail_WhenCollectionContainsEmptyGuid()
    {
        var model = CreateValidModel();
        model.PermissionPublicIds = new List<Guid> { Guid.NewGuid(), Guid.Empty };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.PermissionPublicIds);
    }

    [Fact]
    public void ValidateUserPublicIdsForBulkRoleAssignment_ShouldFail_WhenCollectionContainsEmptyGuid()
    {
        var model = CreateValidModel();
        model.UserPublicIds = new List<Guid> { Guid.NewGuid(), Guid.Empty };

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.UserPublicIds);
    }

    [Fact]
    public void ValidateAll_ShouldPass_WithValidValues()
    {
        var model = CreateValidModel();

        var result = _validator.TestValidate(model);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidatePermissionPublicIdsForBulkAssignment_ShouldFail_WhenNull()
    {
        var model = CreateValidModel();
        model.PermissionPublicIds = null!;

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.PermissionPublicIds);
    }

    [Fact]
    public void ValidateUserPublicIdsForBulkRoleAssignment_ShouldFail_WhenNull()
    {
        var model = CreateValidModel();
        model.UserPublicIds = null!;

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.UserPublicIds);
    }

    private static RolePermissionAssignmentModel CreateValidModel()
    {
        return new RolePermissionAssignmentModel
        {
            RolePublicId = Guid.NewGuid(),
            PermissionPublicId = Guid.NewGuid(),
            PermissionPublicIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            UserPublicIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
        };
    }

    private sealed class RolePermissionAssignmentValidator : AbstractValidator<RolePermissionAssignmentModel>
    {
        public RolePermissionAssignmentValidator()
        {
            RuleFor(x => x.RolePublicId).ValidateRolePublicIdForPermissionAssignment();
            RuleFor(x => x.PermissionPublicId).ValidatePermissionPublicIdForRoleAssignment();
            RuleFor(x => x.PermissionPublicIds).ValidatePermissionPublicIdsForBulkAssignment();
            RuleFor(x => x.UserPublicIds).ValidateUserPublicIdsForBulkRoleAssignment();
        }
    }

    private sealed class RolePermissionAssignmentModel
    {
        public Guid RolePublicId { get; set; }

        public Guid PermissionPublicId { get; set; }

        public IReadOnlyCollection<Guid> PermissionPublicIds { get; set; } = new List<Guid>();

        public IReadOnlyCollection<Guid> UserPublicIds { get; set; } = new List<Guid>();
    }
}
