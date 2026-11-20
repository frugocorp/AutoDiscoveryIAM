// -----------------------------------------------------------------------
// <copyright file="UpdateMyProfileResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;

public record UpdateMyProfileResponse : ApiResponse<UpdateMyProfileResponseDto>
{
  public UpdateMyProfileResponse(bool success, UpdateMyProfileResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo) { }
}
