// -----------------------------------------------------------------------
// <copyright file="PermissionValidationRules.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Validation;

/// <summary>
/// Contiene las reglas de validación estándar y centralizadas para las entidades Permission del sistema RBAC.
/// </summary>
/// <remarks>
/// Estas reglas son utilizadas por los validadores de comandos y DTOs relacionados con Permission.
/// Si se requiere modificar la validación de claves, formatos o metadatos, este es el único lugar a actualizar.
/// </remarks>
public static class PermissionValidationRules
{
  /// <summary>
  /// Regla de validación para el request de SyncPermissions.
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <returns>Reglas encadenadas para el request.</returns>
  public static IRuleBuilderOptions<T, SyncPermissionsRequestDto> ValidateSyncRequest<T>(this IRuleBuilder<T, SyncPermissionsRequestDto> ruleBuilder)
  {
    return ruleBuilder
        .NotNull().WithMessage(PermissionValidationTypes.RequestRequired.Message)
        .Must(request => request == null || !request.DryRun || !request.ForceFullSync)
        .WithMessage(PermissionValidationTypes.ForceFullSyncConflictWithDryRun.Message);
  }
  /// <summary>
  /// Regla de validación para la clave del permiso (campo crítico del RBAC).
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <returns>Reglas encadenadas para la clave del permiso.</returns>
  /// <remarks>
  /// - Requerido (no vacío)
  /// - Mínimo 3 caracteres
  /// - Máximo 200 caracteres
  /// - Formato {feature}.{action_snake_case} o wildcards válidos.
  /// </remarks>
  public static IRuleBuilderOptions<T, string> ValidatePermissionKey<T>(this IRuleBuilder<T, string> ruleBuilder)
  {
    return ruleBuilder
        .NotEmpty().WithMessage(PermissionValidationTypes.KeyRequired.Message)
        .MinimumLength((int)PermissionValidationTypes.KeyMinLength.Args[0]).WithMessage(PermissionValidationTypes.KeyMinLength.Message)
        .MaximumLength((int)PermissionValidationTypes.KeyMaxLength.Args[0]).WithMessage(PermissionValidationTypes.KeyMaxLength.Message)
        .Must(BeValidPermissionKeyFormat).WithMessage(PermissionValidationTypes.KeyInvalidFormat.Message);
  }

  /// <summary>
  /// Regla de validación para la descripción del permiso.
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <returns>Reglas encadenadas para la descripción.</returns>
  public static IRuleBuilderOptions<T, string?> ValidatePermissionDescription<T>(this IRuleBuilder<T, string?> ruleBuilder)
  {
    return ruleBuilder
        .MaximumLength((int)PermissionValidationTypes.DescriptionMaxLength.Args[0]).WithMessage(PermissionValidationTypes.DescriptionMaxLength.Message);
  }

  /// <summary>
  /// Regla de validación para el grupo del permiso.
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <returns>Reglas encadenadas para el grupo.</returns>
  public static IRuleBuilderOptions<T, string?> ValidatePermissionGroup<T>(this IRuleBuilder<T, string?> ruleBuilder)
  {
    return ruleBuilder
        .MaximumLength((int)PermissionValidationTypes.GroupMaxLength.Args[0]).WithMessage(PermissionValidationTypes.GroupMaxLength.Message);
  }

  /// <summary>
  /// Regla de validación para la ruta HTTP del permiso.
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <returns>Reglas encadenadas para la ruta.</returns>
  public static IRuleBuilderOptions<T, string?> ValidatePermissionRoute<T>(this IRuleBuilder<T, string?> ruleBuilder)
  {
    return ruleBuilder
        .MaximumLength((int)PermissionValidationTypes.RouteMaxLength.Args[0]).WithMessage(PermissionValidationTypes.RouteMaxLength.Message);
  }

  /// <summary>
  /// Regla de validación para el método HTTP del permiso.
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <returns>Reglas encadenadas para el método HTTP.</returns>
  public static IRuleBuilderOptions<T, string?> ValidatePermissionHttpMethod<T>(this IRuleBuilder<T, string?> ruleBuilder)
  {
    return ruleBuilder
        .Must(method => method == null || string.IsNullOrWhiteSpace(method) || System.Text.RegularExpressions.Regex.IsMatch(method, PermissionValidationTypes.HttpMethodInvalid.Regex!))
        .WithMessage(PermissionValidationTypes.HttpMethodInvalid.Message);
  }

  /// <summary>
  /// Regla de validación para el tipo CQRS del permiso.
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <returns>Reglas encadenadas para el tipo CQRS.</returns>
  public static IRuleBuilderOptions<T, string?> ValidatePermissionCqrsType<T>(this IRuleBuilder<T, string?> ruleBuilder)
  {
    return ruleBuilder
        .Must(cqrsType => cqrsType == null || string.IsNullOrWhiteSpace(cqrsType) || System.Text.RegularExpressions.Regex.IsMatch(cqrsType, PermissionValidationTypes.CqrsTypeInvalid.Regex!))
        .WithMessage(PermissionValidationTypes.CqrsTypeInvalid.Message);
  }

  // Métodos privados de validación personalizada

  /// <summary>
  /// Valida que la clave del permiso tenga el formato correcto o sea un wildcard válido.
  /// </summary>
  /// <param name="key">La clave del permiso a validar.</param>
  /// <returns>True si la clave es válida, False en caso contrario.</returns>
  private static bool BeValidPermissionKeyFormat(string key)
  {
    if (string.IsNullOrWhiteSpace(key))
    {
      return false;
    }

    // Formato normal: {feature}.{action_snake_case}
    if (System.Text.RegularExpressions.Regex.IsMatch(key, PermissionValidationTypes.KeyInvalidFormat.Regex!))
    {
      return true;
    }

    // Formato wildcard: {feature}.* o *.{action}
    if (key.Contains('*', StringComparison.Ordinal) &&
        System.Text.RegularExpressions.Regex.IsMatch(key, PermissionValidationTypes.KeyWildcardInvalid.Regex!))
    {
      return true;
    }

    return false;
  }
}
