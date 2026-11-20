// -----------------------------------------------------------------------
// <copyright file="CreateUserCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
  public CreateUserCommandValidator(IOptions<PasswordPolicyOptions>? passwordPolicyOptions = null)
  {
    RuleFor(x => x.Username).ValidateUsername();
    RuleFor(x => x.Email).ValidateEmail();
    RuleFor(x => x.Password).ValidatePassword(passwordPolicyOptions?.Value);
  }
}
