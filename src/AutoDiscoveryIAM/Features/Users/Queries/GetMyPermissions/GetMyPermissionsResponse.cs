using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;

public record GetMyPermissionsResponse : ApiResponse<IEnumerable<GetMyPermissionsResponseDto>>
{
  public GetMyPermissionsResponse(bool success, IEnumerable<GetMyPermissionsResponseDto>? data, string? error = null, ErrorInfo? errorInfo = null)
    : base(success, data, error, errorInfo)
  {
  }
}
