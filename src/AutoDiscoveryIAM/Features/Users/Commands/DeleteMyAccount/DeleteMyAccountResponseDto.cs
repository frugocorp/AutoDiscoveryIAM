// -----------------------------------------------------------------------
// <copyright file="DeleteMyAccountResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount;

public record DeleteMyAccountResponseDto(Guid PublicId, string Status);
