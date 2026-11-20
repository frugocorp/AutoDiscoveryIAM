// -----------------------------------------------------------------------
// <copyright file="ExecuteEmailChangeRequestResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ExecuteEmailChangeRequest;

public record ExecuteEmailChangeRequestResponse : ApiResponse<ExecuteEmailChangeRequestResponseDto>
{
  public ExecuteEmailChangeRequestResponse(
      bool success,
      ExecuteEmailChangeRequestResponseDto? data,
      string? error = null,
      ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo) { }
}
