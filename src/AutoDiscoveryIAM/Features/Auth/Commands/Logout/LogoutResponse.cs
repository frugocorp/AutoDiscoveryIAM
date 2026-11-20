// -----------------------------------------------------------------------
// <copyright file="LogoutResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;

/// <summary>
/// Respuesta para la operación Logout.
/// </summary>
public record LogoutResponse : ApiResponse<LogoutResponseDto>
{
  public LogoutResponse(bool success, LogoutResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo) { }
}
