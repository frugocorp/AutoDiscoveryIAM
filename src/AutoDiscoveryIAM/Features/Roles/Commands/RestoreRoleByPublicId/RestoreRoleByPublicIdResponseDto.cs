// -----------------------------------------------------------------------
// <copyright file="RestoreRoleByPublicIdResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;

/// <summary>
/// DTO de respuesta para la restauración de rol por PublicId.
/// </summary>
/// <remarks>
/// Contiene la información del rol que fue restaurado exitosamente,
/// incluyendo detalles relevantes para el cliente de la API.
/// </remarks>
public class RestoreRoleByPublicIdResponseDto
{
    /// <summary>
    /// Identificador público del rol restaurado.
    /// </summary>
    public Guid PublicId { get; set; }

    /// <summary>
    /// Nombre del rol restaurado.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Estado actual del rol.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp de cuándo se realizó la restauración.
    /// </summary>
    public DateTime? RestoredAt { get; set; }
}