using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using Xunit;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Users.Commands.CreateUser;

/// <summary>
/// Pruebas unitarias para CreateUserCommandValidator.
/// </summary>
public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    /// <summary>
    /// Casos de prueba de usernames inválidos y su mensaje de error esperado.
    /// </summary>
    public static IEnumerable<object[]> InvalidUsernames => new List<object[]>
    {
        new object[] { "", UserValidationTypes.UsernameRequired.Message },
        new object[] { "ab", UserValidationTypes.UsernameMinLength.Message },
        new object[] { new string('a', (int)UserValidationTypes.UsernameMaxLength.Args[0] + 1), UserValidationTypes.UsernameMaxLength.Message },
        new object[] { "inv@lid", UserValidationTypes.UsernameInvalidChars.Message },
    };

    /// <summary>
    /// Casos de prueba de emails inválidos y su mensaje de error esperado.
    /// </summary>
    public static IEnumerable<object[]> InvalidEmails => new List<object[]>
    {
        new object[] { "", UserValidationTypes.EmailRequired.Message },
        new object[] { new string('a', (int)UserValidationTypes.EmailMaxLength.Args[0] + 1) + "@mail.com", UserValidationTypes.EmailMaxLength.Message },
        new object[] { "invalid-email", UserValidationTypes.EmailInvalid.Message },
    };

    /// <summary>
    /// Casos de prueba de contraseñas inválidas y su mensaje de error esperado.
    /// </summary>
    public static IEnumerable<object[]> InvalidPasswords => new List<object[]>
    {
        new object[] { "", UserValidationTypes.PasswordRequired.Message },
        new object[] { "short", UserValidationTypes.PasswordMinLength.Message },
        new object[] { new string('a', (int)UserValidationTypes.PasswordMaxLength.Args[0] + 1), UserValidationTypes.PasswordMaxLength.Message },
    };

    /// <summary>
    /// Valida múltiples escenarios inválidos para el campo Username.
    /// </summary>
    /// <param name="username">Valor a probar.</param>
    /// <param name="expectedError">Mensaje de error esperado.</param>
    [Theory(DisplayName = "Username inválido produce error esperado")]
    [MemberData(nameof(InvalidUsernames))]
    public void Validator_Should_Fail_For_Invalid_Username(string username, string expectedError)
    {
        // Arrange
        const string email = "valid@email.com";
        const string password = "ValidPass123*";
        var command = new CreateUserCommand(username, email, password);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.First().ErrorMessage.Should().Be(expectedError);
    }

    /// <summary>
    /// Valida múltiples escenarios inválidos para el campo Email.
    /// </summary>
    /// <param name="email">Valor a probar.</param>
    /// <param name="expectedError">Mensaje de error esperado.</param>
    [Theory(DisplayName = "Email inválido produce error esperado")]
    [MemberData(nameof(InvalidEmails))]
    public void Validator_Should_Fail_For_Invalid_Email(string email, string expectedError)
    {
        // Arrange
        const string username = "validuser";
        const string password = "ValidPass123*";
        var command = new CreateUserCommand(username, email, password);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.First().ErrorMessage.Should().Be(expectedError);
    }

    /// <summary>
    /// Valida múltiples escenarios inválidos para el campo Password.
    /// </summary>
    /// <param name="password">Valor a probar.</param>
    /// <param name="expectedError">Mensaje de error esperado.</param>
    [Theory(DisplayName = "Password inválido produce error esperado")]
    [MemberData(nameof(InvalidPasswords))]
    public void Validator_Should_Fail_For_Invalid_Password(string password, string expectedError)
    {
        // Arrange
        const string username = "validuser";
        const string email = "valid@email.com";
        var command = new CreateUserCommand(username, email, password);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.First().ErrorMessage.Should().Be(expectedError);
    }

    /// <summary>
    /// Verifica que un comando con Username/Email/Password válidos no produce errores.
    /// </summary>
    [Fact(DisplayName = "Comando válido no produce errores")]
    public void Validator_Should_Succeed_For_Valid_Command()
    {
        // Arrange
        var command = new CreateUserCommand("validuser", "valid@email.com", "ValidPass123*");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
