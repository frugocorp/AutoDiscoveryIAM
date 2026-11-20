using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Roles.Queries.GetRoleByPublicId;

public class GetRoleByPublicIdQueryValidatorTests
{
    private readonly GetRoleByPublicIdQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenPublicIdIsEmpty_ShouldHaveError()
    {
        var query = new GetRoleByPublicIdQuery(Guid.Empty);

        var result = _validator.TestValidate(query);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.PublicId)
          .WithErrorMessage(RoleValidationTypes.PublicIdRequired.Message);
    }

    [Fact]
    public void Validate_WhenPublicIdIsValid_ShouldPass()
    {
        var query = new GetRoleByPublicIdQuery(Guid.NewGuid());

        var result = _validator.TestValidate(query);

        result.IsValid.Should().BeTrue();
    }
}
