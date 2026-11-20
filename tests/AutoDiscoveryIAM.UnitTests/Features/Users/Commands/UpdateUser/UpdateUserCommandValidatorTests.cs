using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenPublicIdIsEmpty_ShouldHaveError()
    {
        var command = new UpdateUserCommand(Guid.Empty, new UpdateUserBodyDto("user.ok", null, null));

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.PublicId)
          .WithErrorMessage("PublicId es requerido");
    }

    [Fact]
    public void Validate_WhenAllFieldsAreEmpty_ShouldHaveAggregateError()
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), new UpdateUserBodyDto(null, null, null));

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Any(e => e.ErrorMessage == "Debe proporcionar al menos un campo para actualizar (Username, Email o Notes)")
          .Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEmailIsInvalid_ShouldHaveError()
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), new UpdateUserBodyDto(null, "email-invalido", null));

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Request.Email)
          .WithErrorMessage("Email debe tener un formato válido");
    }

    [Fact]
    public void Validate_WhenInputIsValid_ShouldPass()
    {
        var command = new UpdateUserCommand(Guid.NewGuid(), new UpdateUserBodyDto("usuario.ok", "usuario@test.local", "nota"));

        var result = _validator.TestValidate(command);

        result.IsValid.Should().BeTrue();
    }
}
