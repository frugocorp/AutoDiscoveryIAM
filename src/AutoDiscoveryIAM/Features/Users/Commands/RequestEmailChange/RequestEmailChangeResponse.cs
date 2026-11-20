// -----------------------------------------------------------------------
// <copyright file="RequestEmailChangeResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange;

public record RequestEmailChangeResponse : ApiResponse<RequestEmailChangeResponseDto>
{
  public RequestEmailChangeResponse(
      bool success,
      RequestEmailChangeResponseDto? data,
      string? error = null,
      ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo) { }
}
