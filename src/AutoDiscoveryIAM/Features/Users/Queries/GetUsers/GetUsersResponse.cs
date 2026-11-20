// -----------------------------------------------------------------------
// <copyright file="GetUsersResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;

public record GetUsersResponse : ApiResponse<PagedResultDto<GetUsersResponseDto>>
{
  public GetUsersResponse(
      bool success,
      PagedResultDto<GetUsersResponseDto>? data,
      string? error = null,
      global::FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo)
  {
  }
}
