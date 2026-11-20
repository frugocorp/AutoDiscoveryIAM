// -----------------------------------------------------------------------
// <copyright file="PasswordPolicyOptions.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;

/// <summary>
/// Política de complejidad de contraseña configurable, bindeada desde la sección
/// <c>PasswordStruct</c> de <c>appsettings.json</c>. Se suma a la validación base de
/// longitud (<see cref="FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation.UserValidationTypes.PasswordMinLength"/>/
/// <c>PasswordMaxLength</c>), que no se puede desactivar desde aquí.
/// </summary>
/// <remarks>
/// Con <see cref="Regex"/> vacío (default), no se exige ninguna complejidad adicional — un fork
/// que no configure esta sección solo valida longitud.
/// </remarks>
public class PasswordPolicyOptions
{
  /// <summary>
  /// Expresión regular adicional que la contraseña debe cumplir. Vacío/null = sin regla de
  /// complejidad adicional.
  /// </summary>
  public string? Regex { get; set; }

  /// <summary>
  /// Descripción de la política, pensada para mostrarse en documentación/Swagger.
  /// </summary>
  public string Description { get; set; } = "Contraseña de al menos 8 caracteres.";

  public PasswordPolicyMessages Message { get; set; } = new();
}

/// <summary>
/// Mensajes asociados a la política de contraseña configurable.
/// </summary>
public class PasswordPolicyMessages
{
  public string Valid { get; set; } = "Contraseña válida.";

  public string Invalid { get; set; } = "Contraseña inválida.";
}
