// -----------------------------------------------------------------------
// <copyright file="UpdateMyProfileCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;

public class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
  public UpdateMyProfileCommandValidator()
  {
    RuleFor(x => x)
        .Must(HaveAtLeastOneField)
        .WithMessage("Debe proporcionar el campo PhoneNumber para actualizar.");

    RuleFor(x => x.PhoneNumber).ValidatePhoneNumber();
  }

  private static bool HaveAtLeastOneField(UpdateMyProfileCommand command)
  {
    return command.PhoneNumber != null;
  }
}
