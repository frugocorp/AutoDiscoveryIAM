using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

namespace TuProyecto.Features.UserProfiles.Queries.GetUserProfiles;

public record GetUserProfilesResponse : ApiResponse<PagedResultDto<GetUserProfilesResponseDto>>
{
  public GetUserProfilesResponse(bool success, PagedResultDto<GetUserProfilesResponseDto>? data, string? error = null, ErrorInfo? errorInfo = null)
    : base(success, data, error, errorInfo) { }
}
