// -----------------------------------------------------------------------
// <copyright file="AuthValidationRules.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Validation;

public static class AuthValidationRules
{
  public static IRuleBuilderOptions<T, string> ValidateUsername<T>(
      this IRuleBuilder<T, string> ruleBuilder)
  {
    return ruleBuilder
        .NotEmpty().WithMessage(AuthValidationTypes.UsernameRequired.Message)
        .MinimumLength((int)AuthValidationTypes.UsernameMinLength.Args[0]).WithMessage(AuthValidationTypes.UsernameMinLength.Message)
        .MaximumLength((int)AuthValidationTypes.UsernameMaxLength.Args[0]).WithMessage(AuthValidationTypes.UsernameMaxLength.Message);
  }

  public static IRuleBuilderOptions<T, string> ValidatePassword<T>(
      this IRuleBuilder<T, string> ruleBuilder)
  {
    return ruleBuilder
        .NotEmpty().WithMessage(AuthValidationTypes.PasswordRequired.Message)
        .MinimumLength((int)AuthValidationTypes.PasswordMinLength.Args[0]).WithMessage(AuthValidationTypes.PasswordMinLength.Message)
        .MaximumLength((int)AuthValidationTypes.PasswordMaxLength.Args[0]).WithMessage(AuthValidationTypes.PasswordMaxLength.Message)
        .Matches(AuthValidationTypes.PasswordUppercase.Regex!).WithMessage(AuthValidationTypes.PasswordUppercase.Message)
        .Matches(AuthValidationTypes.PasswordLowercase.Regex!).WithMessage(AuthValidationTypes.PasswordLowercase.Message)
        .Matches(AuthValidationTypes.PasswordDigit.Regex!).WithMessage(AuthValidationTypes.PasswordDigit.Message)
        .Matches(AuthValidationTypes.PasswordSpecialChar.Regex!).WithMessage(AuthValidationTypes.PasswordSpecialChar.Message);
  }

  public static IRuleBuilderOptions<T, string> ValidateEmail<T>(
      this IRuleBuilder<T, string> ruleBuilder)
  {
    return ruleBuilder
        .NotEmpty().WithMessage(AuthValidationTypes.EmailRequired.Message)
        .EmailAddress().WithMessage(AuthValidationTypes.EmailFormat.Message)
        .MaximumLength((int)AuthValidationTypes.EmailMaxLength.Args[0]).WithMessage(AuthValidationTypes.EmailMaxLength.Message);
  }
}
