// -----------------------------------------------------------------------
// <copyright file="GetRoleByPublicIdResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;

/// <summary>
/// Response para la query GetRoleByPublicId (estructura CQRS).
/// </summary>
public record GetRoleByPublicIdResponse : ApiResponse<GetRoleByPublicIdResponseDto>
{
    public GetRoleByPublicIdResponse(bool success, GetRoleByPublicIdResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
        : base(success, data, error, errorInfo) { }
}
