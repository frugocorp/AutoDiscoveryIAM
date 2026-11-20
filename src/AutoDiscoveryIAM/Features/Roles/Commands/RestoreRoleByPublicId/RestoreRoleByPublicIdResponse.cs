// -----------------------------------------------------------------------
// <copyright file="RestoreRoleByPublicIdResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;

/// <summary>
/// Respuesta del comando de restauración de rol por PublicId.
/// </summary>
/// <remarks>
/// Encapsula el resultado de la operación de restauración siguiendo el patrón
/// de respuesta consistente de la API. Hereda de ApiResponse para mantener
/// la estructura uniforme en todas las operaciones.
/// </remarks>
public record RestoreRoleByPublicIdResponse : ApiResponse<RestoreRoleByPublicIdResponseDto>
{
    /// <summary>
    /// Inicializa una nueva instancia de RestoreRoleByPublicIdResponse.
    /// </summary>
    /// <param name="success">Indica si la operación fue exitosa.</param>
    /// <param name="data">Datos del rol restaurado en caso de éxito.</param>
    /// <param name="error">Mensaje de error en caso de fallo.</param>
    /// <param name="errorInfo">Información detallada del error.</param>
    public RestoreRoleByPublicIdResponse(
        bool success,
        RestoreRoleByPublicIdResponseDto? data,
        string? error = null,
        ErrorInfo? errorInfo = null)
        : base(success, data, error, errorInfo) { }
}