// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsCommandValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;

/// <summary>
/// Validador para el comando SyncPermissionsCommand.
/// </summary>
/// <remarks>
/// Utiliza las reglas centralizadas en <see cref="PermissionValidationRules"/> para validar los datos del comando.
/// Valida que los parámetros del comando sean correctos antes de la ejecución.
/// </remarks>
public class SyncPermissionsCommandValidator : AbstractValidator<SyncPermissionsCommand>
{
    /// <summary>
    /// Inicializa una nueva instancia de SyncPermissionsCommandValidator.
    /// </summary>
    /// <remarks>
    /// - Valida que el <c>Request</c> no sea nulo.
    /// - Valida la lógica de negocio usando las reglas de <see cref="PermissionValidationRules"/>.
    /// </remarks>
    public SyncPermissionsCommandValidator()
    {
        RuleFor(x => x.Request).ValidateSyncRequest();
    }
}
