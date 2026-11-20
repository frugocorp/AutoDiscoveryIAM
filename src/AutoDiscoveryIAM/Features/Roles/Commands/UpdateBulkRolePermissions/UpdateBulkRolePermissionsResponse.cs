using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions;

public record UpdateBulkRolePermissionsResponse : ApiResponse<UpdateBulkRolePermissionsResponseDto>
{
    public UpdateBulkRolePermissionsResponse(bool success, UpdateBulkRolePermissionsResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
        : base(success, data, error, errorInfo)
    {
    }
}
