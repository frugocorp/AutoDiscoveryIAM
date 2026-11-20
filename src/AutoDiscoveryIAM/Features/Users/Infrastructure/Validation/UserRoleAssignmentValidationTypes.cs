namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;

public static class UserRoleAssignmentValidationTypes
{
  public enum ValidationKey
  {
    UserPublicIdRequired,
    RolePublicIdRequired,
    UserPublicIdsRequired,
    RolePublicIdsRequired,
    UserPublicIdsContainEmpty,
    RolePublicIdsContainEmpty,
    UserPublicIdsExceedMaxBulkSize,
    RolePublicIdsExceedMaxBulkSize,
    UserNotFound,
    ActiveUserNotFound,
    UserInactiveOrDeleted,
    RoleNotFound,
    ActiveRoleNotFound,
    RoleInactiveOrDeleted,
    RelationAlreadyAssigned,
    ActiveRolesNotFound,
    ActiveUsersNotFound,
    ActiveRelationNotFound,
    DeletedRelationNotFound,
    RelationCannotBeModifiedInactive,
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
      Message = Args.Length > 0
        ? string.Format(System.Globalization.CultureInfo.InvariantCulture, message, Args)
        : message;
    }

    private ValidationType(string code, string message, string? regex, object[] args)
    {
      Code = code;
      Regex = regex;
      Args = args;
      Message = (Args.Length > 0)
        ? string.Format(System.Globalization.CultureInfo.InvariantCulture, message, Args)
        : message;
    }

    public ValidationType WithRegex(string regex)
      => new ValidationType(Code, Message, regex, Args);
  }

  public static ValidationType UserPublicIdRequired =>
    new("USER_ROLE_ASSIGNMENT_USER_PUBLIC_ID_REQUIRED", "El identificador público del usuario es obligatorio.");

  public static ValidationType RolePublicIdRequired =>
    new("USER_ROLE_ASSIGNMENT_ROLE_PUBLIC_ID_REQUIRED", "El identificador público del rol es obligatorio.");

  public static ValidationType UserPublicIdsRequired =>
    new("USER_ROLE_ASSIGNMENT_USER_PUBLIC_IDS_REQUIRED", "La lista de identificadores públicos de usuario es obligatoria.");

  public static ValidationType RolePublicIdsRequired =>
    new("USER_ROLE_ASSIGNMENT_ROLE_PUBLIC_IDS_REQUIRED", "La lista de identificadores públicos de rol es obligatoria.");

  public static ValidationType UserPublicIdsContainEmpty =>
    new("USER_ROLE_ASSIGNMENT_USER_PUBLIC_IDS_CONTAIN_EMPTY", "La lista de usuarios no puede contener identificadores públicos vacíos.");

  public static ValidationType RolePublicIdsContainEmpty =>
    new("USER_ROLE_ASSIGNMENT_ROLE_PUBLIC_IDS_CONTAIN_EMPTY", "La lista de roles no puede contener identificadores públicos vacíos.");

  public static ValidationType UserPublicIdsExceedMaxBulkSize =>
    new("USER_ROLE_ASSIGNMENT_USER_PUBLIC_IDS_EXCEED_MAX_BULK_SIZE", "La lista de usuarios no puede tener más de {0} elementos.", FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination.PaginationDefaults.MaxBulkAssignmentSize);

  public static ValidationType RolePublicIdsExceedMaxBulkSize =>
    new("USER_ROLE_ASSIGNMENT_ROLE_PUBLIC_IDS_EXCEED_MAX_BULK_SIZE", "La lista de roles no puede tener más de {0} elementos.", FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination.PaginationDefaults.MaxBulkAssignmentSize);

  public static ValidationType UserNotFound =>
    new("USER_ROLE_ASSIGNMENT_USER_NOT_FOUND", "No se encontró el usuario '{0}'.");

  public static ValidationType ActiveUserNotFound =>
    new("USER_ROLE_ASSIGNMENT_ACTIVE_USER_NOT_FOUND", "No se encontró el usuario activo '{0}'.");

  public static ValidationType UserInactiveOrDeleted =>
    new("USER_ROLE_ASSIGNMENT_USER_INACTIVE_OR_DELETED", "El usuario '{0}' está inactivo o eliminado.");

  public static ValidationType RoleNotFound =>
    new("USER_ROLE_ASSIGNMENT_ROLE_NOT_FOUND", "No se encontró el rol '{0}'.");

  public static ValidationType ActiveRoleNotFound =>
    new("USER_ROLE_ASSIGNMENT_ACTIVE_ROLE_NOT_FOUND", "No se encontró el rol activo '{0}'.");

  public static ValidationType RoleInactiveOrDeleted =>
    new("USER_ROLE_ASSIGNMENT_ROLE_INACTIVE_OR_DELETED", "El rol '{0}' está inactivo o eliminado.");

  public static ValidationType RelationAlreadyAssigned =>
    new("USER_ROLE_ASSIGNMENT_RELATION_ALREADY_ASSIGNED", "El usuario '{0}' ya tiene asignado el rol '{1}'.");

  public static ValidationType ActiveRolesNotFound =>
    new("USER_ROLE_ASSIGNMENT_ACTIVE_ROLES_NOT_FOUND", "No se encontraron roles activos: {0}");

  public static ValidationType ActiveUsersNotFound =>
    new("USER_ROLE_ASSIGNMENT_ACTIVE_USERS_NOT_FOUND", "No se encontraron usuarios activos: {0}");

  public static ValidationType ActiveRelationNotFound =>
    new("USER_ROLE_ASSIGNMENT_ACTIVE_RELATION_NOT_FOUND", "No existe una relación activa entre el usuario '{0}' y el rol '{1}'.");

  public static ValidationType DeletedRelationNotFound =>
    new("USER_ROLE_ASSIGNMENT_DELETED_RELATION_NOT_FOUND", "No existe una relación eliminada entre el usuario '{0}' y el rol '{1}'.");

  public static ValidationType RelationCannotBeModifiedInactive =>
    new("USER_ROLE_ASSIGNMENT_RELATION_INACTIVE_ENTITIES", "La relación no puede modificarse porque el usuario o el rol no están activos.");

  private static readonly Dictionary<ValidationKey, ValidationType> _dict = new()
    {
        { ValidationKey.UserPublicIdRequired, UserPublicIdRequired },
        { ValidationKey.RolePublicIdRequired, RolePublicIdRequired },
        { ValidationKey.UserPublicIdsRequired, UserPublicIdsRequired },
        { ValidationKey.RolePublicIdsRequired, RolePublicIdsRequired },
        { ValidationKey.UserPublicIdsContainEmpty, UserPublicIdsContainEmpty },
        { ValidationKey.RolePublicIdsContainEmpty, RolePublicIdsContainEmpty },
        { ValidationKey.UserPublicIdsExceedMaxBulkSize, UserPublicIdsExceedMaxBulkSize },
        { ValidationKey.RolePublicIdsExceedMaxBulkSize, RolePublicIdsExceedMaxBulkSize },
        { ValidationKey.UserNotFound, UserNotFound },
        { ValidationKey.ActiveUserNotFound, ActiveUserNotFound },
        { ValidationKey.UserInactiveOrDeleted, UserInactiveOrDeleted },
        { ValidationKey.RoleNotFound, RoleNotFound },
        { ValidationKey.ActiveRoleNotFound, ActiveRoleNotFound },
        { ValidationKey.RoleInactiveOrDeleted, RoleInactiveOrDeleted },
        { ValidationKey.RelationAlreadyAssigned, RelationAlreadyAssigned },
        { ValidationKey.ActiveRolesNotFound, ActiveRolesNotFound },
        { ValidationKey.ActiveUsersNotFound, ActiveUsersNotFound },
        { ValidationKey.ActiveRelationNotFound, ActiveRelationNotFound },
        { ValidationKey.DeletedRelationNotFound, DeletedRelationNotFound },
        { ValidationKey.RelationCannotBeModifiedInactive, RelationCannotBeModifiedInactive },
    };

  public static ValidationType Get(ValidationKey key) => _dict[key];
}
