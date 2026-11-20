// -----------------------------------------------------------------------
// <copyright file="AuthValidationTypes.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Validation;

public static class AuthValidationTypes
{
  public enum ValidationKey
  {
    UsernameRequired,
    UsernameMinLength,
    UsernameMaxLength,
    PasswordRequired,
    PasswordMinLength,
    PasswordMaxLength,
    PasswordUppercase,
    PasswordLowercase,
    PasswordDigit,
    PasswordSpecialChar,
    EmailRequired,
    EmailFormat,
    EmailMaxLength,
    InvalidCredentials,
  }

  public class ValidationType
  {
    public string Code { get; }
    public string Message { get; }
    public object[] Args { get; }
    public string? Regex { get; }

    public ValidationType(string code, string message, params object[] args)
    {
      Code = code;
      Args = args?.Length > 0 ? args : Array.Empty<object>();
      Message = (Args.Length > 0) ? string.Format(System.Globalization.CultureInfo.InvariantCulture, message, Args) : message;
    }

    private ValidationType(string code, string message, string? regex, object[] args)
    {
      Code = code;
      Regex = regex;
      Args = args;
      Message = (Args.Length > 0) ? string.Format(System.Globalization.CultureInfo.InvariantCulture, message, Args) : message;
    }

    // Patrón Fluent Immutable Builder
    public ValidationType WithRegex(string regex)
        => new ValidationType(Code, Message, regex, Args);
  }

  public static ValidationType UsernameRequired => new("USERNAME_REQUIRED", "El nombre de usuario es requerido");
  public static ValidationType UsernameMinLength => new("USERNAME_MIN_LENGTH", "El nombre de usuario debe tener al menos {0} caracteres", 3);
  public static ValidationType UsernameMaxLength => new("USERNAME_MAX_LENGTH", "El nombre de usuario no puede exceder {0} caracteres", 50);
  public static ValidationType PasswordRequired => new("PASSWORD_REQUIRED", "La contraseña es requerida");
  public static ValidationType PasswordMinLength => new("PASSWORD_MIN_LENGTH", "La contraseña debe tener al menos {0} caracteres", 6);
  public static ValidationType PasswordMaxLength => new("PASSWORD_MAX_LENGTH", "La contraseña no puede exceder {0} caracteres", 100);
  public static ValidationType PasswordUppercase => new ValidationType("PASSWORD_UPPERCASE", "La contraseña debe contener al menos una mayúscula").WithRegex("[A-Z]");
  public static ValidationType PasswordLowercase => new ValidationType("PASSWORD_LOWERCASE", "La contraseña debe contener al menos una minúscula").WithRegex("[a-z]");
  public static ValidationType PasswordDigit => new ValidationType("PASSWORD_DIGIT", "La contraseña debe contener al menos un número").WithRegex("[0-9]");
  public static ValidationType PasswordSpecialChar => new ValidationType("PASSWORD_SPECIAL_CHAR", "La contraseña debe contener al menos un carácter especial").WithRegex("[^a-zA-Z0-9]");
  public static ValidationType EmailRequired => new("EMAIL_REQUIRED", "El email es requerido");
  public static ValidationType EmailFormat => new("EMAIL_FORMAT", "El email no tiene un formato válido");
  public static ValidationType EmailMaxLength => new("EMAIL_MAX_LENGTH", "El email no puede exceder {0} caracteres", 100);
  public static ValidationType InvalidCredentials => new("AUTH_INVALID_CREDENTIALS", "Credenciales inválidas.");

  private static readonly Dictionary<ValidationKey, ValidationType> _dict = new()
    {
        { ValidationKey.UsernameRequired, UsernameRequired },
        { ValidationKey.UsernameMinLength, UsernameMinLength },
        { ValidationKey.UsernameMaxLength, UsernameMaxLength },
        { ValidationKey.PasswordRequired, PasswordRequired },
        { ValidationKey.PasswordMinLength, PasswordMinLength },
        { ValidationKey.PasswordMaxLength, PasswordMaxLength },
        { ValidationKey.PasswordUppercase, PasswordUppercase },
        { ValidationKey.PasswordLowercase, PasswordLowercase },
        { ValidationKey.PasswordDigit, PasswordDigit },
        { ValidationKey.PasswordSpecialChar, PasswordSpecialChar },
        { ValidationKey.EmailRequired, EmailRequired },
        { ValidationKey.EmailFormat, EmailFormat },
        { ValidationKey.EmailMaxLength, EmailMaxLength },
        { ValidationKey.InvalidCredentials, InvalidCredentials },
    };

  public static ValidationType Get(ValidationKey key) => _dict[key];
}
