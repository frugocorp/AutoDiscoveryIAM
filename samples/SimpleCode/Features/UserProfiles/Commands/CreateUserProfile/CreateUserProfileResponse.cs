using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace TuProyecto.Features.UserProfiles.Commands.CreateUserProfile;

public record CreateUserProfileResponse : ApiResponse<CreateUserProfileResponseDto>
{
  public CreateUserProfileResponse(bool success, CreateUserProfileResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
    : base(success, data, error, errorInfo) { }
}
