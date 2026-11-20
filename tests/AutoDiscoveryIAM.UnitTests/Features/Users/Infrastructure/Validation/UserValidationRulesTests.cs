using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Infrastructure.Validation;

public class UserValidationRulesTests
{
    private readonly UserValidationModelValidator _validator = new();

    [Theory]
    [InlineData("active")]
    [InlineData("inactive")]
    [InlineData("blocked")]
    [InlineData("suspended")]
    [InlineData("pending")]
    public void ValidateStatus_ShouldPass_WhenAllowed(string status)
    {
        var model = CreateValidModel();
        model.Status = status;

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void ValidateStatus_ShouldFail_WhenInvalid()
    {
        var model = CreateValidModel();
        model.Status = "deleted";

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("john.doe")]
    [InlineData("john_doe")]
    [InlineData("john-doe")]
    [InlineData("john123")]
    public void ValidateUsername_ShouldPass_WhenValid(string username)
    {
        var model = CreateValidModel();
        model.Username = username;

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("john doe")]
    [InlineData("john!")]
    public void ValidateUsername_ShouldFail_WhenInvalid(string username)
    {
        var model = CreateValidModel();
        model.Username = username;

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void ValidateEmail_ShouldFail_WhenInvalid()
    {
        var model = CreateValidModel();
        model.Email = "not-an-email";

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ValidatePassword_ShouldFail_WhenTooShort()
    {
        var model = CreateValidModel();
        model.Password = "short";

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ValidatePhoneNumber_ShouldFail_WhenTooLong()
    {
        var model = CreateValidModel();
        model.PhoneNumber = new string('9', 21);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ValidateIsActive_ShouldPass_ForBothBoolValues(bool isActive)
    {
        var model = CreateValidModel();
        model.IsActive = isActive;

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveValidationErrorFor(x => x.IsActive);
    }

    private static UserValidationModel CreateValidModel()
    {
        return new UserValidationModel
        {
            Username = "valid_user",
            Email = "user@example.com",
            Password = "StrongPass123!",
            PhoneNumber = "1234567890",
            Status = "active",
            IsActive = true,
        };
    }

    private sealed class UserValidationModelValidator : AbstractValidator<UserValidationModel>
    {
        public UserValidationModelValidator()
        {
            RuleFor(x => x.Username).ValidateUsername();
            RuleFor(x => x.Email).ValidateEmail();
            RuleFor(x => x.Password).ValidatePassword();
            RuleFor(x => x.PhoneNumber).ValidatePhoneNumber();
            RuleFor(x => x.Status).ValidateStatus();
            RuleFor(x => x.IsActive).ValidateIsActive();
        }
    }

    private sealed class UserValidationModel
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
