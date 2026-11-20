// -----------------------------------------------------------------------
// <copyright file="TwoFactorCodeGenerator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;

public interface ITwoFactorCodeGenerator
{
  string GenerateChallengeToken();

  string GenerateNumericCode(int length);
}

/// <summary>
/// Genera el token de desafío de login (opaco) y el código numérico de un solo uso (método email).
/// </summary>
public sealed class TwoFactorCodeGenerator : ITwoFactorCodeGenerator
{
  public string GenerateChallengeToken()
  {
    var buffer = RandomNumberGenerator.GetBytes(32);
    return Convert.ToBase64String(buffer)
      .Replace("+", "-")
      .Replace("/", "_")
      .Replace("=", string.Empty);
  }

  public string GenerateNumericCode(int length)
  {
    var effectiveLength = Math.Max(length, 4);
    var maxExclusive = (int)Math.Pow(10, effectiveLength);
    var code = RandomNumberGenerator.GetInt32(maxExclusive);
    return code.ToString(new string('0', effectiveLength), CultureInfo.InvariantCulture);
  }
}
