// -----------------------------------------------------------------------
// <copyright file="UpdateRoleByPublicIdResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;

/// <summary>
/// DTO para la respuesta de actualización de un rol por PublicId.
/// </summary>
/// <remarks>
/// Retornado por <see cref="UpdateRoleByPublicIdEndpoint"/> al cliente HTTP después de una actualización exitosa.
/// Solo expone el PublicId, nunca el Id interno de la base de datos.
/// </remarks>
public record UpdateRoleByPublicIdResponseDto(
    Guid PublicId,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
