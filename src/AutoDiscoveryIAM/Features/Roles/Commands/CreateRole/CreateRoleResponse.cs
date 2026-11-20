// -----------------------------------------------------------------------
// <copyright file="CreateRoleResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;

public record CreateRoleResponse : ApiResponse<CreateRoleResponseDto>
{
    public CreateRoleResponse(
        bool success,
        CreateRoleResponseDto? data,
        string? error = null,
        ErrorInfo? errorInfo = null)
        : base(success, data, error, errorInfo) { }
}
