using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRolePermissionsByPermissionPublicId;

public record DeleteRolePermissionsByPermissionPublicIdResponse : ApiResponse<DeleteRolePermissionsByPermissionPublicIdResponseDto>
{
    public DeleteRolePermissionsByPermissionPublicIdResponse(bool success, DeleteRolePermissionsByPermissionPublicIdResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo)
    {
    }
}
