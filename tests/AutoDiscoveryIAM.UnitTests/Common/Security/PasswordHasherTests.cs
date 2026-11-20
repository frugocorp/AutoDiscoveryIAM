using AwesomeAssertions;
using Xunit;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login.Data;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Common.Security;

/// <summary>
/// Pruebas unitarias para el servicio de gestión segura de contraseñas.
/// Valida el hash Argon2id y la verificación de contraseñas con datos externalizados.
/// </summary>
public class PasswordHasherTests
{
  private readonly PasswordHasher _passwordHasher = new();

  [Theory]
  [MemberData(nameof(PasswordValidationTestData.ValidPasswordsForHashing), MemberType = typeof(PasswordValidationTestData))]
  public void HashPassword_Should_Generate_Valid_Argon2id_Hash_For_Valid_Passwords(string password)
  {
    var hash = _passwordHasher.HashPassword(password);

    hash.Should().NotBeNullOrWhiteSpace();
    hash.Should().StartWith("$argon2id$"); // Formato Argon2id (Isopoh.Cryptography.Argon2)
  }

  [Fact]
  public void HashPassword_Should_Throw_Exception_For_Null_Password()
  {
    var act = () => _passwordHasher.HashPassword(null!);
    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void HashPassword_Should_Generate_Different_Hashes_For_Same_Password()
  {
    const string password = "SamePassword123!";

    var hash1 = _passwordHasher.HashPassword(password);
    var hash2 = _passwordHasher.HashPassword(password);

    hash1.Should().NotBe(hash2, "porque cada hash Argon2id usa una sal aleatoria distinta");
    hash1.Should().NotBeNullOrWhiteSpace();
    hash2.Should().NotBeNullOrWhiteSpace();
  }

  [Theory]
  [MemberData(nameof(PasswordValidationTestData.KnownPasswordHashPairs), MemberType = typeof(PasswordValidationTestData))]
  public void VerifyPassword_Should_Work_Correctly_With_Known_Hashes(
      string password, string hash, bool shouldMatch)
  {
    var result = _passwordHasher.VerifyPassword(password, hash);

    result.Should().Be(shouldMatch);
  }

  [Fact]
  public void VerifyPassword_Should_Throw_Exception_For_Null_Hash()
  {
    var act = () => _passwordHasher.VerifyPassword("password123", null!);
    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void VerifyPassword_Should_Throw_Exception_For_Null_Password()
  {
    var act = () => _passwordHasher.VerifyPassword(null!, "$argon2id$v=19$m=65536,t=3,p=1$abc$def");
    act.Should().Throw<ArgumentNullException>();
  }

  [Theory]
  [MemberData(nameof(PasswordValidationTestData.MalformedHashCases), MemberType = typeof(PasswordValidationTestData))]
  public void VerifyPassword_Should_Return_False_For_Malformed_Or_Empty_Hashes(
      string password, string malformedHash, string description)
  {
    var result = _passwordHasher.VerifyPassword(password, malformedHash);

    result.Should().BeFalse($"porque {description}");
  }

  [Theory]
  [MemberData(nameof(PasswordValidationTestData.ValidPasswordsForHashing), MemberType = typeof(PasswordValidationTestData))]
  public void HashPassword_And_VerifyPassword_Should_Work_Together(string password)
  {
    var hash = _passwordHasher.HashPassword(password);
    var isValid = _passwordHasher.VerifyPassword(password, hash);
    var isInvalidWithWrongPassword = _passwordHasher.VerifyPassword("wrongPassword", hash);

    isValid.Should().BeTrue("porque la contraseña debe coincidir con su propio hash");
    isInvalidWithWrongPassword.Should().BeFalse("porque una contraseña incorrecta no debe coincidir");
  }

  /// <summary>
  /// Prueba de performance para verificar que el hashing no sea excesivamente lento.
  /// Argon2id con los parámetros por defecto (m=65536 KiB, t=3, p=1) es intencionalmente
  /// memory-hard: toma ~500-850ms por operación en hardware de desarrollo típico.
  /// </summary>
  [Theory]
  [Trait("Category", "Performance")]
  [MemberData(nameof(PasswordValidationTestData.PerformancePasswordCases), MemberType = typeof(PasswordValidationTestData))]
  public void HashPassword_Performance_Should_Be_Reasonable(string password)
  {
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    var hash = _passwordHasher.HashPassword(password);

    stopwatch.Stop();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(45000,
        "porque el hash Argon2id (m=65536,t=3,p=1) no debe tardar más de 45 segundos incluso bajo ci-full (cobertura + ejecución paralela añaden contención de CPU/memoria real sobre hardware de desarrollo modesto -- medido hasta ~30.7s en un i7-7700HQ de 4 núcleos bajo ci-full completo)");
    hash.Should().NotBeNullOrWhiteSpace();
  }

    [Fact(DisplayName = "Una contrasena mas larga que el tope se rechaza sin calcular Argon2")]
    public void VerifyPassword_OverMaxLength_ReturnsFalse()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.HashPassword("Password123!");

        hasher.VerifyPassword(new string('a', PasswordHasher.MaxVerifiableLength + 1), hash).Should().BeFalse();
    }
}
