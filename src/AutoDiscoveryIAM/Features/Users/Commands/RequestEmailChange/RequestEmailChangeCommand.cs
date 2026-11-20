// -----------------------------------------------------------------------
// <copyright file="RequestEmailChangeCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange;

/// <summary>
/// <paramref name="UserPublicId"/> se resuelve desde los claims del usuario autenticado
/// (self-service) en el endpoint, nunca desde el body de la petición.
/// </summary>
public record RequestEmailChangeCommand(Guid UserPublicId, string NewEmail);
