// -----------------------------------------------------------------------
// <copyright file="DeleteRoleByPublicIdResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;

/// <summary>
/// DTO para la respuesta de eliminación de un rol por PublicId.
/// </summary>
/// <remarks>
/// Retornado por el endpoint al cliente HTTP después de una eliminación exitosa.
/// Solo expone el PublicId, nunca el Id interno de la base de datos.
/// </remarks>
public record DeleteRoleByPublicIdResponseDto
{
    public Guid PublicId { get; init; }
    public string Status { get; init; } = string.Empty;
}