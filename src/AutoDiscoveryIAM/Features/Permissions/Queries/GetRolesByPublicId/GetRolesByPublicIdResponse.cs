using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId;

public record GetRolesByPublicIdResponse : ApiResponse<IEnumerable<GetRolesByPublicIdResponseDto>>
{
    public GetRolesByPublicIdResponse(bool success, IEnumerable<GetRolesByPublicIdResponseDto>? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo)
    {
    }
}
