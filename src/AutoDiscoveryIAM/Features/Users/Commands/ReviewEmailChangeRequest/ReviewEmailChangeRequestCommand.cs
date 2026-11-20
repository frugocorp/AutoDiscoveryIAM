// -----------------------------------------------------------------------
// <copyright file="ReviewEmailChangeRequestCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest;

public record ReviewEmailChangeRequestCommand(Guid RequestPublicId, bool Approve, string? RejectionReason);
