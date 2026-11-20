using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;

/// <summary>
/// Contiene las reglas de validación estándar y centralizadas para las entidades User.
/// </summary>
/// <remarks>
/// Estas reglas son utilizadas por los validadores de comandos y DTOs relacionados con User.
/// Si se requiere modificar la validación de username, email, etc., este es el único lugar a actualizar.
/// </remarks>
public static class UserValidationRules
{
  /// <summary>
  /// Regla de validación para el nombre de usuario.
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <returns>Reglas encadenadas para el username.</returns>
  /// <remarks>
  /// - Requerido (no vacío)
  /// - Mínimo 3 caracteres
  /// - Máximo 100 caracteres
  /// - Solo letras, números, guiones, guion bajo y puntos.
  /// </remarks>
  public static IRuleBuilderOptions<T, string> ValidateUsername<T>(this IRuleBuilder<T, string> ruleBuilder)
  {
    return ruleBuilder
        .NotEmpty().WithMessage(UserValidationTypes.UsernameRequired.Message)
        .MinimumLength((int)UserValidationTypes.UsernameMinLength.Args[0]).WithMessage(UserValidationTypes.UsernameMinLength.Message)
        .MaximumLength((int)UserValidationTypes.UsernameMaxLength.Args[0]).WithMessage(UserValidationTypes.UsernameMaxLength.Message)
        .Matches(UserValidationTypes.UsernameInvalidChars.Regex).WithMessage(UserValidationTypes.UsernameInvalidChars.Message);
  }

  /// <summary>
  /// Regla de validación para el email del usuario.
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <returns>Reglas encadenadas para el email.</returns>
  public static IRuleBuilderOptions<T, string> ValidateEmail<T>(this IRuleBuilder<T, string> ruleBuilder)
  {
    return ruleBuilder
        .NotEmpty().WithMessage(UserValidationTypes.EmailRequired.Message)
        .MaximumLength((int)UserValidationTypes.EmailMaxLength.Args[0]).WithMessage(UserValidationTypes.EmailMaxLength.Message)
        .EmailAddress().WithMessage(UserValidationTypes.EmailInvalid.Message);
  }

  /// <summary>
  /// Regla de validación para el password del usuario.
  /// </summary>
  /// <typeparam name="T">Tipo del objeto que contiene la propiedad.</typeparam>
  /// <param name="ruleBuilder">Builder de reglas de FluentValidation.</param>
  /// <param name="policy">
  /// Política de complejidad configurable (sección <c>PasswordStruct</c>). Si es
  /// <c>null</c> o no trae <see cref="PasswordPolicyOptions.Regex"/>, solo se valida longitud
  /// — un fork que no configure la sección no ve cambios.
  /// </param>
  /// <returns>Reglas encadenadas para el password.</returns>
  public static IRuleBuilderOptions<T, string> ValidatePassword<T>(this IRuleBuilder<T, string> ruleBuilder, PasswordPolicyOptions? policy = null)
  {
    var rule = ruleBuilder
        .NotEmpty().WithMessage(UserValidationTypes.PasswordRequired.Message)
        .MinimumLength((int)UserValidationTypes.PasswordMinLength.Args[0]).WithMessage(UserValidationTypes.PasswordMinLength.Message)
        .MaximumLength((int)UserValidationTypes.PasswordMaxLength.Args[0]).WithMessage(UserValidationTypes.PasswordMaxLength.Message);

    if (!string.IsNullOrEmpty(policy?.Regex))
    {
      rule = rule.Matches(policy.Regex).WithMessage(policy.Message.Invalid);
    }

    return rule;
  }

  /// <summary>
  /// Regla de validación para el número de teléfono (PhoneNumber).
  /// </summary>
  public static IRuleBuilderOptions<T, string?> ValidatePhoneNumber<T>(this IRuleBuilder<T, string?> ruleBuilder)
  {
    return ruleBuilder
        .MaximumLength((int)UserValidationTypes.PhoneNumberMaxLength.Args[0]).WithMessage(UserValidationTypes.PhoneNumberMaxLength.Message);
  }

  /// <summary>
  /// Regla de validación para el estado (Status).
  /// </summary>
  public static IRuleBuilderOptions<T, string> ValidateStatus<T>(this IRuleBuilder<T, string> ruleBuilder)
  {
    return ruleBuilder
        .NotEmpty().WithMessage(UserValidationTypes.StatusRequired.Message)
        .Must(s => UserValidationTypes.StatusAllowedValues.Contains(s)).WithMessage(UserValidationTypes.StatusInvalid.Message);
  }

  /// <summary>
  /// Regla de validación para el campo IsActive (bool).
  /// </summary>
  public static IRuleBuilderOptions<T, bool> ValidateIsActive<T>(this IRuleBuilder<T, bool> ruleBuilder)
  {
    return ruleBuilder.Must(v => v == true || v == false);
  }
}
