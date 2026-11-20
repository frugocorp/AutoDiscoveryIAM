namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Dtos;

/// <summary>
/// DTO que encapsula el resultado del proceso de conversión de namespace a permission key.
/// Contiene tanto la información extraída como el resultado de validación.
/// </summary>
public class PermissionNamingResultDto
{
    /// <summary>
    /// Nombre del feature en formato lowercase.
    /// Ejemplo: "posts", "users", "roles"
    /// </summary>
    public string Feature { get; init; } = string.Empty;

    /// <summary>
    /// Nombre de la acción extraída en su formato original PascalCase.
    /// Ejemplo: "RestorePostByPublicId", "CreateUser", "GetRoles"
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// Nombre de la acción convertida a snake_case.
    /// Ejemplo: "restore_post_by_public_id", "create_user", "get_roles"
    /// </summary>
    public string ActionSnakeCase { get; init; } = string.Empty;

    /// <summary>
    /// Tipo CQRS determinado del namespace.
    /// Valores posibles: "Command", "Query"
    /// </summary>
    public string CqrsType { get; init; } = string.Empty;

    /// <summary>
    /// Permission key final construida en formato feature.action_snake_case.
    /// Ejemplo: "posts.restore_post_by_public_id", "users.create_user"
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Indica si el proceso de parsing y construcción fue exitoso.
    /// Si es false, revisar ErrorMessage para detalles del error.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Mensaje de error en caso de que IsValid sea false.
    /// Null o empty si el proceso fue exitoso.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
