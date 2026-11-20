// -----------------------------------------------------------------------
// <copyright file="ReviewEmailChangeRequestResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest;

public record ReviewEmailChangeRequestResponseDto(
    Guid RequestPublicId,
    string Status,
    DateTime? ReviewedAt,
    string? RejectionReason);
