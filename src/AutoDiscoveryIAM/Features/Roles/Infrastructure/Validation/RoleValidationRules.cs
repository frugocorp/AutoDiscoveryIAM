// -----------------------------------------------------------------------
// <copyright file="RoleValidationRules.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;

/// <summary>
/// Contiene las reglas de validación estándar y centralizadas para las entidades Role del sistema RBAC.
/// </summary>
/// <remarks>
/// Estas reglas son utilizadas por los validadores de comandos y DTOs relacionados con Role.
/// Si se requiere modificar la validación de nombres, descripciones o identificadores, este es el único lugar a actualizar.
/// </remarks>
public static class RoleValidationRules
{
  /// <summary>
  /// Regla de validación para el nombre del rol (campo crítico del RBAC).
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <returns>Reglas encadenadas para el nombre del rol.</returns>
  /// <remarks>
  /// - Requerido (no vacío)
  /// - Mínimo 2 caracteres
  /// - Máximo 100 caracteres
  /// - Solo letras, números, espacios, guiones bajos y guiones.
  /// </remarks>
  public static IRuleBuilderOptions<T, string> ValidateRoleName<T>(this IRuleBuilder<T, string> ruleBuilder)
  {
    return ruleBuilder
        .NotEmpty().WithMessage(RoleValidationTypes.NameRequired.Message)
        .MinimumLength((int)RoleValidationTypes.NameMinLength.Args[0]).WithMessage(RoleValidationTypes.NameMinLength.Message)
        .MaximumLength((int)RoleValidationTypes.NameMaxLength.Args[0]).WithMessage(RoleValidationTypes.NameMaxLength.Message)
        .Matches(RoleValidationTypes.NameInvalidChars.Regex!).WithMessage(RoleValidationTypes.NameInvalidChars.Message);
  }

  /// <summary>
  /// Regla de validación para la descripción del rol.
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <returns>Reglas encadenadas para la descripción.</returns>
  public static IRuleBuilderOptions<T, string?> ValidateRoleDescription<T>(this IRuleBuilder<T, string?> ruleBuilder)
  {
    return ruleBuilder
        .MaximumLength((int)RoleValidationTypes.DescriptionMaxLength.Args[0]).WithMessage(RoleValidationTypes.DescriptionMaxLength.Message);
  }

  /// <summary>
  /// Regla de validación para el ID del rol.
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <returns>Reglas encadenadas para el ID.</returns>
  public static IRuleBuilderOptions<T, int> ValidateRoleId<T>(this IRuleBuilder<T, int> ruleBuilder)
  {
    return ruleBuilder
        .NotEmpty().WithMessage(RoleValidationTypes.IdRequired.Message)
        .GreaterThan((int)RoleValidationTypes.IdGreaterThanZero.Args[0]).WithMessage(RoleValidationTypes.IdGreaterThanZero.Message);
  }

  /// <summary>
  /// Regla de validación para el PublicId (GUID) del rol.
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <returns>Reglas encadenadas para el PublicId.</returns>
  public static IRuleBuilderOptions<T, Guid> ValidateRolePublicId<T>(this IRuleBuilder<T, Guid> ruleBuilder)
  {
    return ruleBuilder
        .NotEmpty().WithMessage(RoleValidationTypes.PublicIdRequired.Message)
        .Must(guid => guid != Guid.Empty).WithMessage(RoleValidationTypes.PublicIdRequired.Message);
  }
}
