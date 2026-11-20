// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsCommandResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;

/// <summary>
/// Representa la respuesta de la operación de sincronización de permisos.
/// </summary>
/// <remarks>
/// Devuelto por <see cref="SyncPermissionsHandler"/> tras procesar el comando <see cref="SyncPermissionsCommand"/>.
/// </remarks>
public record SyncPermissionsCommandResponse : ApiResponse<SyncPermissionsCommandResponseDto>
{
    /// <summary>
    /// Inicializa una nueva instancia de <see cref="SyncPermissionsCommandResponse"/>.
    /// </summary>
    /// <param name="success">Indica si la sincronización fue exitosa.</param>
    /// <param name="data">Datos del resultado de la sincronización, o <c>null</c> si la sincronización falló.</param>
    /// <param name="error">Mensaje de error en caso de fallo, o <c>null</c> si la operación fue exitosa.</param>
    /// <param name="errorInfo">Objeto con metadatos estructurados del error, o <c>null</c> si la operación fue exitosa.</param>
    public SyncPermissionsCommandResponse(
        bool success,
        SyncPermissionsCommandResponseDto? data,
        string? error = null,
        ErrorInfo? errorInfo = null)
        : base(success, data, error, errorInfo) { }
}
