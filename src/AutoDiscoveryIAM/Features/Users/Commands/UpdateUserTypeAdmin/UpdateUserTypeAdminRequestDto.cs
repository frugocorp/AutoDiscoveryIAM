// -----------------------------------------------------------------------
// <copyright file="UpdateUserTypeAdminRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;

/// <summary>
/// DTO que encapsula la solicitud completa para cambiar tipo de usuario a Admin.
/// </summary>
/// <remarks>
/// Combina el PublicId de la ruta con el cuerpo de la solicitud para el endpoint.
/// Usado en el endpoint PUT /users/{publicId}/admin.
/// </remarks>
/// <example>
/// var request = new UpdateUserTypeAdminRequestDto(Guid.Parse("..."), new UpdateUserTypeAdminBodyDto(true, "Promoción"));
/// </example>
/// <param name="PublicId">PublicId del usuario desde la ruta URL.</param>
/// <param name="Body">Datos del cuerpo de la solicitud.</param>
public record UpdateUserTypeAdminRequestDto(
    [FromRoute] Guid PublicId,
    [FromBody] UpdateUserTypeAdminBodyDto Body);
