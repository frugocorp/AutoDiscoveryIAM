// -----------------------------------------------------------------------
// <copyright file="ExchangeLoginCodeCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode;

public class ExchangeLoginCodeCommandValidator : AbstractValidator<ExchangeLoginCodeCommand>
{
  public ExchangeLoginCodeCommandValidator()
  {
    RuleFor(x => x.Code).NotEmpty().WithMessage("El código de intercambio es requerido.");
  }
}
