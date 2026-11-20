namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Infrastructure.Validation;

public static class RolePermissionAssignmentValidationTypes
{
  public enum ValidationKey
  {
    RolePublicIdRequired,
    PermissionPublicIdRequired,
    PermissionPublicIdsRequired,
    PermissionPublicIdsContainEmpty,
    PermissionPublicIdsExceedMaxBulkSize,
    RoleNotFound,
    ActiveRoleNotFound,
    RoleInactiveOrDeleted,
    PermissionNotFound,
    PermissionInactiveOrDeleted,
    RelationAlreadyAssigned,
    ActiveRelationNotFound,
    DeletedRelationNotFound,
    RelationCannotBeModifiedInactive,
    ActivePermissionsNotFound,
    DefaultRoleCannotHaveElevatedPermission,
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
      Args = args?.Length > 0 ? args : [];
      Message = Args.Length > 0
        ? string.Format(System.Globalization.CultureInfo.InvariantCulture, message, Args)
        : message;
    }

    private ValidationType(string code, string message, string? regex, object[] args)
    {
      Code = code;
      Regex = regex;
      Args = args;
      Message = Args.Length > 0
        ? string.Format(System.Globalization.CultureInfo.InvariantCulture, message, Args)
        : message;
    }

    public ValidationType WithRegex(string regex)
      => new ValidationType(Code, Message, regex, Args);
  }

  public static ValidationType RolePublicIdRequired =>
    new("ROLE_PERMISSION_ASSIGNMENT_ROLE_PUBLIC_ID_REQUIRED", "El identificador público del rol es obligatorio.");

  public static ValidationType PermissionPublicIdRequired =>
    new("ROLE_PERMISSION_ASSIGNMENT_PERMISSION_PUBLIC_ID_REQUIRED", "El identificador público del permiso es obligatorio.");

  public static ValidationType PermissionPublicIdsRequired =>
    new("ROLE_PERMISSION_ASSIGNMENT_PERMISSION_PUBLIC_IDS_REQUIRED", "La lista de identificadores públicos de permiso es obligatoria.");

  public static ValidationType PermissionPublicIdsContainEmpty =>
    new("ROLE_PERMISSION_ASSIGNMENT_PERMISSION_PUBLIC_IDS_CONTAIN_EMPTY", "La lista de permisos no puede contener identificadores públicos vacíos.");

  public static ValidationType PermissionPublicIdsExceedMaxBulkSize =>
    new("ROLE_PERMISSION_ASSIGNMENT_PERMISSION_PUBLIC_IDS_EXCEED_MAX_BULK_SIZE", "La lista de permisos no puede tener más de {0} elementos.", FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination.PaginationDefaults.MaxBulkAssignmentSize);

  public static ValidationType RoleNotFound =>
    new("ROLE_PERMISSION_ASSIGNMENT_ROLE_NOT_FOUND", "No se encontró el rol '{0}'.");

  public static ValidationType ActiveRoleNotFound =>
    new("ROLE_PERMISSION_ASSIGNMENT_ACTIVE_ROLE_NOT_FOUND", "No se encontró el rol activo '{0}'.");

  public static ValidationType RoleInactiveOrDeleted =>
    new("ROLE_PERMISSION_ASSIGNMENT_ROLE_INACTIVE_OR_DELETED", "El rol '{0}' está inactivo o eliminado.");

  public static ValidationType PermissionNotFound =>
    new("ROLE_PERMISSION_ASSIGNMENT_PERMISSION_NOT_FOUND", "No se encontró el permiso '{0}'.");

  public static ValidationType PermissionInactiveOrDeleted =>
    new("ROLE_PERMISSION_ASSIGNMENT_PERMISSION_INACTIVE_OR_DELETED", "El permiso '{0}' está inactivo o eliminado.");

  public static ValidationType RelationAlreadyAssigned =>
    new("ROLE_PERMISSION_ASSIGNMENT_RELATION_ALREADY_ASSIGNED", "El permiso '{0}' ya está asignado al rol '{1}'.");

  public static ValidationType ActiveRelationNotFound =>
    new("ROLE_PERMISSION_ASSIGNMENT_ACTIVE_RELATION_NOT_FOUND", "No existe una relación activa entre el rol '{0}' y el permiso '{1}'.");

  public static ValidationType DeletedRelationNotFound =>
    new("ROLE_PERMISSION_ASSIGNMENT_DELETED_RELATION_NOT_FOUND", "No existe una relación eliminada entre el rol '{0}' y el permiso '{1}'.");

  public static ValidationType RelationCannotBeModifiedInactive =>
    new("ROLE_PERMISSION_ASSIGNMENT_RELATION_INACTIVE_ENTITIES", "La relación no puede modificarse porque el rol o el permiso no están activos.");

  public static ValidationType ActivePermissionsNotFound =>
    new("ROLE_PERMISSION_ASSIGNMENT_ACTIVE_PERMISSIONS_NOT_FOUND", "No se encontraron permisos activos: {0}");

  public static ValidationType DefaultRoleCannotHaveElevatedPermission =>
    new("ROLE_PERMISSION_ASSIGNMENT_DEFAULT_ROLE_CANNOT_HAVE_ELEVATED_PERMISSION", "El rol '{0}' es el rol default para nuevos usuarios (IsDefaultForNewUser) y no puede tener asignado el permiso '{1}': requiere acceso de desarrollo o super admin, o es de administración del paquete (todo usuario auto-registrado nacería administrador).");

  private static readonly Dictionary<ValidationKey, ValidationType> _dict = new()
    {
      { ValidationKey.RolePublicIdRequired, RolePublicIdRequired },
      { ValidationKey.PermissionPublicIdRequired, PermissionPublicIdRequired },
      { ValidationKey.PermissionPublicIdsRequired, PermissionPublicIdsRequired },
      { ValidationKey.PermissionPublicIdsContainEmpty, PermissionPublicIdsContainEmpty },
      { ValidationKey.PermissionPublicIdsExceedMaxBulkSize, PermissionPublicIdsExceedMaxBulkSize },
      { ValidationKey.RoleNotFound, RoleNotFound },
      { ValidationKey.ActiveRoleNotFound, ActiveRoleNotFound },
      { ValidationKey.RoleInactiveOrDeleted, RoleInactiveOrDeleted },
      { ValidationKey.PermissionNotFound, PermissionNotFound },
      { ValidationKey.PermissionInactiveOrDeleted, PermissionInactiveOrDeleted },
      { ValidationKey.RelationAlreadyAssigned, RelationAlreadyAssigned },
      { ValidationKey.ActiveRelationNotFound, ActiveRelationNotFound },
      { ValidationKey.DeletedRelationNotFound, DeletedRelationNotFound },
      { ValidationKey.RelationCannotBeModifiedInactive, RelationCannotBeModifiedInactive },
      { ValidationKey.ActivePermissionsNotFound, ActivePermissionsNotFound },
      { ValidationKey.DefaultRoleCannotHaveElevatedPermission, DefaultRoleCannotHaveElevatedPermission },
    };

  public static ValidationType Get(ValidationKey key) => _dict[key];
}
