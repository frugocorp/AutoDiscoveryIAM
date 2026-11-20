// -----------------------------------------------------------------------
// <copyright file="UpdateUserTypeAdminBodyDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;

/// <summary>
/// DTO para los datos del cuerpo de la solicitud de cambio de tipo de usuario a Admin.
/// </summary>
/// <remarks>
/// Contiene los campos necesarios para la promoción de un usuario a Admin.
/// Se valida mediante <see cref="UpdateUserTypeAdminCommandValidator"/>.
/// </remarks>
/// <example>
/// var bodyDto = new UpdateUserTypeAdminBodyDto(true, "Usuario promovido a Admin por reorganización");
/// </example>
/// <param name="IsAdmin">Indicador de si el usuario debe ser Admin. Campo requerido.</param>
/// <param name="Reason">Razón del cambio de tipo. Campo opcional para auditoría.</param>
public record UpdateUserTypeAdminBodyDto(
    [property: NotNull] bool IsAdmin,
    string? Reason);
