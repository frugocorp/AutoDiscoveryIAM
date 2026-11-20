// -----------------------------------------------------------------------
// <copyright file="LogoutCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;

/// <summary>
/// Validador para el comando LogoutCommand.
/// </summary>
public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
  public LogoutCommandValidator()
  {
    // Si RevokeAllTokens es false, debe proporcionar un refresh token válido
    RuleFor(x => x.RefreshToken)
      .NotEmpty()
      .WithMessage("Debe proporcionar un refresh token válido cuando RevokeAllTokens es false.")
      .When(x => !x.RevokeAllTokens);

    // Si RevokeAllTokens es true, debe estar autenticado
    RuleFor(x => x.UserId)
      .NotEmpty()
      .WithMessage("Debe estar autenticado para revocar todos los tokens.")
      .When(x => x.RevokeAllTokens);

    RuleFor(x => x.RefreshToken)
      .Length(10, 500)
      .WithMessage("El refresh token debe tener entre 10 y 500 caracteres.")
      .When(x => !string.IsNullOrEmpty(x.RefreshToken));

    RuleFor(x => x.UserId)
      .GreaterThan(0)
      .WithMessage("El ID de usuario debe ser mayor a 0.")
      .When(x => x.UserId.HasValue);

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
