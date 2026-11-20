// -----------------------------------------------------------------------
// <copyright file="UpdateUserBodyDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;

/// <summary>
/// DTO para los datos del cuerpo de la solicitud de actualización de usuario.
/// </summary>
/// <remarks>
/// Contiene los campos actualizables básicos del usuario.
/// Se valida mediante <see cref="UpdateUserCommandValidator"/>.
/// </remarks>
/// <example>
/// var bodyDto = new UpdateUserBodyDto("john.doe", "john.doe@example.com", "Usuario actualizado");
/// </example>
/// <param name="Username">Nuevo nombre de usuario. Campo opcional.</param>
/// <param name="Email">Nuevo email del usuario. Campo opcional.</param>
/// <param name="Notes">Notas sobre la actualización. Campo opcional para auditoría.</param>
public record UpdateUserBodyDto(
    [StringLength(50, MinimumLength = 3)] string? Username,
    [EmailAddress] string? Email,
    string? Notes);
