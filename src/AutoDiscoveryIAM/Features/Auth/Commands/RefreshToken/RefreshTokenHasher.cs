// -----------------------------------------------------------------------
// <copyright file="RefreshTokenHasher.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Hash rápido y determinístico (SHA-256, no Argon2id/BCrypt) para refresh tokens,
/// que se persisten en BD como hash y se comparan por igualdad en cada lookup.
/// </summary>
public static class RefreshTokenHasher
{
  public static string Hash(string plaintextToken)
  {
    var bytes = Encoding.UTF8.GetBytes(plaintextToken);
    var hashBytes = SHA256.HashData(bytes);
    return Convert.ToHexString(hashBytes);
  }
}
