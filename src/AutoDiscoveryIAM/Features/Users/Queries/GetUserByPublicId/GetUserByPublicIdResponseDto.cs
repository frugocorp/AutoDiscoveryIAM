// -----------------------------------------------------------------------
// <copyright file="GetUserByPublicIdResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;

/// <summary>
/// DTO para exponer los datos de un usuario en la respuesta de GetUserByPublicId.
/// Solo expone el PublicId, nunca el Id interno de la base de datos.
/// </summary>
public record GetUserByPublicIdResponseDto(
    Guid PublicId,
    string Username,
    string Email,
    bool IsActive,
    bool IsAdmin,
    bool IsDevelopment,
    bool IsSuAdmin)
{
  public static GetUserByPublicIdResponseDto FromEntity(User user)
  {
    return new GetUserByPublicIdResponseDto(
        user.PublicId,
        user.Username,
        user.Email,
        user.IsActive,
        user.IsAdmin,
        user.IsDevelopment,
        user.IsSuAdmin);
  }
}
