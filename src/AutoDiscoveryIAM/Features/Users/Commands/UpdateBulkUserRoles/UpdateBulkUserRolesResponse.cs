using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles;

public record UpdateBulkUserRolesResponse : ApiResponse<UpdateBulkUserRolesResponseDto>
{
  public UpdateBulkUserRolesResponse(bool success, UpdateBulkUserRolesResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
    : base(success, data, error, errorInfo)
  {
  }
}
