// -----------------------------------------------------------------------
// <copyright file="GetPermissionsResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetPermissions;

/// <summary>
/// Respuesta de la query GetPermissions, contiene la lista de permisos recuperados.
/// </summary>
public record GetPermissionsResponse : ApiResponse<PagedResultDto<GetPermissionsResponseDto>>
{
    public GetPermissionsResponse(bool success, PagedResultDto<GetPermissionsResponseDto>? data, string? error = null, ErrorInfo? errorInfo = null)
        : base(success, data, error, errorInfo) { }
}
