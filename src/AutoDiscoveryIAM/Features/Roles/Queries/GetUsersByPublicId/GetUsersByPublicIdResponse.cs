using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId;

public record GetUsersByPublicIdResponse : ApiResponse<IEnumerable<GetUsersByPublicIdResponseDto>>
{
    public GetUsersByPublicIdResponse(bool success, IEnumerable<GetUsersByPublicIdResponseDto>? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo)
    {
    }
}
