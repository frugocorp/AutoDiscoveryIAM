// -----------------------------------------------------------------------
// <copyright file="CreateUserResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;

public record CreateUserResponseDto(Guid PublicId, string Email, string Username, string Status);
