using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Infrastructure.Validation;

/// <summary>
/// Issue #075: la política de contraseña (regex adicional sobre la validación base de longitud)
/// es configurable vía <see cref="PasswordPolicyOptions"/> — sin política configurada, el
/// comportamiento es idéntico al previo a este issue (solo longitud).
/// </summary>
public class PasswordPolicyValidationTests
{
    private sealed class PasswordModel
    {
        public string Password { get; set; } = string.Empty;
    }

    private sealed class PasswordModelValidator : AbstractValidator<PasswordModel>
    {
        public PasswordModelValidator(PasswordPolicyOptions? policy)
        {
            RuleFor(x => x.Password).ValidatePassword(policy);
        }
    }

    [Fact]
    public void ValidatePassword_WithoutPolicy_ShouldOnlyValidateLength()
    {
        var validator = new PasswordModelValidator(policy: null);

        var result = validator.TestValidate(new PasswordModel { Password = "aaaaaaaa" });

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ValidatePassword_WithConfiguredRegex_ShouldRejectPasswordNotMatchingIt()
    {
        var policy = new PasswordPolicyOptions
        {
            Regex = "^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9]).+$",
            Message = new PasswordPolicyMessages { Invalid = "Falta mayúscula, minúscula o número." },
        };
        var validator = new PasswordModelValidator(policy);

        var result = validator.TestValidate(new PasswordModel { Password = "aaaaaaaa" });

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Falta mayúscula, minúscula o número.");
    }

    [Fact]
    public void ValidatePassword_WithConfiguredRegex_ShouldAcceptPasswordMatchingIt()
    {
        var policy = new PasswordPolicyOptions
        {
            Regex = "^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9]).+$",
        };
        var validator = new PasswordModelValidator(policy);

        var result = validator.TestValidate(new PasswordModel { Password = "Aaaaaaa1" });

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ValidatePassword_WithEmptyRegexInPolicy_ShouldOnlyValidateLength()
    {
        var policy = new PasswordPolicyOptions { Regex = string.Empty };
        var validator = new PasswordModelValidator(policy);

        var result = validator.TestValidate(new PasswordModel { Password = "aaaaaaaa" });

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
