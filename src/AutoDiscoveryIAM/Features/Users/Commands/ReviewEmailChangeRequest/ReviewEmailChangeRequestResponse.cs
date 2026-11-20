// -----------------------------------------------------------------------
// <copyright file="ReviewEmailChangeRequestResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest;

public record ReviewEmailChangeRequestResponse : ApiResponse<ReviewEmailChangeRequestResponseDto>
{
  public ReviewEmailChangeRequestResponse(
      bool success,
      ReviewEmailChangeRequestResponseDto? data,
      string? error = null,
      ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo) { }
}
