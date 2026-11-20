// -----------------------------------------------------------------------
// <copyright file="UpdateRoleByPublicIdBodyDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateRoleByPublicId;

/// <summary>
/// DTO para los datos del cuerpo de la solicitud de actualización de rol.
/// </summary>
/// <remarks>
/// Contiene únicamente los campos que pueden ser actualizados del rol.
/// <summary>
/// Se valida mediante <see cref="UpdateRoleByPublicIdCommandValidator"/>.
/// </summary>
/// </remarks>
/// <example>
/// var bodyDto = new UpdateRoleByPublicIdBodyDto("Administrator", "Rol para administración del sistema");
/// </example>
/// <param name="Name">Nombre actualizado del rol. Campo requerido.</param>
/// <param name="Description">Descripción actualizada del rol. Campo opcional.</param>
public record UpdateRoleByPublicIdBodyDto(
    [property: NotNull] string Name,
    string? Description);
