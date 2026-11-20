using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId;

public record GetPermissionsByPublicIdResponse : ApiResponse<IEnumerable<GetPermissionsByPublicIdResponseDto>>
{
    public GetPermissionsByPublicIdResponse(bool success, IEnumerable<GetPermissionsByPublicIdResponseDto>? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo)
    {
    }
}
