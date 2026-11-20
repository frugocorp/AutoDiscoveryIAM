using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteUserRolesByPublicId;

public record DeleteUserRolesByPublicIdResponse : ApiResponse<DeleteUserRolesByPublicIdResponseDto>
{
    public DeleteUserRolesByPublicIdResponse(bool success, DeleteUserRolesByPublicIdResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo)
    {
    }
}
