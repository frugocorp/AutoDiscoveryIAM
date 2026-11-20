// -----------------------------------------------------------------------
// <copyright file="GetRoleByPublicIdRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;

/// <summary>
/// DTO de request para obtener un rol por PublicId.
/// Solo acepta PublicId, nunca el Id interno de la base de datos.
/// </summary>
public class GetRoleByPublicIdRequestDto
{
    public Guid PublicId { get; set; }
}
