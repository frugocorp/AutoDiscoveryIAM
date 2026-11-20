// -----------------------------------------------------------------------
// <copyright file="LoginEdgeCasesData.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login.Data;

/// <summary>
/// Casos extremos y edge cases para testing del sistema de login.
/// Incluye caracteres especiales, formatos inválidos, y casos límite.
/// </summary>
public static class LoginEdgeCasesData
{
  /// <summary>
  /// Casos con caracteres especiales y formatos extremos en emails.
  /// </summary>
  public static IEnumerable<object[]> EdgeCaseEmails => new List<object[]>
  {
    // Emails con caracteres especiales válidos según RFC
    new object[] { "test+tag@example.com", "ValidPass123!", false, "EmailWithPlus" },
    new object[] { "user.name@sub.domain.com", "ValidPass123!", false, "EmailWithSubdomain" },
    new object[] { "123456@numbers.com", "ValidPass123!", false, "NumericEmail" },

    // Emails límite - máxima longitud válida (64 chars local + @ + 253 chars domain)
    new object[] { new string('a', 64) + "@" + new string('b', 60) + ".com", "ValidPass123!", false, "MaxLengthEmail" },

    // Emails con caracteres Unicode
    new object[] { "用户@example.com", "ValidPass123!", false, "UnicodeEmail" },
    new object[] { "user@münchen.de", "ValidPass123!", false, "UmlautDomain" },
    new object[] { "тест@example.com", "ValidPass123!", false, "CyrillicEmail" },

    // Emails mal formateados que deben fallar
    new object[] { "", "ValidPass123!", false, "EmptyEmail" },
    new object[] { " ", "ValidPass123!", false, "WhitespaceEmail" },
    new object[] { "notemail", "ValidPass123!", false, "NoAtSign" },
    new object[] { "@nodomain.com", "ValidPass123!", false, "NoLocalPart" },
    new object[] { "noatsign.com", "ValidPass123!", false, "NoAtSignWithDot" },
    new object[] { "user@", "ValidPass123!", false, "NoDomain" },
    new object[] { "user@@double.com", "ValidPass123!", false, "DoubleAtSign" },
    new object[] { "user@domain.", "ValidPass123!", false, "TrailingDot" },
    new object[] { ".user@domain.com", "ValidPass123!", false, "LeadingDot" },
    new object[] { "user.@domain.com", "ValidPass123!", false, "TrailingDotLocal" },

    // Casos con longitud extrema
    new object[] { new string('a', 1000) + "@toolong.com", "ValidPass123!", false, "ExtremelyLongEmail" },
    new object[] { "user@" + new string('b', 500) + ".com", "ValidPass123!", false, "ExtremelyLongDomain" },
  };

  /// <summary>
  /// Casos con caracteres especiales y formatos extremos en contraseñas.
  /// </summary>
  public static IEnumerable<object[]> EdgeCasePasswords => new List<object[]>
  {
    // Contraseñas con caracteres especiales complejos
    new object[] { "admin@example.com", "P@$$w0rd!#$%^&*()_+-=[]{}|;:,.<>?", false, "SpecialCharsPassword" },
    new object[] { "admin@example.com", "🔐🚀🎯🌟💎⭐🔥💡🎨🎪", false, "EmojiPassword" },
    new object[] { "admin@example.com", "Пароль123!", false, "CyrillicPassword" },
    new object[] { "admin@example.com", "密码123", false, "ChinesePassword" },
    new object[] { "admin@example.com", "كلمة المرور123", false, "ArabicPassword" },

    // Contraseñas extremadamente largas
    new object[] { "admin@example.com", new string('A', 1000), false, "ExtremelyLongPassword" },
    new object[] { "admin@example.com", string.Concat(Enumerable.Repeat("🔐", 100)), false, "LongEmojiPassword" },

    // Contraseñas con espacios y caracteres de control
    new object[] { "admin@example.com", "Password With Spaces", false, "PasswordWithSpaces" },
    new object[] { "admin@example.com", "Password\tWith\tTabs", false, "PasswordWithTabs" },
    new object[] { "admin@example.com", "Password\nWith\nNewlines", false, "PasswordWithNewlines" },
    new object[] { "admin@example.com", "Password\rWith\rCarriage", false, "PasswordWithCarriageReturn" },

    // Contraseñas límite
    new object[] { "admin@example.com", "", false, "EmptyPassword" },
    new object[] { "admin@example.com", " ", false, "WhitespacePassword" },
    new object[] { "admin@example.com", "\0", false, "NullCharPassword" },

    // Contraseñas que podrían causar problemas de encoding
    new object[] { "admin@example.com", "UTF-8: ñáéíóú àèìòù âêîôû", false, "AccentedCharsPassword" },
    new object[] { "admin@example.com", "Unicode: αβγδε ∑∏∆∇∂", false, "MathSymbolsPassword" },

    // Patrones que podrían causar problemas en regex o validación
    new object[] { "admin@example.com", ".*+?^${}()|[]\\", false, "RegexMetaCharsPassword" },
    new object[] { "admin@example.com", "'; DROP TABLE Users; --", false, "SQLInjectionAttempt" },
    new object[] { "admin@example.com", "<script>alert('xss')</script>", false, "XSSAttempt" },
    new object[] { "admin@example.com", "../../../etc/passwd", false, "PathTraversalAttempt" },
  };

  /// <summary>
  /// Casos extremos combinando emails y contraseñas problemáticos.
  /// </summary>
  public static IEnumerable<object[]> CombinedEdgeCases => new List<object[]>
  {
    // Combinaciones de casos extremos
    new object[] { "🚀test@example.com", "🔐Pass123!", false, "EmojiEmailAndPassword" },
    new object[] { new string('a', 100) + "@test.com", new string('B', 100), false, "BothExtremelyLong" },
    new object[] { "", "", false, "BothEmpty" },
    new object[] { " ", " ", false, "BothWhitespace" },

    // Casos que podrían causar overflow o problemas de memoria
    new object[] { new string('x', 10000), new string('y', 10000), false, "MemoryStressTest" },

    // Casos con caracteres de diferentes encodings
    new object[] { "test@中文.com", "пароль123", false, "MixedEncodingChars" },
  };

  /// <summary>
  /// Casos que podrían causar problemas de timing attack.
  /// Todos con el mismo patrón de timing para evitar información sobre usuarios válidos.
  /// </summary>
  public static IEnumerable<object[]> TimingAttackCases => new List<object[]>
  {
    // Usuario válido, contraseña inválida (debe tomar mismo tiempo que usuario inválido)
    new object[] { "admin@example.com", "wrongpassword", false, "ValidUserWrongPassword" },

    // Usuario inválido, cualquier contraseña (tiempo de referencia)
    new object[] { "nonexistent@test.com", "anypassword", false, "InvalidUserAnyPassword" },

    // Emails similares para verificar timing consistency
    new object[] { "admin@example.co", "wrongpassword", false, "SimilarEmailWrongPassword" },
    new object[] { "Admin@Example.Com", "wrongpassword", false, "CaseVariationWrongPassword" },
  };

  /// <summary>
  /// Casos para verificar límites de rate limiting y protección anti-brute force.
  /// </summary>
  public static IEnumerable<object[]> BruteForceProtectionCases
  {
    get
    {
      // Generar múltiples intentos para el mismo usuario
      for (int i = 1; i <= 20; i++)
      {
        yield return new object[]
        {
          "bruteforce@test.com",
          $"attempt{i}",
          false,
          $"BruteForceAttempt_{i:D2}",
        };
      }
    }
  }
}
