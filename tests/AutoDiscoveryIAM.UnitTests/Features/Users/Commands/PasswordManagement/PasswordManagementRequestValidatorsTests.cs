using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Xunit;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.PasswordManagement;

public class PasswordManagementRequestValidatorsTests
{
    private readonly ChangePasswordRequestValidator _changeValidator = new();
    private readonly RequestPasswordResetRequestValidator _requestValidator = new();
    private readonly ConfirmPasswordResetRequestValidator _confirmValidator = new();

    public static IEnumerable<object[]> InvalidPasswords => new List<object[]>
    {
        new object[] { "", UserValidationTypes.PasswordRequired.Message },
        new object[] { "short", UserValidationTypes.PasswordMinLength.Message },
        new object[] { new string('a', (int)UserValidationTypes.PasswordMaxLength.Args[0] + 1), UserValidationTypes.PasswordMaxLength.Message },
    };

    public static IEnumerable<object[]> InvalidEmails => new List<object[]>
    {
        new object[] { "", UserValidationTypes.EmailRequired.Message },
        new object[] { "invalid-email", UserValidationTypes.EmailInvalid.Message },
    };

    [Theory(DisplayName = "ChangePasswordRequestValidator falla para NewPassword invalida")]
    [MemberData(nameof(InvalidPasswords))]
    public void ChangePasswordRequestValidator_Should_Fail_For_Invalid_NewPassword(string newPassword, string expectedError)
    {
        var dto = new ChangePasswordRequestDto("CurrentPass123*", newPassword);

        var result = _changeValidator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.First().ErrorMessage.Should().Be(expectedError);
    }

    [Fact(DisplayName = "ChangePasswordRequestValidator acepta NewPassword valida sin CurrentPassword")]
    public void ChangePasswordRequestValidator_Should_Pass_When_CurrentPassword_Is_Null()
    {
        var dto = new ChangePasswordRequestDto(null, "NewPass123*");

        var result = _changeValidator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory(DisplayName = "RequestPasswordResetRequestValidator falla para Email invalido")]
    [MemberData(nameof(InvalidEmails))]
    public void RequestPasswordResetRequestValidator_Should_Fail_For_Invalid_Email(string email, string expectedError)
    {
        var dto = new RequestPasswordResetRequestDto(email);

        var result = _requestValidator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.First().ErrorMessage.Should().Be(expectedError);
    }

    [Fact(DisplayName = "RequestPasswordResetRequestValidator acepta email valido")]
    public void RequestPasswordResetRequestValidator_Should_Pass_For_Valid_Email()
    {
        var dto = new RequestPasswordResetRequestDto("user@example.com");

        var result = _requestValidator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact(DisplayName = "ConfirmPasswordResetRequestValidator falla cuando el token esta vacio")]
    public void ConfirmPasswordResetRequestValidator_Should_Fail_For_Empty_Token()
    {
        var dto = new ConfirmPasswordResetRequestDto(string.Empty, "NewPass123*");

        var result = _confirmValidator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.First().ErrorMessage.Should().Be(UserValidationTypes.ResetTokenRequired.Message);
    }

    [Theory(DisplayName = "ConfirmPasswordResetRequestValidator falla para NewPassword invalida")]
    [MemberData(nameof(InvalidPasswords))]
    public void ConfirmPasswordResetRequestValidator_Should_Fail_For_Invalid_NewPassword(string newPassword, string expectedError)
    {
        var dto = new ConfirmPasswordResetRequestDto("valid-token", newPassword);

        var result = _confirmValidator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.First().ErrorMessage.Should().Be(expectedError);
    }

    [Fact(DisplayName = "ConfirmPasswordResetRequestValidator acepta token y password validos")]
    public void ConfirmPasswordResetRequestValidator_Should_Pass_For_Valid_Data()
    {
        var dto = new ConfirmPasswordResetRequestDto("valid-token", "NewPass123*");

        var result = _confirmValidator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }
}
