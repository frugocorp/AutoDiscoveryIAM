// -----------------------------------------------------------------------
// <copyright file="GetUserByPublicIdRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;

/// <summary>
/// DTO de request para obtener un usuario por PublicId. Solo acepta PublicId, nunca el Id interno.
/// </summary>
public class GetUserByPublicIdRequestDto
{
  public Guid PublicId { get; set; }
}
