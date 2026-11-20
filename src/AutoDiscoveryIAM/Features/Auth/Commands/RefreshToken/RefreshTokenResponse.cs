// -----------------------------------------------------------------------
// <copyright file="RefreshTokenResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Respuesta para la operación RefreshToken.
/// </summary>
public record RefreshTokenResponse : ApiResponse<RefreshTokenResponseDto>
{
  public RefreshTokenResponse(bool success, RefreshTokenResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo) { }
}
