using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Dtos;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Services.PermissionNaming;

/// <summary>
/// Servicio responsable de convertir namespaces de endpoints a permission keys siguiendo
/// las convenciones establecidas del proyecto (feature.action_snake_case).
/// </summary>
public interface IPermissionNamingService
{
    /// <summary>
    /// Método principal que orquesta todo el proceso de conversión namespace → permission key.
    /// Parsea el namespace, extrae las partes necesarias y construye la permission key final.
    /// </summary>
    /// <param name="namespaceName">
    /// El namespace completo del endpoint.
    /// Ejemplo: "FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Posts.Commands.RestorePostByPublicId"
    /// </param>
    /// <returns>
    /// PermissionNamingResultDto con el resultado del proceso de conversión.
    /// Incluye IsValid = true/false para indicar éxito/error.
    /// </returns>
    PermissionNamingResultDto BuildPermissionNaming(string namespaceName);

    /// <summary>
    /// Parsea el namespace en sus partes componentes separándolo por puntos.
    /// </summary>
    /// <param name="namespaceName">
    /// El namespace a parsear. Puede ser null o empty.
    /// </param>
    /// <returns>
    /// Array de strings con las partes del namespace.
    /// Retorna array vacío si namespaceName es null/empty.
    /// </returns>
    string[] ParseNamespaceParts(string? namespaceName);

    /// <summary>
    /// Extrae el feature del namespace basándose en la posición dentro del array de partes.
    /// Asume estructura: ...Features.{Feature}.Commands/Queries.{Action}
    /// </summary>
    /// <param name="namespaceParts">
    /// Array con las partes del namespace obtenido de ParseNamespaceParts.
    /// </param>
    /// <returns>
    /// Nombre del feature en lowercase.
    /// Ejemplo: "Posts" → "posts"
    /// </returns>
    string ExtractFeatureFromNamespace(string[] namespaceParts);

    /// <summary>
    /// Extrae la acción del namespace basándose en la última parte del array.
    /// La acción representa el nombre de la clase del endpoint.
    /// </summary>
    /// <param name="namespaceParts">
    /// Array con las partes del namespace obtenido de ParseNamespaceParts.
    /// </param>
    /// <returns>
    /// Nombre de la acción en su formato original PascalCase.
    /// Ejemplo: "RestorePostByPublicId"
    /// </returns>
    string ExtractActionFromNamespace(string[] namespaceParts);

    /// <summary>
    /// Determina el tipo CQRS del namespace basándose en si contiene "Commands" o "Queries".
    /// </summary>
    /// <param name="namespaceParts">
    /// Array con las partes del namespace obtenido de ParseNamespaceParts.
    /// </param>
    /// <returns>
    /// Tipo CQRS: "Command" o "Query"
    /// </returns>
    string ExtractCqrsTypeFromNamespace(string[] namespaceParts);

    /// <summary>
    /// Convierte texto en PascalCase a snake_case siguiendo las reglas del proyecto.
    /// Utilizado para convertir nombres de acciones a formato de permission key.
    /// </summary>
    /// <param name="pascalCase">
    /// Texto en PascalCase a convertir.
    /// Ejemplo: "RestorePostByPublicId"
    /// </param>
    /// <returns>
    /// Texto convertido a snake_case.
    /// Ejemplo: "restore_post_by_public_id"
    /// </returns>
    string ConvertToSnakeCase(string pascalCase);

    /// <summary>
    /// Genera la key final del permiso combinando feature y acción en snake_case.
    /// Formato: {feature}.{action_snake_case}
    /// </summary>
    /// <param name="feature">
    /// Nombre del feature en lowercase.
    /// Ejemplo: "posts"
    /// </param>
    /// <param name="actionSnakeCase">
    /// Nombre de la acción en snake_case.
    /// Ejemplo: "restore_post_by_public_id"
    /// </param>
    /// <returns>
    /// Permission key completa.
    /// Ejemplo: "posts.restore_post_by_public_id"
    /// </returns>
    string GeneratePermissionKey(string feature, string actionSnakeCase);
}
