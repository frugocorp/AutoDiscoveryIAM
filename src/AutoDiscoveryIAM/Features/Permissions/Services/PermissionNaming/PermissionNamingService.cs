using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Models.Dtos;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Services.PermissionNaming;

/// <summary>
/// Servicio responsable de convertir namespaces de endpoints a permission keys siguiendo
/// las convenciones establecidas del proyecto (feature.action_snake_case).
/// Migrado desde DiscoveryPermissionsService para reutilización en middleware de autorización.
/// </summary>
public class PermissionNamingService : IPermissionNamingService
{
    /// <summary>
    /// Segmento que marca el inicio de la estructura de slices, en la librería y en la app que la consume.
    /// </summary>
    private const string FeaturesSegment = "Features";

    /// <summary>
    /// Método principal que orquesta todo el proceso de conversión namespace → permission key.
    /// Parsea el namespace, extrae las partes necesarias y construye la permission key final.
    /// </summary>
    /// <remarks>
    /// Valida por estructura (<c>...Features.{Feature}[...].{Action}</c>), no por cantidad de segmentos:
    /// el namespace raíz es libre (<c>MiApi</c>, <c>Empresa.Producto</c> o
    /// <c>FrugoCorp.AspNetCore.AutoDiscoveryIAM</c>), porque la misma regla se aplica a los endpoints
    /// de la app que consume el paquete, no solo a los de la librería. La acción es siempre el último
    /// segmento, para que los slices anidados (ej. <c>Login.External.ExchangeLoginCode</c>) sean
    /// permisos propios.
    /// </remarks>
    public PermissionNamingResultDto BuildPermissionNaming(string namespaceName)
    {
        try
        {
            var namespaceParts = ParseNamespaceParts(namespaceName);

            // Validar estructura mínima esperada: {Root}.Features.{Feature}.{Action} -- Root libre.
            var featuresIndex = Array.IndexOf(namespaceParts, FeaturesSegment);
            if (featuresIndex >= 0 && namespaceParts.Length - featuresIndex < 3)
            {
                return CreateErrorResult($"Namespace structure is too short. Expected '<Root>.{FeaturesSegment}.<Feature>.<Action>', got {namespaceParts.Length} parts. Namespace: {namespaceName}");
            }

            var feature = ExtractFeatureFromNamespace(namespaceParts);
            var action = ExtractActionFromNamespace(namespaceParts);
            var cqrsType = ExtractCqrsTypeFromNamespace(namespaceParts);

            // Validar que se pudieron extraer las partes esenciales
            // "...Features.Users.Commands" no tiene acción: el último segmento es la carpeta CQRS.
            if (string.IsNullOrEmpty(feature) || string.IsNullOrEmpty(action) || action is "Commands" or "Queries")
            {
                return CreateErrorResult($"Could not extract feature or action from namespace. Feature: '{feature}', Action: '{action}', Namespace: {namespaceName}");
            }

            var actionSnakeCase = ConvertToSnakeCase(action);
            var key = GeneratePermissionKey(feature, actionSnakeCase);

            return CreateSuccessResult(feature, action, actionSnakeCase, cqrsType, key);
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Unexpected error parsing namespace '{namespaceName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Crea un resultado de error para PermissionNamingResultDto.
    /// </summary>
    private static PermissionNamingResultDto CreateErrorResult(string errorMessage)
    {
        return new PermissionNamingResultDto
        {
            IsValid = false,
            ErrorMessage = errorMessage,
        };
    }

    /// <summary>
    /// Crea un resultado exitoso para PermissionNamingResultDto.
    /// </summary>
    private static PermissionNamingResultDto CreateSuccessResult(string feature, string action, string actionSnakeCase, string cqrsType, string key)
    {
        return new PermissionNamingResultDto
        {
            Feature = feature.ToLowerInvariant(),
            Action = action,
            ActionSnakeCase = actionSnakeCase,
            CqrsType = cqrsType,
            Key = key,
            IsValid = true,
            ErrorMessage = null,
        };
    }

    /// <summary>
    /// Parsea el namespace en sus partes componentes separándolo por puntos.
    /// </summary>
    public string[] ParseNamespaceParts(string? namespaceName)
    {
        return namespaceName?.Split('.') ?? Array.Empty<string>();
    }

    /// <summary>
    /// Extrae el feature del namespace basándose en la posición dentro del array de partes.
    /// Asume estructura: ...Features.{Feature}.Commands/Queries.{Action}
    /// Migrado desde DiscoveryPermissionsService.ExtractFeatureFromNamespace.
    /// </summary>
    public string ExtractFeatureFromNamespace(string[] namespaceParts)
    {
        // Buscar el índice donde aparece "Features"
        var featuresIndex = Array.IndexOf(namespaceParts, "Features");

        if (featuresIndex >= 0 && featuresIndex + 1 < namespaceParts.Length)
        {
            return namespaceParts[featuresIndex + 1];
        }

        return string.Empty;
    }

    /// <summary>
    /// Extrae la acción del namespace basándose en la última parte del array.
    /// La acción representa el nombre de la clase del endpoint.
    /// Migrado desde DiscoveryPermissionsService.ExtractActionFromNamespace.
    /// </summary>
    public string ExtractActionFromNamespace(string[] namespaceParts)
    {
        // La acción suele ser la última parte del namespace
        return namespaceParts.LastOrDefault() ?? string.Empty;
    }

    /// <summary>
    /// Determina el tipo CQRS del namespace basándose en si contiene "Commands" o "Queries".
    /// Migrado desde DiscoveryPermissionsService.ExtractCqrsTypeFromNamespace.
    /// </summary>
    public string ExtractCqrsTypeFromNamespace(string[] namespaceParts)
    {
        if (namespaceParts.Contains("Commands"))
        {
            return "Command";
        }

        if (namespaceParts.Contains("Queries"))
        {
            return "Query";
        }

        return "Unknown";
    }

    /// <summary>
    /// Convierte texto en PascalCase a snake_case siguiendo las reglas del proyecto.
    /// Utilizado para convertir nombres de acciones a formato de permission key.
    /// Migrado desde DiscoveryPermissionsService.ConvertToSnakeCase.
    /// </summary>
    public string ConvertToSnakeCase(string pascalCase)
    {
        if (string.IsNullOrEmpty(pascalCase))
            return string.Empty;

        // Usar regex para insertar guiones bajos antes de letras mayúsculas
        // que estén precedidas por letras minúsculas o números
        var result = System.Text.RegularExpressions.Regex.Replace(
            pascalCase,
            @"(?<=\p{Ll}|\p{N})(?=\p{Lu})",
            "_");

        return result.ToLowerInvariant();
    }

    /// <summary>
    /// Genera la key final del permiso combinando feature y acción en snake_case.
    /// Formato: {feature}.{action_snake_case}
    /// </summary>
    public string GeneratePermissionKey(string feature, string actionSnakeCase)
    {
        return $"{feature.ToLowerInvariant()}.{actionSnakeCase}";
    }
}
