using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Validation;
using AwesomeAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Features.Auth.Commands.Login
{
  /// <summary>
  /// Pruebas unitarias para validar las reglas de LoginCommandValidator.
  /// Incluye casos válidos e inválidos para cada campo y verifica mensajes de error específicos usando AuthValidationTypes.
  /// </summary>
  public class LoginCommandValidatorTests
  {
    private readonly LoginCommandValidator _validator = new();

    /// <summary>
    /// Prueba que verifica que el validador LoginCommandValidator falla correctamente cuando el email es inválido.
    /// </summary>
    /// <param name="email">Email a validar.</param>
    /// <param name="password">Contraseña válida.</param>
    /// <param name="expectedKey">Clave de validación esperada.</param>
    [Theory]
    // Email requerido
    [InlineData("", "Testpass.123*", AuthValidationTypes.ValidationKey.EmailRequired)]
    // Email muy corto
    [InlineData("a", "Testpass.123*", AuthValidationTypes.ValidationKey.EmailFormat)]
    // Email muy largo
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa@aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Testpass.123*", AuthValidationTypes.ValidationKey.EmailMaxLength)]
    public void Validator_Should_Fail_For_Invalid_Username(string email, string password, AuthValidationTypes.ValidationKey expectedKey)
    {
      var command = new LoginCommand(email, password);
      var result = _validator.TestValidate(command);
      var expectedValidation = AuthValidationTypes.Get(expectedKey);
      result.IsValid.Should().BeFalse();
      result.Errors.First().ErrorMessage.Should().Be(expectedValidation.Message);
    }

    /// <summary>
    /// Prueba que verifica que el validador LoginCommandValidator falla correctamente cuando el password es inválido.
    /// </summary>
    /// <param name="username">Nombre de usuario válido.</param>
    /// <param name="password">Contraseña a validar.</param>
    /// <param name="expectedKey">Clave de validación esperada.</param>
    [Theory]
    // Password requerido
    [InlineData("testuser@test.com", "", AuthValidationTypes.ValidationKey.PasswordRequired)]
    // Password muy corta
    [InlineData("testuser@test.com", "aA1*", AuthValidationTypes.ValidationKey.PasswordMinLength)]
    // Password muy larga
    [InlineData("testuser@test.com", "A1*a000000000000000000000000000000000sssssA0895874850000000000000000000000ssssssssssddddddddddddddddd", AuthValidationTypes.ValidationKey.PasswordMaxLength)]
    // Sin mayúscula
    [InlineData("testuser@test.com", "password1*", AuthValidationTypes.ValidationKey.PasswordUppercase)]
    // Sin minúscula
    [InlineData("testuser@test.com", "PASSWORD1*", AuthValidationTypes.ValidationKey.PasswordLowercase)]
    // Sin dígito
    [InlineData("testuser@test.com", "Password*", AuthValidationTypes.ValidationKey.PasswordDigit)]
    // Sin caracter especial
    [InlineData("testuser@test.com", "Password123", AuthValidationTypes.ValidationKey.PasswordSpecialChar)]
    public void Validator_Should_Fail_For_Invalid_Password(string username, string password, AuthValidationTypes.ValidationKey expectedKey)
    {
      var command = new LoginCommand(username, password);
      var result = _validator.TestValidate(command);
      var expectedValidation = AuthValidationTypes.Get(expectedKey);
      result.IsValid.Should().BeFalse();
      result.Errors.First().ErrorMessage.Should().Be(expectedValidation.Message);
    }
  }
}



