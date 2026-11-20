// -----------------------------------------------------------------------
// <copyright file="OneTimeTokenHasher.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;

/// <summary>
/// Hash de los tokens de un solo uso que viajan por correo (activación de cuenta y restablecimiento
/// de contraseña). En la base de datos solo se guarda el hash: quien la lea (respaldo filtrado,
/// réplica de reportes, inyección SQL) no puede usar el enlace pendiente de nadie.
/// </summary>
/// <remarks>
/// SHA-256 y no Argon2id: el token ya es aleatorio y largo (no se puede adivinar por diccionario), y el
/// hash tiene que ser determinístico para buscarlo por igualdad.
/// </remarks>
public static class OneTimeTokenHasher
{
  public static string Hash(string plaintextToken)
  {
    ArgumentNullException.ThrowIfNull(plaintextToken);
    return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plaintextToken)));
  }

  /// <summary>
  /// Compara en tiempo constante el token recibido contra el hash guardado.
  /// </summary>
  public static bool Matches(string? storedHash, string? plaintextToken)
  {
    if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(plaintextToken))
    {
      return false;
    }

    return CryptographicOperations.FixedTimeEquals(
      Encoding.ASCII.GetBytes(storedHash),
      Encoding.ASCII.GetBytes(Hash(plaintextToken)));
  }
}
