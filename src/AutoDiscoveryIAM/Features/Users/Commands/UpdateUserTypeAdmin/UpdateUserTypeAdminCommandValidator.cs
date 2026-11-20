// -----------------------------------------------------------------------
// <copyright file="UpdateUserTypeAdminCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;

/// <summary>
/// Validador para el comando <see cref="UpdateUserTypeAdminCommand"/>.
/// </summary>
/// <remarks>
/// Valida los datos necesarios para cambiar el tipo Admin de un usuario.
/// NOTA: Este validador usa FluentValidation que está marcado para eliminación futura.
/// </remarks>
public class UpdateUserTypeAdminCommandValidator : AbstractValidator<UpdateUserTypeAdminCommand>
{
  /// <summary>
  /// Inicializa una nueva instancia del validador para el comando de cambio de tipo Admin.
  /// </summary>
  /// <remarks>
  /// - Valida que el <c>PublicId</c> no sea vacío.
  /// - Valida que <c>IsAdmin</c> esté presente en el request.
  /// - Valida que <c>Reason</c> no exceda la longitud máxima si se proporciona.
  /// </remarks>
  public UpdateUserTypeAdminCommandValidator()
  {
    RuleFor(x => x.PublicId)
      .NotEmpty().WithMessage("El PublicId del usuario es requerido");

    RuleFor(x => x.Request.IsAdmin)
      .NotNull().WithMessage("El campo IsAdmin es requerido");

    RuleFor(x => x.Request.Reason)
      .MaximumLength(500).WithMessage("La razón no puede exceder los 500 caracteres")
      .When(x => !string.IsNullOrEmpty(x.Request.Reason));
  }
}
