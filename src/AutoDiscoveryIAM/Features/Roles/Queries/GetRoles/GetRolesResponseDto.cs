// -----------------------------------------------------------------------
// <copyright file="GetRolesResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoles;

/// <summary>
/// DTO para exponer los datos de un rol en la respuesta de GetRoles.
/// Solo expone el PublicId, nunca el Id interno de la base de datos.
/// </summary>
public record GetRolesResponseDto(
    Guid PublicId,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? DeletedAt,
    bool IsDeleted);
