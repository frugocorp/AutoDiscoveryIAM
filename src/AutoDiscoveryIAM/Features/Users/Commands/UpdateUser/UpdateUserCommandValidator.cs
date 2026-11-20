// -----------------------------------------------------------------------
// <copyright file="UpdateUserCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;

/// <summary>
/// Validador para el comando UpdateUserCommand.
/// </summary>
/// <remarks>
/// Define las reglas de validación para la actualización general de usuarios.
/// Utiliza FluentValidation para validar los datos de entrada.
/// </remarks>
public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
  /// <summary>
  /// Inicializa las reglas de validación para UpdateUserCommand.
  /// </summary>
  public UpdateUserCommandValidator()
  {
    RuleFor(x => x.PublicId)
        .NotEmpty()
        .WithMessage("PublicId es requerido");

    // Validaciones condicionales: al menos uno de los campos debe tener valor
    RuleFor(x => x)
        .Must(HaveAtLeastOneField)
        .WithMessage("Debe proporcionar al menos un campo para actualizar (Username, Email o Notes)");

    // Username opcional pero con validaciones si se proporciona
    When(x => !string.IsNullOrWhiteSpace(x.Request.Username), () =>
    {
      RuleFor(x => x.Request.Username)
          .Length(3, 50)
          .WithMessage("Username debe tener entre 3 y 50 caracteres")
          .Matches("^[a-zA-Z0-9._-]+$")
          .WithMessage("Username solo puede contener letras, números, puntos, guiones y guiones bajos");
    });

    // Email opcional pero con validaciones si se proporciona
    When(x => !string.IsNullOrWhiteSpace(x.Request.Email), () =>
    {
      RuleFor(x => x.Request.Email)
          .EmailAddress()
          .WithMessage("Email debe tener un formato válido")
          .MaximumLength(100)
          .WithMessage("Email no puede exceder 100 caracteres");
    });

    // Notes opcional con longitud máxima
    When(x => !string.IsNullOrWhiteSpace(x.Request.Notes), () =>
    {
      RuleFor(x => x.Request.Notes)
          .MaximumLength(500)
          .WithMessage("Notes no puede exceder 500 caracteres");
    });
  }

  /// <summary>
  /// Verifica que al menos un campo esté presente para actualizar.
  /// </summary>
  private static bool HaveAtLeastOneField(UpdateUserCommand command)
  {
    return !string.IsNullOrWhiteSpace(command.Request.Username) ||
           !string.IsNullOrWhiteSpace(command.Request.Email) ||
           !string.IsNullOrWhiteSpace(command.Request.Notes);
  }
}
