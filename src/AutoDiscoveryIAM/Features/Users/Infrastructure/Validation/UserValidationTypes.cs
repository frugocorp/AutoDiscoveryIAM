namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;

public static class UserValidationTypes
{
  public enum ValidationKey
  {
    UsernameRequired,
    UsernameMinLength,
    UsernameMaxLength,
    UsernameInvalidChars,
    EmailRequired,
    EmailMaxLength,
    EmailInvalid,
    EmailExists,
    UsernameExists,
    PasswordRequired,
    PasswordMinLength,
    PasswordMaxLength,
    CurrentPasswordInvalid,
    ResetTokenRequired,
    IdRequired,
    IdGreaterThanZero,
    PublicIdRequired,
    PublicIdInvalid,
    PhoneNumberMaxLength,
    StatusRequired,
    StatusInvalid,

    // Los valores nuevos van siempre al final: insertarlos a la mitad cambia el número de los
    // siguientes y rompe a quien guarde o compare el valor numérico.
    MustChangePasswordRequiresPassword,
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

  // Definición de tipos de validación concretos como propiedades
  public static ValidationType UsernameRequired => new("USERNAME_REQUIRED", "El nombre de usuario es obligatorio.");
  public static ValidationType UsernameMinLength => new("USERNAME_MIN_LENGTH", "El nombre de usuario debe tener al menos {0} caracteres.", 3);
  public static ValidationType UsernameMaxLength => new("USERNAME_MAX_LENGTH", "El nombre de usuario no puede exceder los {0} caracteres.", 100);
  public static ValidationType UsernameInvalidChars => new ValidationType("USERNAME_INVALID_CHARS", "El nombre de usuario contiene caracteres no permitidos. Solo se permiten letras, números, punto (.), guion (-) y guion bajo (_).").WithRegex("^[a-zA-Z0-9._-]+$");

  public static ValidationType EmailRequired => new("EMAIL_REQUIRED", "El correo electrónico es obligatorio.");
  public static ValidationType EmailMaxLength => new("EMAIL_MAX_LENGTH", "El correo electrónico no puede exceder los {0} caracteres.", 150);
  public static ValidationType EmailInvalid => new("EMAIL_INVALID", "El correo electrónico no tiene un formato válido.");
  public static ValidationType EmailExists => new("EMAIL_EXISTS", "El correo electrónico ya está registrado.");
  public static ValidationType UsernameExists => new("USERNAME_EXISTS", "El nombre de usuario ya está registrado.");

  public static ValidationType PasswordRequired => new("PASSWORD_REQUIRED", "La contraseña es obligatoria.");
  public static ValidationType PasswordMinLength => new("PASSWORD_MIN_LENGTH", "La contraseña debe tener al menos {0} caracteres.", 8);
  public static ValidationType PasswordMaxLength => new("PASSWORD_MAX_LENGTH", "La contraseña no puede exceder los {0} caracteres.", 256);
  public static ValidationType CurrentPasswordInvalid => new("CURRENT_PASSWORD_INVALID", "La contraseña actual es incorrecta.");
  public static ValidationType ResetTokenRequired => new("RESET_TOKEN_REQUIRED", "El token de reseteo es obligatorio.");
  public static ValidationType MustChangePasswordRequiresPassword => new("MUST_CHANGE_PASSWORD_REQUIRES_PASSWORD", "mustChangePassword solo aplica cuando el administrador define una contraseña inicial.");

  public static ValidationType IdRequired => new("ID_REQUIRED", "El Id del usuario es requerido.");
  public static ValidationType IdGreaterThanZero => new("ID_GREATER_THAN_ZERO", "El Id del usuario debe ser mayor a {0}.", 0);
  public static ValidationType PublicIdRequired => new("PUBLIC_ID_REQUIRED", "El identificador público (GUID) es obligatorio.");
  public static ValidationType PublicIdInvalid => new ValidationType("PUBLIC_ID_INVALID", "El identificador público (GUID) no tiene un formato válido.").WithRegex("^[{(]?[0-9A-Fa-f]{8}(-[0-9A-Fa-f]{4}){3}-[0-9A-Fa-f]{12}[)}]?$");

  public static ValidationType PhoneNumberMaxLength => new("PHONE_NUMBER_MAX_LENGTH", "El número de teléfono no puede exceder los {0} caracteres.", 20);

  public static ValidationType StatusRequired => new("STATUS_REQUIRED", "El estado es obligatorio.");
  public static ValidationType StatusInvalid => new("STATUS_INVALID", "El estado es inválido. Valores permitidos: active, inactive, blocked, suspended, pending, pending_approval.");
  public static readonly string[] StatusAllowedValues = new[] { "active", "inactive", "blocked", "suspended", "pending", "pending_approval" };

  // Diccionario para acceso por clave
  private static readonly Dictionary<ValidationKey, ValidationType> _dict = new()
    {
        { ValidationKey.UsernameRequired, UsernameRequired },
        { ValidationKey.UsernameMinLength, UsernameMinLength },
        { ValidationKey.UsernameMaxLength, UsernameMaxLength },
        { ValidationKey.UsernameInvalidChars, UsernameInvalidChars },
        { ValidationKey.EmailRequired, EmailRequired },
        { ValidationKey.EmailMaxLength, EmailMaxLength },
        { ValidationKey.EmailInvalid, EmailInvalid },
        { ValidationKey.EmailExists, EmailExists },
        { ValidationKey.UsernameExists, UsernameExists },
        { ValidationKey.PasswordRequired, PasswordRequired },
        { ValidationKey.PasswordMinLength, PasswordMinLength },
        { ValidationKey.PasswordMaxLength, PasswordMaxLength },
        { ValidationKey.CurrentPasswordInvalid, CurrentPasswordInvalid },
        { ValidationKey.ResetTokenRequired, ResetTokenRequired },
        { ValidationKey.MustChangePasswordRequiresPassword, MustChangePasswordRequiresPassword },
        { ValidationKey.IdRequired, IdRequired },
        { ValidationKey.IdGreaterThanZero, IdGreaterThanZero },
        { ValidationKey.PublicIdRequired, PublicIdRequired },
        { ValidationKey.PublicIdInvalid, PublicIdInvalid },
        { ValidationKey.PhoneNumberMaxLength, PhoneNumberMaxLength },
        { ValidationKey.StatusRequired, StatusRequired },
        { ValidationKey.StatusInvalid, StatusInvalid },
        // Agrega más asignaciones según necesidades
    };

  public static ValidationType Get(ValidationKey key) => _dict[key];
}

