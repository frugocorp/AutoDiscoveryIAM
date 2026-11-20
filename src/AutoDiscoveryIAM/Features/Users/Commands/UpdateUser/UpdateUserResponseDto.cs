// -----------------------------------------------------------------------
// <copyright file="UpdateUserResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;

/// <summary>
/// DTO de respuesta para la operación de actualización general de usuario.
/// </summary>
/// <remarks>
/// Contiene la información del usuario actualizado tras la modificación.
/// Se utiliza como respuesta del endpoint PUT /users/{publicId}.
/// </remarks>
/// <example>
/// var response = new UpdateUserResponseDto(
///     Guid.Parse("..."),
///     "john.doe",
///     "john.doe@example.com",
///     DateTime.UtcNow,
///     "Usuario actualizado"
/// );
/// </example>
/// <param name="PublicId">Identificador público del usuario actualizado.</param>
/// <param name="Username">Nombre de usuario actualizado.</param>
/// <param name="Email">Email del usuario actualizado.</param>
/// <param name="UpdatedAt">Fecha y hora de la última actualización.</param>
/// <param name="Notes">Notas de la actualización para auditoría.</param>
public record UpdateUserResponseDto(
    Guid PublicId,
    string Username,
    string Email,
    DateTime UpdatedAt,
    string? Notes);
