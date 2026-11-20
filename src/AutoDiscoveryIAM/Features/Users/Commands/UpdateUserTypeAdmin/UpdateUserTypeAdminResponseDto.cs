// -----------------------------------------------------------------------
// <copyright file="UpdateUserTypeAdminResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;

/// <summary>
/// DTO de respuesta para la operación de cambio de tipo Admin de usuario.
/// </summary>
/// <remarks>
/// Contiene la información del usuario actualizado tras el cambio de tipo Admin.
/// Se utiliza como respuesta del endpoint PUT /users/{publicId}/admin.
/// </remarks>
/// <example>
/// var response = new UpdateUserTypeAdminResponseDto(
///     Guid.Parse("..."),
///     "john.doe",
///     "john@example.com",
///     true,
///     "Promovido por reorganización"
/// );
/// </example>
/// <param name="PublicId">Identificador público del usuario actualizado.</param>
/// <param name="Username">Nombre de usuario actualizado.</param>
/// <param name="Email">Email del usuario actualizado.</param>
/// <param name="IsAdmin">Nuevo estado Admin del usuario.</param>
/// <param name="Reason">Razón del cambio de tipo para auditoría.</param>
public record UpdateUserTypeAdminResponseDto(
    Guid PublicId,
    string Username,
    string Email,
    bool IsAdmin,
    string? Reason);
