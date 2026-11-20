// -----------------------------------------------------------------------
// <copyright file="RequestEmailChangeCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange;

public class RequestEmailChangeCommandValidator : AbstractValidator<RequestEmailChangeCommand>
{
  public RequestEmailChangeCommandValidator()
  {
    RuleFor(x => x.NewEmail).ValidateEmail();
  }
}
