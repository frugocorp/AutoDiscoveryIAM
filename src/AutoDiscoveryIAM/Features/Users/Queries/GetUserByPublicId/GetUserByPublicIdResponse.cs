// -----------------------------------------------------------------------
// <copyright file="GetUserByPublicIdResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;

public record GetUserByPublicIdResponse : ApiResponse<GetUserByPublicIdResponseDto>
{
  public GetUserByPublicIdResponse(bool success, GetUserByPublicIdResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo) { }
}
