// -----------------------------------------------------------------------
// <copyright file="UpdateRoleByPublicIdCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FluentValidation;

/// <summary>
/// Validador para el comando <see cref="UpdateRoleByPublicIdCommand"/>.
/// </summary>
/// <remarks>
/// Utiliza las reglas centralizadas en <see cref="RoleValidationRules"/> para validar los datos del rol.
/// </remarks>
public class UpdateRoleByPublicIdCommandValidator : AbstractValidator<UpdateRoleByPublicIdCommand>
{
  /// <summary>
  /// Initializes a new instance of the <see cref="UpdateRoleByPublicIdCommandValidator"/> class.
  /// Inicializa una nueva instancia del validador para el comando de actualización de rol.
  /// </summary>
  /// <remarks>
  /// - Valida que el <c>PublicId</c> no sea vacío.
  /// - Valida <c>Name</c> y <c>Description</c> usando las reglas de <see cref="RoleValidationRules"/>.
  /// </remarks>
  public UpdateRoleByPublicIdCommandValidator()
  {
    RuleFor(x => x.PublicId)
      .NotEmpty().WithMessage(RoleValidationTypes.PublicIdRequired.Message);

    RuleFor(x => x.Request.Name).ValidateRoleName();
    RuleFor(x => x.Request.Description).ValidateRoleDescription();
  }
}
