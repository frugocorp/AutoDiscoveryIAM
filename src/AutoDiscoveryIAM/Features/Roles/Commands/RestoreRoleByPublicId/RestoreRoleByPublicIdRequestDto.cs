// -----------------------------------------------------------------------
// <copyright file="RestoreRoleByPublicIdRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.RestoreRoleByPublicId;

/// <summary>
/// DTO de request para restaurar un rol por PublicId.
/// </summary>
/// <remarks>
/// Utiliza el patrón [AsParameters] para combinar parámetros de ruta y query.
/// El PublicId se obtiene de la ruta del endpoint.
/// </remarks>
/// <param name="PublicId">Identificador público del rol a restaurar obtenido de la ruta.</param>
public record RestoreRoleByPublicIdRequestDto(
    [FromRoute] Guid PublicId);