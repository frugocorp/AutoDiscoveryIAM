// -----------------------------------------------------------------------
// <copyright file="OpenApiTagDescriptions.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

/// <summary>
/// Fuente única de verdad para las descripciones de los tags de Swagger/OpenAPI. En Microsoft.OpenApi
/// 2.x, cada operación referencia un tag por nombre (<c>OpenApiTagReference</c>) contra la lista de
/// <c>OpenApiDocument.Tags</c> -- <see cref="All"/> se usa una sola vez, en <see cref="DocumentTagDescriptionsTransformer"/>,
/// para declarar esa lista completa a nivel de documento.
/// </summary>
public static class OpenApiTagDescriptions
{
  private static readonly Dictionary<string, string> Descriptions = new()
  {
    ["Auth"] = "Autenticación, sesión y verificación de identidad: login, logout, renovación de token y segundo factor.",
    ["Users"] = "Gestión de cuentas de usuario: datos propios, activación, contraseña, 2FA, roles asignados y permisos efectivos.",
    ["Roles"] = "Roles del sistema RBAC: creación, consulta, permisos y usuarios asociados a cada rol.",
    ["Permissions"] = "Permisos del sistema RBAC, generados automáticamente por reflexión sobre los endpoints y su asignación a roles.",
  };

  /// <summary>
  /// Devuelve todos los <see cref="OpenApiTag"/> conocidos, con su descripción única.
  /// </summary>
  public static ISet<OpenApiTag> All()
    => Descriptions
      .Select(kv => new OpenApiTag { Name = kv.Key, Description = kv.Value })
      .ToHashSet();
}
