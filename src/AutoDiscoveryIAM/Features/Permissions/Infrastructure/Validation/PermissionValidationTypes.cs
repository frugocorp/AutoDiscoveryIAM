// -----------------------------------------------------------------------
// <copyright file="PermissionValidationTypes.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Infrastructure.Validation;

/// <summary>
/// Define los tipos de validación para entidades Permission del sistema RBAC.
/// </summary>
public static class PermissionValidationTypes
{
  public enum ValidationKey
  {
    // Validaciones específicas para SyncPermissions Command
    RequestRequired,
    ForceFullSyncConflictWithDryRun,
    IdGreaterThanZero,

    // Validaciones para Permission Key (campo crítico del RBAC)
    KeyRequired,
    KeyMinLength,
    KeyMaxLength,
    KeyInvalidFormat,
    KeyWildcardInvalid,
    DescriptionMaxLength,
    GroupMaxLength,
    RouteMaxLength,
    HttpMethodInvalid,
    FullNamespaceMaxLength,
    SourceFilePathMaxLength,
    EndpointMethodNameMaxLength,
    CqrsTypeInvalid,
    PreviousKeyMaxLength,
    PreviousFilePathMaxLength,
    PreviousMethodNameMaxLength,
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

  // Validaciones específicas para SyncPermissions Command
  public static ValidationType RequestRequired => new("REQUEST_REQUIRED", "El request no puede ser nulo");
  public static ValidationType ForceFullSyncConflictWithDryRun => new("FORCE_FULL_SYNC_CONFLICT", "ForceFullSync no tiene efecto cuando DryRun está habilitado");
  public static ValidationType IdGreaterThanZero => new("PERMISSION_ID_GREATER_THAN_ZERO", "ID de permiso debe ser mayor a cero");

  // Validaciones para Permission Key (campo crítico del RBAC)
  public static ValidationType KeyRequired => new("PERMISSION_KEY_REQUIRED", "La clave del permiso es obligatoria");
  public static ValidationType KeyMinLength => new("PERMISSION_KEY_MIN_LENGTH", "La clave del permiso debe tener al menos {0} caracteres", 3);
  public static ValidationType KeyMaxLength => new("PERMISSION_KEY_MAX_LENGTH", "La clave del permiso no puede exceder {0} caracteres", 200);
  public static ValidationType KeyInvalidFormat => new ValidationType("PERMISSION_KEY_INVALID_FORMAT", "La clave debe seguir el formato {feature}.{action_snake_case} (ej: users.create_user)")
      .WithRegex(@"^[a-z_]+\.[a-z_]+$");
  public static ValidationType KeyWildcardInvalid => new ValidationType("PERMISSION_KEY_WILDCARD_INVALID", "Los wildcards deben seguir el formato {feature}.* o *.{action} (ej: users.*, *.read)")
      .WithRegex(@"^([a-z_]+\.\*|\*\.[a-z_]+)$");

  // Validaciones para campos opcionales
  public static ValidationType DescriptionMaxLength => new("PERMISSION_DESCRIPTION_MAX_LENGTH", "La descripción no puede exceder {0} caracteres", 500);
  public static ValidationType GroupMaxLength => new("PERMISSION_GROUP_MAX_LENGTH", "El grupo no puede exceder {0} caracteres", 100);

  // Validaciones para metadatos técnicos
  public static ValidationType RouteMaxLength => new("PERMISSION_ROUTE_MAX_LENGTH", "La ruta no puede exceder {0} caracteres", 500);
  public static ValidationType HttpMethodInvalid => new ValidationType("PERMISSION_HTTP_METHOD_INVALID", "El método HTTP debe ser GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS")
      .WithRegex(@"^(GET|POST|PUT|DELETE|PATCH|HEAD|OPTIONS)$");
  public static ValidationType FullNamespaceMaxLength => new("PERMISSION_NAMESPACE_MAX_LENGTH", "El namespace no puede exceder {0} caracteres", 500);
  public static ValidationType SourceFilePathMaxLength => new("PERMISSION_FILEPATH_MAX_LENGTH", "La ruta del archivo no puede exceder {0} caracteres", 1000);
  public static ValidationType EndpointMethodNameMaxLength => new("PERMISSION_METHOD_NAME_MAX_LENGTH", "El nombre del método no puede exceder {0} caracteres", 200);
  public static ValidationType CqrsTypeInvalid => new ValidationType("PERMISSION_CQRS_TYPE_INVALID", "El tipo CQRS debe ser Command o Query")
      .WithRegex(@"^(Command|Query)$");

  // Validaciones para campos de auditoría
  public static ValidationType PreviousKeyMaxLength => new("PERMISSION_PREVIOUS_KEY_MAX_LENGTH", "La clave anterior no puede exceder {0} caracteres", 200);
  public static ValidationType PreviousFilePathMaxLength => new("PERMISSION_PREVIOUS_FILEPATH_MAX_LENGTH", "La ruta anterior no puede exceder {0} caracteres", 1000);
  public static ValidationType PreviousMethodNameMaxLength => new("PERMISSION_PREVIOUS_METHOD_MAX_LENGTH", "El método anterior no puede exceder {0} caracteres", 200);

  private static readonly Dictionary<ValidationKey, ValidationType> _dict = new()
    {
        // Validaciones específicas para Commands
        { ValidationKey.RequestRequired, RequestRequired },
        { ValidationKey.ForceFullSyncConflictWithDryRun, ForceFullSyncConflictWithDryRun },
        { ValidationKey.IdGreaterThanZero, IdGreaterThanZero },

        // Validaciones para Permission Key
        { ValidationKey.KeyRequired, KeyRequired },
        { ValidationKey.KeyMinLength, KeyMinLength },
        { ValidationKey.KeyMaxLength, KeyMaxLength },
        { ValidationKey.KeyInvalidFormat, KeyInvalidFormat },
        { ValidationKey.KeyWildcardInvalid, KeyWildcardInvalid },
        { ValidationKey.DescriptionMaxLength, DescriptionMaxLength },
        { ValidationKey.GroupMaxLength, GroupMaxLength },
        { ValidationKey.RouteMaxLength, RouteMaxLength },
        { ValidationKey.HttpMethodInvalid, HttpMethodInvalid },
        { ValidationKey.FullNamespaceMaxLength, FullNamespaceMaxLength },
        { ValidationKey.SourceFilePathMaxLength, SourceFilePathMaxLength },
        { ValidationKey.EndpointMethodNameMaxLength, EndpointMethodNameMaxLength },
        { ValidationKey.CqrsTypeInvalid, CqrsTypeInvalid },
        { ValidationKey.PreviousKeyMaxLength, PreviousKeyMaxLength },
        { ValidationKey.PreviousFilePathMaxLength, PreviousFilePathMaxLength },
        { ValidationKey.PreviousMethodNameMaxLength, PreviousMethodNameMaxLength },
    };

  public static ValidationType Get(ValidationKey key) => _dict[key];
}
