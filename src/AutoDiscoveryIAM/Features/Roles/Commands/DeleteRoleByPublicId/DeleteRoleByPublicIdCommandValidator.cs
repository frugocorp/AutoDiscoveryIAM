// -----------------------------------------------------------------------
// <copyright file="DeleteRoleByPublicIdCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;

/// <summary>
/// Validador para el comando DeleteRoleByPublicIdCommand.
/// </summary>
/// <remarks>
/// Aplica reglas de validación al comando de eliminación, incluyendo:
/// - PublicId requerido (la validación de formato GUID la maneja automáticamente .NET)
/// Utiliza RoleValidationTypes para consistencia en mensajes de error.
/// </remarks>
public class DeleteRoleByPublicIdCommandValidator : AbstractValidator<DeleteRoleByPublicIdCommand>
{
  public DeleteRoleByPublicIdCommandValidator()
  {
    RuleFor(x => x.PublicId)
        .NotEmpty().WithMessage(RoleValidationTypes.PublicIdRequired.Message);
  }
}