// -----------------------------------------------------------------------
// <copyright file="CreateUserByAdminCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUserByAdmin;

/// <summary>
/// Mismas reglas de usuario, correo y contraseña que el registro público.
/// </summary>
public class CreateUserByAdminCommandValidator : AbstractValidator<CreateUserByAdminCommand>
{
  /// <summary>
  /// Inicializa una nueva instancia de <see cref="CreateUserByAdminCommandValidator"/>.
  /// </summary>
  /// <param name="passwordPolicyOptions">Política de contraseñas configurada.</param>
  public CreateUserByAdminCommandValidator(IOptions<PasswordPolicyOptions>? passwordPolicyOptions = null)
  {
    RuleFor(x => x.Username).ValidateUsername();
    RuleFor(x => x.Email).ValidateEmail();
    // Sin contraseña es una invitación: el usuario la define con el enlace del correo.
    RuleFor(x => x.Password!).ValidatePassword(passwordPolicyOptions?.Value).When(x => x.Password != null);
    RuleFor(x => x.MustChangePassword)
      .NotEqual(true)
      .When(x => x.Password == null)
      .WithErrorCode(UserValidationTypes.MustChangePasswordRequiresPassword.Code)
      .WithMessage(UserValidationTypes.MustChangePasswordRequiresPassword.Message);
  }
}
