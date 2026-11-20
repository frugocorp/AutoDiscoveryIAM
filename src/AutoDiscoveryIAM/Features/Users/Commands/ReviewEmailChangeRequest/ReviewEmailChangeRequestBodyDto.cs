// -----------------------------------------------------------------------
// <copyright file="ReviewEmailChangeRequestBodyDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest;

public record ReviewEmailChangeRequestBodyDto(bool Approve, string? RejectionReason);
