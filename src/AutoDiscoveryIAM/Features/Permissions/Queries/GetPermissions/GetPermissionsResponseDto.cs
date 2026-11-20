// -----------------------------------------------------------------------
// <copyright file="GetPermissionsResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetPermissions;

/// <summary>
/// DTO para exponer los datos de un permiso en la respuesta de GetPermissions.
/// Solo expone el PublicId, nunca el Id interno de la base de datos.
/// </summary>
public record GetPermissionsResponseDto(
    Guid PublicId,
    string Key,
    string? Description,
    string? Group,
    bool IsActive,
    string? Route,
    string? HttpMethod,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
