// -----------------------------------------------------------------------
// <copyright file="UpdateUserTypeAdminCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUserTypeAdmin;

/// <summary>
/// Comando para cambiar el tipo de usuario a Admin mediante su PublicId.
/// </summary>
/// <remarks>
/// Este comando encapsula los datos necesarios para promover un usuario a Admin, siguiendo el patrón CQRS.
/// Utilizado por <see cref="UpdateUserTypeAdminHandler"/>.
/// Solo usuarios SuAdmin y Development pueden ejecutar esta operación crítica.
/// </remarks>
/// <example>
/// var command = new UpdateUserTypeAdminCommand(Guid.Parse("..."), new UpdateUserTypeAdminBodyDto(true, "Promovido a Admin por cambio de rol"));
/// </example>
/// <param name="PublicId">PublicId del usuario a promover a Admin.</param>
/// <param name="Request">Datos de la promoción. Validados por <see cref="UpdateUserTypeAdminCommandValidator"/>.</param>
public record UpdateUserTypeAdminCommand(Guid PublicId, UpdateUserTypeAdminBodyDto Request);
