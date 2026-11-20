using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequestDto>
{
  public ChangePasswordRequestValidator(IOptions<PasswordPolicyOptions>? passwordPolicyOptions = null)
  {
    RuleFor(x => x.NewPassword).ValidatePassword(passwordPolicyOptions?.Value);
  }
}

public class RequestPasswordResetRequestValidator : AbstractValidator<RequestPasswordResetRequestDto>
{
  public RequestPasswordResetRequestValidator()
  {
    RuleFor(x => x.Email).ValidateEmail();
  }
}

public class ConfirmPasswordResetRequestValidator : AbstractValidator<ConfirmPasswordResetRequestDto>
{
  public ConfirmPasswordResetRequestValidator(IOptions<PasswordPolicyOptions>? passwordPolicyOptions = null)
  {
    RuleFor(x => x.Token)
      .NotEmpty().WithMessage(UserValidationTypes.ResetTokenRequired.Message);
    RuleFor(x => x.NewPassword).ValidatePassword(passwordPolicyOptions?.Value);
  }
}
