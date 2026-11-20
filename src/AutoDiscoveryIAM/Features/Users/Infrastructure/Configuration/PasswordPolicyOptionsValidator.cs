// -----------------------------------------------------------------------
// <copyright file="PasswordPolicyOptionsValidator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Configuration;

/// <summary>
/// Valida <see cref="PasswordPolicyOptions"/> al arranque (<c>ValidateOnStart</c>): si se configura
/// <see cref="PasswordPolicyOptions.Regex"/>, debe ser una expresión regular compilable.
/// </summary>
public class PasswordPolicyOptionsValidator : IValidateOptions<PasswordPolicyOptions>
{
  /// <inheritdoc />
  public ValidateOptionsResult Validate(string? name, PasswordPolicyOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    if (string.IsNullOrEmpty(options.Regex))
    {
      return ValidateOptionsResult.Success;
    }

    try
    {
      _ = new Regex(options.Regex);
    }
    catch (ArgumentException ex)
    {
      return ValidateOptionsResult.Fail($"PasswordStruct:regex no es una expresión regular válida: {ex.Message}");
    }

    return ValidateOptionsResult.Success;
  }
}
