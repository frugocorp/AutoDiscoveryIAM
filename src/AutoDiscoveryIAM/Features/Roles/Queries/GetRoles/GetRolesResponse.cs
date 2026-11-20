// -----------------------------------------------------------------------
// <copyright file="GetRolesResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoles;

public record GetRolesResponse : ApiResponse<PagedResultDto<GetRolesResponseDto>>
{
  public GetRolesResponse(
      bool success,
      PagedResultDto<GetRolesResponseDto>? data,
      string? error = null,
      ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo) { }
}
