// -----------------------------------------------------------------------
// <copyright file="AuthSecuritySettingsValidators.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;

/// <summary>
/// Valida al arranque la sección <c>Lockout</c>: un máximo de intentos o una duración de bloqueo
/// menores a 1 dejarían la protección contra fuerza bruta rota sin ningún aviso.
/// </summary>
internal sealed class LockoutSettingsValidator : IValidateOptions<LockoutSettings>
{
  public ValidateOptionsResult Validate(string? name, LockoutSettings options)
  {
    ArgumentNullException.ThrowIfNull(options);

    var errors = new List<string>();
    if (options.MaxFailedAttempts < 1)
    {
      errors.Add($"Lockout:MaxFailedAttempts debe ser al menos 1 (tiene {options.MaxFailedAttempts}).");
    }

    if (options.LockoutMinutes < 1)
    {
      errors.Add($"Lockout:LockoutMinutes debe ser al menos 1 (tiene {options.LockoutMinutes}).");
    }

    return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
  }
}

/// <summary>
/// Valida al arranque la sección <c>TwoFactor</c>: un código de menos de 6 dígitos es adivinable
/// antes del bloqueo, y uno de más de 9 no cabe en el generador.
/// </summary>
internal sealed class TwoFactorSettingsValidator : IValidateOptions<TwoFactorSettings>
{
  private const int MinimumCodeLength = 6;
  private const int MaximumCodeLength = 9;

  public ValidateOptionsResult Validate(string? name, TwoFactorSettings options)
  {
    ArgumentNullException.ThrowIfNull(options);

    var errors = new List<string>();
    if (options.CodeLength is < MinimumCodeLength or > MaximumCodeLength)
    {
      errors.Add($"TwoFactor:CodeLength debe estar entre {MinimumCodeLength} y {MaximumCodeLength} (tiene {options.CodeLength}).");
    }

    if (options.CodeExpiryMinutes < 1)
    {
      errors.Add($"TwoFactor:CodeExpiryMinutes debe ser al menos 1 (tiene {options.CodeExpiryMinutes}).");
    }

    if (options.ChallengeTokenExpiryMinutes < 1)
    {
      errors.Add($"TwoFactor:ChallengeTokenExpiryMinutes debe ser al menos 1 (tiene {options.ChallengeTokenExpiryMinutes}).");
    }

    return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
  }
}

/// <summary>
/// Valida al arranque la sección <c>RefreshTokenSettings</c>: un token de menos de 32 bytes (256 bits)
/// pierde la resistencia a adivinarlo por fuerza bruta.
/// </summary>
internal sealed class RefreshTokenSettingsValidator : IValidateOptions<RefreshTokenSettings>
{
  private const int MinimumTokenLength = 32;

  public ValidateOptionsResult Validate(string? name, RefreshTokenSettings options)
  {
    ArgumentNullException.ThrowIfNull(options);

    var errors = new List<string>();
    if (options.TokenLength < MinimumTokenLength)
    {
      errors.Add($"RefreshTokenSettings:TokenLength debe ser al menos {MinimumTokenLength} bytes (tiene {options.TokenLength}).");
    }

    if (options.ExpiryDays < 1)
    {
      errors.Add($"RefreshTokenSettings:ExpiryDays debe ser al menos 1 (tiene {options.ExpiryDays}).");
    }

    return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
  }
}
