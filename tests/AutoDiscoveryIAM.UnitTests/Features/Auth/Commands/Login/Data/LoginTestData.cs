// -----------------------------------------------------------------------
// <copyright file="LoginTestData.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login.Data;

/// <summary>
/// Datos de prueba externalizados para tests de autenticación.
/// Contiene casos válidos, inválidos, edge cases y escenarios de performance.
/// </summary>
public static class LoginTestData
{
  /// <summary>
  /// Proporciona casos de login válidos con credenciales que deberían autenticarse correctamente.
  /// Incluye hashes Argon2id reales generados con los parámetros por defecto de Isopoh.Cryptography.Argon2.
  /// </summary>
  /// <returns>Datos de prueba para casos válidos.</returns>
  public static IEnumerable<object[]> ValidLoginCases()
  {
    yield return new object[]
    {
      "admin@example.com",
      "AdminPass123!",
      "$argon2id$v=19$m=65536,t=3,p=1$imBP+dxp6AfePkAmjVfl9A$Ch3Yrq6VGEnwaO2rxwAM/FjzX5bBXrrKPGseqx9O6Ig", // Real Argon2id hash para AdminPass123!
      "Usuario administrador con credenciales válidas",
    };

    yield return new object[]
    {
      "user@example.com",
      "UserPass123!",
      "$argon2id$v=19$m=65536,t=3,p=1$EAbZ0dVvIvPEE2cf4RvOBg$aekIoccqSoBj4bPhbY2UmwSJ4nRki6TwOq3nhowvVXc", // Real Argon2id hash para UserPass123!
      "Usuario regular con credenciales válidas",
    };

    yield return new object[]
    {
      "manager@example.com",
      "ManagerPass123!",
      "$argon2id$v=19$m=65536,t=3,p=1$soHhL4p6TRLhfIcGBUDrGg$EPTmiym8wSPb7xe6438Gycc5BXWoa4r4DmzCP3bcdws", // Real Argon2id hash para ManagerPass123!
      "Usuario gerente con credenciales válidas",
    };

    yield return new object[]
    {
      "test@example.com",
      "TestPass123!",
      "$argon2id$v=19$m=65536,t=3,p=1$OOwzesn4eOhertDDWp7BEQ$q3C9LZDkbhpIUeH8c1Sxi2ailXMeVnP3EEQ2rs3K7dU", // Real Argon2id hash para TestPass123!
      "Usuario de prueba con credenciales válidas",
    };
  }

  /// <summary>
  /// Proporciona casos de login válidos que deberían pasar la autenticación.
  /// Solo incluye email, password y description (sin hashedPassword).
  /// </summary>
  /// <returns>Datos de prueba para casos válidos.</returns>
  public static IEnumerable<object[]> ValidLoginCasesSimple()
  {
    yield return new object[]
    {
      "admin@example.com",
      "AdminPass123!",
      "Usuario administrador con credenciales válidas",
    };

    yield return new object[]
    {
      "user@example.com",
      "UserPass123!",
      "Usuario regular con credenciales válidas",
    };

    yield return new object[]
    {
      "manager@example.com",
      "ManagerPass123!",
      "Usuario gerente con credenciales válidas",
    };

    yield return new object[]
    {
      "test@example.com",
      "TestPass123!",
      "Usuario de prueba con credenciales válidas",
    };
  }

  /// <summary>
  /// Proporciona casos de login inválidos que deberían fallar la autenticación.
  /// Solo incluye email, password y description (sin expectedError).
  /// </summary>
  /// <returns>Datos de prueba para casos inválidos.</returns>
  public static IEnumerable<object[]> InvalidLoginCasesSimple()
  {
    yield return new object[]
    {
      "admin@example.com",
      "WrongPassword123!",
      "Correct email but wrong password",
    };

    yield return new object[]
    {
      "nonexistent@example.com",
      "AnyPassword123!",
      "Non-existent user",
    };

    yield return new object[]
    {
      "",
      "AdminPass123!",
      "Empty email",
    };

    yield return new object[]
    {
      "admin@example.com",
      "",
      "Empty password",
    };

    yield return new object[]
    {
      "inactive@example.com",
      "InactivePass123!",
      "Inactive user attempting login",
    };
  }

  /// <summary>
  /// Proporciona casos edge de login para testing avanzado.
  /// </summary>
  /// <returns>Datos de prueba para casos edge.</returns>
  public static IEnumerable<object[]> EdgeCasesLoginData()
  {
    yield return new object[]
    {
      "user.with+symbols@example-domain.co.uk",
      "P@ssw0rd!ComplexSymbols#$%",
      "failure",
      "Email with valid symbols but user doesn't exist",
    };

    yield return new object[]
    {
      "unicode@тест.com",
      "contraseña123!",
      "failure",
      "Email with Unicode characters",
    };

    yield return new object[]
    {
      "admin@example.com",
      "Pass Word 123!",
      "failure",
      "Password with spaces",
    };

    yield return new object[]
    {
      "admin@example.com   ",
      "AdminPass123!",
      "failure",
      "Email with trailing spaces",
    };

    yield return new object[]
    {
      "admin@example.com",
      new string('a', 1000),
      "failure",
      "Extremely long password",
    };
  }

  /// <summary>
  /// Proporciona casos de performance para testing de velocidad.
  /// </summary>
  /// <returns>Datos de prueba para performance.</returns>
  public static IEnumerable<object[]> PerformanceLoginCases()
  {
    yield return new object[]
    {
      "admin@example.com",
      "AdminPass123!",
      15000, // Max expected milliseconds: Argon2id (m=65536,t=3,p=1) bajo ci-full con recoleccion de cobertura y ejecucion paralela puede superar 6-7s por contencion de CPU/memoria
      "Standard login performance test",
    };

    yield return new object[]
    {
      "nonexistent@example.com",
      "AnyPassword123!",
      15000, // Comentario anterior ("no invoca VerifyPassword") era incorrecto: LoginHandler SI
             // llama VerifyPassword contra un hash dummy aqui a proposito (mitigacion de timing
             // attack, ver DummyPasswordHash en LoginHandler.cs) -- paga el mismo costo Argon2id
             // que los otros 2 casos de este metodo, mismo umbral por la misma razon (ci-full).
      "Non-existent user performance test",
    };

    yield return new object[]
    {
      "user@example.com",
      "WrongPassword123!",
      15000, // Password verification con Argon2id sigue tomando tiempo bajo ci-full (ver justificacion arriba)
      "Wrong password performance test",
    };
  }

  /// <summary>
  /// Proporciona casos de seguridad para testing de vulnerabilidades.
  /// </summary>
  /// <returns>Datos de prueba para seguridad.</returns>
  public static IEnumerable<object[]> SecurityTestCases()
  {
    yield return new object[]
    {
      "admin@example.com'; DROP TABLE Users; --",
      "AdminPass123!",
      "failure",
      "SQL injection attempt in email",
    };

    yield return new object[]
    {
      "admin@example.com",
      "AdminPass123!'; DROP TABLE Users; --",
      "failure",
      "SQL injection attempt in password",
    };

    yield return new object[]
    {
      "<script>alert('xss')</script>@example.com",
      "AdminPass123!",
      "failure",
      "XSS attempt in email",
    };

    yield return new object[]
    {
      "admin@example.com",
      "<script>alert('xss')</script>",
      "failure",
      "XSS attempt in password",
    };

    yield return new object[]
    {
      "admin@example.com",
      new string('A', 10000),
      "failure",
      "Buffer overflow attempt with long password",
    };
  }

  /// <summary>
  /// Casos masivos para test de carga del sistema de login.
  /// Genera múltiples combinaciones para verificar performance.
  /// </summary>
  public static IEnumerable<object[]> MassiveLoginCases()
  {
    // Casos válidos distribuidos (300 casos)
    for (int i = 1; i <= 100; i++)
    {
      yield return new object[]
      {
        "admin@example.com",
        "AdminPass123!",
        "success",
        $"MassiveValid_Admin_{i:D3}",
      };

      yield return new object[]
      {
        "user@example.com",
        "UserPass123!",
        "success",
        $"MassiveValid_User_{i:D3}",
      };

      yield return new object[]
      {
        "manager@example.com",
        "ManagerPass123!",
        "success",
        $"MassiveValid_Manager_{i:D3}",
      };
    }

    // Casos inválidos distribuidos (700 casos)
    for (int i = 1; i <= 350; i++)
    {
      yield return new object[]
      {
        $"invalid{i:D4}@test.com",
        $"InvalidPassword{i}#",
        "failure",
        $"MassiveInvalid_NonExistent_{i:D3}",
      };

      yield return new object[]
      {
        "admin@example.com",
        $"WrongPassword{i}#",
        "failure",
        $"MassiveInvalid_WrongPassword_{i:D3}",
      };
    }
  }

  /// <summary>
  /// Casos específicos para anti-enumeración de usuarios.
  /// Todos deben devolver el mismo mensaje genérico.
  /// </summary>
  public static IEnumerable<object[]> AntiEnumerationCases()
  {
    // Usuario válido, contraseña inválida
    yield return new object[]
    {
      "admin@example.com",
      "wrongpassword",
      "failure",
      "ValidUser_InvalidPassword",
    };

    // Usuario inválido, cualquier contraseña
    yield return new object[]
    {
      "nonexistent@example.com",
      "anypassword",
      "failure",
      "InvalidUser_AnyPassword",
    };

    // Usuario inválido, contraseña válida conocida
    yield return new object[]
    {
      "fake@nowhere.com",
      "AdminPass123!",
      "failure",
      "InvalidUser_ValidPassword",
    };

    // Variaciones de email existente
    yield return new object[]
    {
      "admin@example.co",
      "AdminPass123!",
      "failure",
      "SimilarEmail_ValidPassword",
    };
  }

  /// <summary>
  /// Proporciona casos de credenciales inválidas para testing de seguridad.
  /// Incluye combinaciones que deberían fallar por credenciales incorrectas.
  /// </summary>
  /// <returns>Datos de prueba para credenciales inválidas.</returns>
  public static IEnumerable<object[]> InvalidCredentialsCases()
  {
    // Password incorrecto, usuario válido
    yield return new object[]
    {
      "admin@example.com",
      "WrongPassword123!",
      "Invalid credentials",
    };

    // Usuario inexistente
    yield return new object[]
    {
      "nonexistent@example.com",
      "AnyPassword123!",
      "Invalid credentials",
    };

    // Email con mayúsculas (caso sensible)
    yield return new object[]
    {
      "ADMIN@EXAMPLE.COM",
      "AdminPass123!",
      "Invalid credentials",
    };

    // Password vacío
    yield return new object[]
    {
      "admin@example.com",
      "",
      "Invalid credentials",
    };

    // Email vacío
    yield return new object[]
    {
      "",
      "AdminPass123!",
      "Invalid credentials",
    };
  }

  /// <summary>
  /// Proporciona casos de validación de formato para testing de edge cases.
  /// Incluye emails y passwords con formatos incorrectos o límite.
  /// </summary>
  /// <returns>Datos de prueba para validación de formato.</returns>
  public static IEnumerable<object[]> FormatValidationCases()
  {
    foreach (var testCase in GetEmailFormatValidationCases())
      yield return testCase;

    foreach (var testCase in GetPasswordFormatValidationCases())
      yield return testCase;

    foreach (var testCase in GetSpecialCharacterValidationCases())
      yield return testCase;
  }

  /// <summary>
  /// Casos de validación específicos para formato de email.
  /// </summary>
  private static IEnumerable<object[]> GetEmailFormatValidationCases()
  {
    // Email sin @
    yield return new object[]
    {
      "invalidemail.com",
      "ValidPass123!",
      "Invalid email format",
      "EmailWithoutAtSymbol",
    };

    // Email con múltiples @
    yield return new object[]
    {
      "user@@example.com",
      "ValidPass123!",
      "Invalid email format",
      "EmailWithMultipleAtSymbols",
    };

    // Email extremadamente largo
    yield return new object[]
    {
      new string('a', 300) + "@example.com",
      "ValidPass123!",
      "Email too long",
      "ExtremelyLongEmail",
    };

    // Caracteres especiales en email
    yield return new object[]
    {
      "user+test@example.com",
      "ValidPass123!",
      "Special chars in email",
      "EmailWithSpecialChars",
    };
  }

  /// <summary>
  /// Casos de validación específicos para formato de password.
  /// </summary>
  private static IEnumerable<object[]> GetPasswordFormatValidationCases()
  {
    // Password extremadamente largo
    yield return new object[]
    {
      "admin@example.com",
      new string('x', 1000),
      "Password too long",
      "ExtremelyLongPassword",
    };
  }

  /// <summary>
  /// Casos de validación para caracteres especiales.
  /// </summary>
  private static IEnumerable<object[]> GetSpecialCharacterValidationCases()
  {
    // Unicode en password
    yield return new object[]
    {
      "admin@example.com",
      "🔐UnicodePass123!🚀",
      "Unicode password",
      "PasswordWithUnicode",
    };
  }

}
