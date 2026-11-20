// -----------------------------------------------------------------------
// <copyright file="VerifyTwoFactorCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor;

public class VerifyTwoFactorCommandValidator : AbstractValidator<VerifyTwoFactorCommand>
{
  public VerifyTwoFactorCommandValidator()
  {
    RuleFor(x => x.ChallengeToken)
      .NotEmpty()
      .WithMessage("El token de desafio es requerido.");

    RuleFor(x => x.Code)
      .NotEmpty()
      .WithMessage("El codigo de verificacion es requerido.")
      .Length(4, 16)
      .WithMessage("El codigo de verificacion debe tener entre 4 y 16 caracteres.");
  }
}
