// -----------------------------------------------------------------------
// <copyright file="GetUsersResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;

public record GetUsersResponseDto(
    Guid PublicId,
    string Username,
    string Email,
    bool IsActive,
    bool IsAdmin,
    bool IsDevelopment,
    bool IsSuAdmin)
{
  public static GetUsersResponseDto FromEntity(User user)
  {
    return new GetUsersResponseDto(
        user.PublicId,
        user.Username,
        user.Email,
        user.IsActive,
        user.IsAdmin,
        user.IsDevelopment,
        user.IsSuAdmin);
  }
}
