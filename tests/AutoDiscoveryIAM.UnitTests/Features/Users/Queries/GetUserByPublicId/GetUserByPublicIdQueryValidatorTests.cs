using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Queries.GetUserByPublicId;

public class GetUserByPublicIdQueryValidatorTests
{
  private readonly GetUserByPublicIdQueryValidator _validator = new();

  [Fact]
  public void Validate_WhenPublicIdIsEmpty_ShouldHaveError()
  {
    var query = new GetUserByPublicIdQuery(Guid.Empty);

    var result = _validator.TestValidate(query);

    result.IsValid.Should().BeFalse();
    result.ShouldHaveValidationErrorFor(x => x.PublicId);
  }

  [Fact]
  public void Validate_WhenPublicIdIsValid_ShouldPass()
  {
    var query = new GetUserByPublicIdQuery(Guid.NewGuid());

    var result = _validator.TestValidate(query);

    result.IsValid.Should().BeTrue();
  }
}
