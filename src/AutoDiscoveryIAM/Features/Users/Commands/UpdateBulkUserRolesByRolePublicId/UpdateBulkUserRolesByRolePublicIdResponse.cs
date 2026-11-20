using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId;

public record UpdateBulkUserRolesByRolePublicIdResponse : ApiResponse<UpdateBulkUserRolesByRolePublicIdResponseDto>
{
    public UpdateBulkUserRolesByRolePublicIdResponse(bool success, UpdateBulkUserRolesByRolePublicIdResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo)
    {
    }
}
