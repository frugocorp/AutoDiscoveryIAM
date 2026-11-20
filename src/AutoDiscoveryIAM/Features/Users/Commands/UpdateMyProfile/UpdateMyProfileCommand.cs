// -----------------------------------------------------------------------
// <copyright file="UpdateMyProfileCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;

/// <summary>
/// Comando de autoservicio: el propio usuario autenticado actualiza sus datos
/// personales. <paramref name="UserPublicId"/> se resuelve desde los claims del JWT en el
/// endpoint, nunca desde el body de la petición.
/// </summary>
public record UpdateMyProfileCommand(
    Guid UserPublicId,
    string? PhoneNumber);
