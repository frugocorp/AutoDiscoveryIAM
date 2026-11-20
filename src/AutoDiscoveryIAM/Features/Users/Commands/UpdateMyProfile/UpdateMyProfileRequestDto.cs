// -----------------------------------------------------------------------
// <copyright file="UpdateMyProfileRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;

/// <summary>
/// Campos autoservicio actualizables por el propio usuario. Opcional — si se omite, no se
/// modifica.
/// </summary>
public record UpdateMyProfileRequestDto(string? PhoneNumber);
