using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateUserTypeAdmin;

public class UpdateUserTypeAdminCommandValidatorTests
{
    private readonly UpdateUserTypeAdminCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenPublicIdIsEmpty_ShouldHaveError()
    {
        var command = new UpdateUserTypeAdminCommand(Guid.Empty, new UpdateUserTypeAdminBodyDto(true, "reason"));

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.PublicId)
          .WithErrorMessage("El PublicId del usuario es requerido");
    }

    [Fact]
    public void Validate_WhenReasonExceedsMaxLength_ShouldHaveError()
    {
        var command = new UpdateUserTypeAdminCommand(Guid.NewGuid(), new UpdateUserTypeAdminBodyDto(true, new string('r', 501)));

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Request.Reason)
          .WithErrorMessage("La razón no puede exceder los 500 caracteres");
    }

    [Fact]
    public void Validate_WhenInputIsValid_ShouldPass()
    {
        var command = new UpdateUserTypeAdminCommand(Guid.NewGuid(), new UpdateUserTypeAdminBodyDto(true, "Promocion valida"));

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeTrue();
    }
}
