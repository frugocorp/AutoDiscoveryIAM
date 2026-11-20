// -----------------------------------------------------------------------
// <copyright file="ExternalLoginExchangeCodeHasher.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External;

/// <summary>
/// Hash rápido y determinístico (SHA-256, no Argon2id/BCrypt) para el código de intercambio de
/// login externo -- mismo motivo y misma técnica que
/// <see cref="FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken.RefreshTokenHasher"/>
/// para refresh tokens: se persiste como hash y se compara por igualdad en cada canje, nunca en claro.
/// </summary>
public static class ExternalLoginExchangeCodeHasher
{
  public static string Hash(string plaintextCode)
  {
    var bytes = Encoding.UTF8.GetBytes(plaintextCode);
    var hashBytes = SHA256.HashData(bytes);
    return Convert.ToHexString(hashBytes);
  }
}
