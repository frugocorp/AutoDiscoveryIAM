// -----------------------------------------------------------------------
// <copyright file="RefreshTokenCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Validador para el comando RefreshTokenCommand.
/// </summary>
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
  public RefreshTokenCommandValidator()
  {
    RuleFor(x => x.RefreshToken)
      .NotEmpty()
      .WithMessage("El refresh token es requerido.")
      .Length(10, 500)
      .WithMessage("El refresh token debe tener entre 10 y 500 caracteres.");

    RuleFor(x => x.IpAddress)
      .MaximumLength(45)
      .WithMessage("La dirección IP no puede exceder 45 caracteres.")
      .When(x => !string.IsNullOrEmpty(x.IpAddress));

    RuleFor(x => x.UserAgent)
      .MaximumLength(500)
      .WithMessage("El User-Agent no puede exceder 500 caracteres.")
      .When(x => !string.IsNullOrEmpty(x.UserAgent));
  }
}
