// -----------------------------------------------------------------------
// <copyright file="UpdateUserCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;

/// <summary>
/// Comando para actualizar información general de un usuario.
/// </summary>
/// <remarks>
/// Este comando permite actualizar datos básicos del usuario como nombre y email.
/// Procesado por <see cref="UpdateUserHandler"/>.
/// </remarks>
/// <param name="PublicId">Identificador público del usuario a actualizar.</param>
/// <param name="Request">Datos de actualización del usuario.</param>
public record UpdateUserCommand(
    Guid PublicId,
    UpdateUserBodyDto Request);
