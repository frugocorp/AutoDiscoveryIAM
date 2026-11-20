// -----------------------------------------------------------------------
// <copyright file="UpdateUserRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;

/// <summary>
/// DTO que representa la solicitud completa para actualizar un usuario.
/// </summary>
/// <remarks>
/// Combina el PublicId desde la ruta URL con los datos del cuerpo de la solicitud.
/// Utilizado con [AsParameters] en el endpoint para mapeo automático.
/// </remarks>
/// <param name="PublicId">Identificador público del usuario desde la URL.</param>
/// <param name="Body">Datos de actualización del usuario desde el cuerpo de la solicitud.</param>
public record UpdateUserRequestDto(
    [FromRoute] Guid PublicId,
    [FromBody] UpdateUserBodyDto Body);
