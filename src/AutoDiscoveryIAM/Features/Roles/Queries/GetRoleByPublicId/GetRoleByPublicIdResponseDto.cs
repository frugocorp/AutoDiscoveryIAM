// -----------------------------------------------------------------------
// <copyright file="GetRoleByPublicIdResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;

/// <summary>
/// DTO para exponer los datos de un rol en la respuesta de GetRoleByPublicId.
/// Solo expone el PublicId, nunca el Id interno de la base de datos.
/// </summary>
public record GetRoleByPublicIdResponseDto(
    Guid PublicId,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? DeletedAt,
    bool IsDeleted);
