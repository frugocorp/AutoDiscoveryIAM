// -----------------------------------------------------------------------
// <copyright file="RoleValidationTypes.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;

/// <summary>
/// Define los tipos de validación para entidades Role del sistema RBAC.
/// </summary>
public static class RoleValidationTypes
{
  public enum ValidationKey
  {
    NameRequired,
    NameMinLength,
    NameMaxLength,
    NameInvalidChars,
    DescriptionMaxLength,
    IdRequired,
    IdGreaterThanZero,
    PublicIdRequired,
    PublicIdInvalid,
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

  // Validaciones para Role Name (campo crítico del RBAC)
  public static ValidationType NameRequired => new("ROLE_NAME_REQUIRED", "El nombre del rol es obligatorio");
  public static ValidationType NameMinLength => new("ROLE_NAME_MIN_LENGTH", "El nombre del rol debe tener al menos {0} caracteres", 2);
  public static ValidationType NameMaxLength => new("ROLE_NAME_MAX_LENGTH", "El nombre del rol no puede exceder {0} caracteres", 100);
  public static ValidationType NameInvalidChars => new ValidationType("ROLE_NAME_INVALID_CHARS", "El nombre del rol solo puede contener letras, números, espacios, guiones bajos y guiones")
      .WithRegex(@"^[A-Za-z][A-Za-z0-9_\s\-]*$");

  // Validaciones para campos opcionales
  public static ValidationType DescriptionMaxLength => new("ROLE_DESCRIPTION_MAX_LENGTH", "La descripción no puede exceder {0} caracteres", 500);

  // Validaciones para campos de identificación
  public static ValidationType IdRequired => new("ROLE_ID_REQUIRED", "El Id del rol es requerido");
  public static ValidationType IdGreaterThanZero => new("ROLE_ID_GREATER_THAN_ZERO", "El Id del rol debe ser mayor a {0}", 0);
  public static ValidationType PublicIdRequired => new("ROLE_PUBLIC_ID_REQUIRED", "El identificador público (GUID) es obligatorio");
  public static ValidationType PublicIdInvalid => new ValidationType("ROLE_PUBLIC_ID_INVALID", "El identificador público (GUID) no tiene un formato válido")
      .WithRegex("^[{(]?[0-9A-Fa-f]{8}(-[0-9A-Fa-f]{4}){3}-[0-9A-Fa-f]{12}[)}]?$");

  private static readonly Dictionary<ValidationKey, ValidationType> _dict = new()
    {
        { ValidationKey.NameRequired, NameRequired },
        { ValidationKey.NameMinLength, NameMinLength },
        { ValidationKey.NameMaxLength, NameMaxLength },
        { ValidationKey.NameInvalidChars, NameInvalidChars },
        { ValidationKey.DescriptionMaxLength, DescriptionMaxLength },
        { ValidationKey.IdRequired, IdRequired },
        { ValidationKey.IdGreaterThanZero, IdGreaterThanZero },
        { ValidationKey.PublicIdRequired, PublicIdRequired },
        { ValidationKey.PublicIdInvalid, PublicIdInvalid },
    };

  public static ValidationType Get(ValidationKey key) => _dict[key];
}
