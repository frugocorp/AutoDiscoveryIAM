// -----------------------------------------------------------------------
// <copyright file="PasswordHasher.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Isopoh.Cryptography.Argon2;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;

public class PasswordHasher : IPasswordHasher
{
  /// <summary>
  /// Longitud máxima que se llega a verificar. Ninguna contraseña válida la alcanza (la política
  /// permite mucho menos), y los campos de contraseña actual no tienen validador de longitud: sin
  /// este tope, un cuerpo de varios MB obligaría a procesar Argon2id sobre todo ese texto.
  /// </summary>
  internal const int MaxVerifiableLength = 1024;

  public string HashPassword(string password)
  {
    // Usa Argon2id con parámetros seguros por defecto
    return Argon2.Hash(password);
  }

  public bool VerifyPassword(string password, string hashedPassword)
  {
    ArgumentNullException.ThrowIfNull(password);

    if (password.Length > MaxVerifiableLength)
    {
      return false;
    }

    return Argon2.Verify(hashedPassword, password);
  }
}
