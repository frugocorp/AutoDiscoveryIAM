// -----------------------------------------------------------------------
// <copyright file="DeleteRoleByPublicIdRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;

/// <summary>
/// DTO para la solicitud de eliminación de un rol por PublicId.
/// </summary>
/// <remarks>
/// Utilizado con [AsParameters] para mapear automáticamente el PublicId desde la ruta.
/// Utilizado en el endpoint de eliminación para recibir la solicitud desde el cliente HTTP.
/// </remarks>
/// <param name="PublicId">PublicId del rol a eliminar, obtenido desde la ruta {publicId:guid}.</param>
public record DeleteRoleByPublicIdRequestDto(
    [FromRoute] Guid PublicId);