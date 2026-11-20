// -----------------------------------------------------------------------
// <copyright file="DeleteMyAccountCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount;

/// <summary>
/// Comando de autoservicio: el propio usuario autenticado elimina (soft-delete) su
/// propia cuenta. <paramref name="UserPublicId"/> se resuelve desde los claims del JWT en el
/// endpoint, nunca desde el body de la petición; <paramref name="CurrentPassword"/> sí viene del body.
/// </summary>
public record DeleteMyAccountCommand(Guid UserPublicId, string? CurrentPassword);
