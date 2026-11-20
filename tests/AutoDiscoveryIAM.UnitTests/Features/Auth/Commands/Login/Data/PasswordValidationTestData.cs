// -----------------------------------------------------------------------
// <copyright file="PasswordValidationTestData.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login.Data;

/// <summary>
/// Datos de prueba específicos para validación de contraseñas con Argon2id.
/// Proporciona casos de hash/verify para testing exhaustivo del PasswordHasher.
/// </summary>
public static class PasswordValidationTestData
{
  /// <summary>
  /// Casos válidos para testing de hash de contraseñas.
  /// </summary>
  public static IEnumerable<object[]> ValidPasswordsForHashing => new List<object[]>
  {
    new object[] { "SecurePassword123!" },
    new object[] { "MyP@ssw0rd2024" },
    new object[] { "ComplexP@ss#123" },
    new object[] { "MinLen1!" }, // Longitud mínima válida
    new object[] { new string('a', 50) + "123!" }, // Contraseña larga
    new object[] { "🔒Password123" }, // Con emoji
    new object[] { "Contraseña123!" }, // Con tildes
    new object[] { "Pass Word 123!" }, // Con espacios
  };

  /// <summary>
  /// Casos inválidos para testing de validación de contraseñas.
  /// </summary>
  public static IEnumerable<object[]> InvalidPasswordsForHashing => new List<object[]>
  {
    new object[] { "" }, // Contraseña vacía
    new object[] { "   " }, // Solo espacios
    new object[] { null! }, // Contraseña nula
  };

  /// <summary>
  /// Pares de contraseña/hash conocidos para testing de verificación.
  /// Estos hashes fueron generados previamente con Argon2id (parámetros por defecto de Isopoh.Cryptography.Argon2).
  /// </summary>
  public static IEnumerable<object[]> KnownPasswordHashPairs => new List<object[]>
  {
    // Estos son hashes reales generados con Argon2id para testing
    new object[]
    {
      "testPassword123!",
      "$argon2id$v=19$m=65536,t=3,p=1$+c/b+dXyjgY80NZFu3r9JQ$mLx8z+8rcMz16iZIsmhYa0wzXdpzZyGCVG3TCCLcYJE", // Hash correcto
      true,
    },
    new object[]
    {
      "wrongPassword",
      "$argon2id$v=19$m=65536,t=3,p=1$+c/b+dXyjgY80NZFu3r9JQ$mLx8z+8rcMz16iZIsmhYa0wzXdpzZyGCVG3TCCLcYJE", // Mismo hash, password distinto
      false,
    },
    new object[]
    {
      "AnotherSecurePass!",
      "$argon2id$v=19$m=65536,t=3,p=1$RkzmSys4+akIR5UFGhEUbA$aljwGQMBJPRvT80e9EzbS39owrX2Vxni4LC+HUaY68U", // Hash correcto
      true,
    },
  };

  /// <summary>
  /// Casos de hashes malformados o vacíos que Argon2.Verify tolera devolviendo <c>false</c> (sin lanzar excepción).
  /// El caso de hash <c>null</c> se prueba por separado, ya que ese sí lanza <see cref="ArgumentNullException"/>.
  /// </summary>
  public static IEnumerable<object[]> MalformedHashCases => new List<object[]>
  {
    new object[] { "password123", "", "Hash vacío" },
    new object[] { "password123", "invalid_hash", "Hash inválido" },
    new object[] { "password123", "$argon2id$v=19$invalid", "Hash Argon2id malformado" },
    new object[] { "password123", "   ", "Hash solo espacios" },
    new object[] { "password123", "plaintext_not_hash", "Texto plano como hash" },
  };

  /// <summary>
  /// Generador para tests de performance de hashing.
  /// </summary>
  public static IEnumerable<object[]> PerformancePasswordCases
  {
    get
    {
      for (int i = 1; i <= 50; i++)
      {
        yield return new object[] { $"PerformancePassword{i}#Test!" };
      }
    }
  }
}
