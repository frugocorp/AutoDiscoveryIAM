// -----------------------------------------------------------------------
// <copyright file="UpdateMyProfileResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;

public record UpdateMyProfileResponseDto(
    Guid PublicId,
    string? PhoneNumber,
    DateTime? UpdatedAt);
