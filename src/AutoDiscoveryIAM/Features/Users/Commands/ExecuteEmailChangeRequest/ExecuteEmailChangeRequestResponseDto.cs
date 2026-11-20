// -----------------------------------------------------------------------
// <copyright file="ExecuteEmailChangeRequestResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ExecuteEmailChangeRequest;

public record ExecuteEmailChangeRequestResponseDto(
    Guid RequestPublicId,
    Guid UserPublicId,
    string NewEmail,
    string Status,
    DateTime? ExecutedAt);
