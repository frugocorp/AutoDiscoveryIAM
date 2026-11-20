// -----------------------------------------------------------------------
// <copyright file="ExchangeLoginCodeResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login.External.ExchangeLoginCode;

/// <summary>
/// Reutiliza <see cref="LoginResponseDto"/> tal cual -- el código canjeado representa exactamente
/// la misma sesión (Token/ExpiresAt/RefreshToken) que un login normal o el callback social sin
/// redirect habrían devuelto.
/// </summary>
public record ExchangeLoginCodeResponse : ApiResponse<LoginResponseDto>
{
  public ExchangeLoginCodeResponse(bool success, LoginResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
    : base(success, data, error, errorInfo)
  {
  }
}
