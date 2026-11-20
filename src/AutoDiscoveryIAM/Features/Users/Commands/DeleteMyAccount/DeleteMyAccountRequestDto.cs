// -----------------------------------------------------------------------
// <copyright file="DeleteMyAccountRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount;

/// <summary>
/// Cuerpo de <c>DELETE /users/me</c>: la contraseña actual confirma que quien elimina la cuenta es su
/// dueño, no quien solo tenga un access token robado.
/// </summary>
public sealed record DeleteMyAccountRequestDto(string? CurrentPassword);
