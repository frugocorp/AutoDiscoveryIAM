// -----------------------------------------------------------------------
// <copyright file="RequestEmailChangeResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange;

public record RequestEmailChangeResponseDto(
    Guid RequestPublicId,
    string RequestedEmail,
    string Status,
    DateTime CreatedAt);
