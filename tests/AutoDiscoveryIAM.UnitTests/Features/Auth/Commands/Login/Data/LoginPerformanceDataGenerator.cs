// -----------------------------------------------------------------------
// <copyright file="LoginPerformanceDataGenerator.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login.Data;

/// <summary>
/// Generador de datos para tests de performance del sistema de login.
/// Genera 1000+ combinaciones de credenciales para validar rendimiento.
/// </summary>
public static class LoginPerformanceDataGenerator
{
  /// <summary>
  /// Genera 1000+ casos de test para validar performance del sistema de login.
  /// Incluye combinaciones válidas e inválidas para simular carga real.
  /// </summary>
  public static IEnumerable<object[]> PerformanceLoginCases =>
    GenerateValidCases()
      .Concat(GenerateInvalidPasswordCases())
      .Concat(GenerateInvalidUserCases())
      .Concat(GenerateExtremeCases())
      .Concat(GenerateLongPasswordCases());

  private static IEnumerable<object[]> GenerateValidCases()
  {
    // Usuarios reales que existen en SeedTestData()
    var validUsers = new[]
    {
      ("admin@example.com", "AdminPass123!"),
      ("user@example.com", "UserPass123!"),
    };

    // Casos válidos (30% del total) - distribuir entre usuarios reales
    for (int i = 1; i <= 301; i++)
    {
      var userIndex = (i - 1) % validUsers.Length;
      var (email, password) = validUsers[userIndex];

      yield return new object[]
      {
        email,
        password,
        true, // Esperado: éxito
        $"Performance_Valid_{i}_{userIndex}",
      };
    }
  }

  private static IEnumerable<object[]> GenerateInvalidPasswordCases()
  {
    // Casos inválidos - contraseña incorrecta (40% del total)
    for (int i = 1; i <= 400; i++)
    {
      yield return new object[]
      {
        "admin@example.com", // Usuario que existe
        $"WrongPassword{i}",
        false, // Esperado: fallo
        $"Performance_InvalidPassword_{i}",
      };
    }
  }

  private static IEnumerable<object[]> GenerateInvalidUserCases()
  {
    // Casos inválidos - usuario inexistente (20% del total)
    for (int i = 1; i <= 200; i++)
    {
      yield return new object[]
      {
        $"nonexistent{i:D4}@nowhere.com",
        $"AnyPassword{i}#",
        false, // Esperado: fallo
        $"Performance_UserNotFound_{i}",
      };
    }
  }

  private static IEnumerable<object[]> GenerateExtremeCases()
  {
    // Casos extremos - emails largos (5% del total)
    for (int i = 1; i <= 50; i++)
    {
      var longEmail = new string('a', 50) + $"{i}@" + new string('b', 30) + ".com";
      yield return new object[]
      {
        longEmail,
        $"LongEmailPassword{i}",
        false, // Esperado: fallo (usuario no existe)
        $"Performance_LongEmail_{i}",
      };
    }
  }

  private static IEnumerable<object[]> GenerateLongPasswordCases()
  {
    // Casos extremos - contraseñas largas (5% del total)
    for (int i = 1; i <= 50; i++)
    {
      var longPassword = new string('X', 100) + $"{i}#";
      yield return new object[]
      {
        "admin@example.com",
        longPassword,
        false, // Esperado: fallo
        $"Performance_LongPassword_{i}",
      };
    }
  }

  /// <summary>
  /// Casos de concurrencia para test de performance bajo carga.
  /// Simula múltiples usuarios intentando login simultáneamente.
  /// </summary>
  public static IEnumerable<object[]> ConcurrentLoginCases
  {
    get
    {
      var validUsers = new[]
      {
        ("admin@example.com", "AdminPass123!"),
        ("user@example.com", "UserPass123!"),
      };

      for (int i = 1; i <= 100; i++)
      {
        var userIndex = (i - 1) % validUsers.Length;
        var (email, password) = validUsers[userIndex];

        // Mezclar casos válidos e inválidos
        var isValidCase = i % 3 == 0; // 33% éxito
        var finalPassword = isValidCase ? password : $"WrongPass{i}#";

        yield return new object[]
        {
          i, // Thread ID
          email,
          finalPassword,
          isValidCase, // Esperado resultado
          $"Concurrent_{i}",
        };
      }
    }
  }

  /// <summary>
  /// Datos para benchmark de hashing con Argon2id. Los parámetros de costo (m=65536,t=3,p=1)
  /// son fijos en <c>PasswordHasher</c>, por lo que se repiten varias corridas por password
  /// para observar variación de timing, en vez de simular distintos "work factors" como BCrypt.
  /// </summary>
  public static IEnumerable<object[]> PasswordHasherPerformanceCases
  {
    get
    {
      var iterations = new[] { 1, 2, 3, 4 };
      var passwords = new[]
      {
        "SimplePass123",
        "ComplexP@ssw0rd!WithNumbers123",
        new string('A', 72),
        "🔐🚀Special_Chars_Test_2024🎯",
      };

      foreach (var iteration in iterations)
      {
        foreach (var password in passwords)
        {
          yield return new object[]
          {
            iteration,
            password,
            $"Argon2id_Run{iteration}_{password.Length}chars",
          };
        }
      }
    }
  }
}
