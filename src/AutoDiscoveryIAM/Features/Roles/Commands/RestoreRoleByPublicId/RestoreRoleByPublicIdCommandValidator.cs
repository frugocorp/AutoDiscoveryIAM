// -----------------------------------------------------------------------
// <copyright file="RestoreRoleByPublicIdCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;

/// <summary>
/// Validador para el comando RestoreRoleByPublicIdCommand.
/// </summary>
/// <remarks>
/// Aplica reglas de validación al comando de restauración, incluyendo:
/// - PublicId requerido (la validación de formato GUID la maneja automáticamente .NET)
/// Utiliza RoleValidationTypes para consistencia en mensajes de error.
/// </remarks>
public class RestoreRoleByPublicIdCommandValidator : AbstractValidator<RestoreRoleByPublicIdCommand>
{
    public RestoreRoleByPublicIdCommandValidator()
    {
        RuleFor(x => x.PublicId)
            .NotEmpty().WithMessage(RoleValidationTypes.PublicIdRequired.Message);
    }
}