// -----------------------------------------------------------------------
// <copyright file="DeleteMyAccountResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.DeleteMyAccount;

public record DeleteMyAccountResponse : ApiResponse<DeleteMyAccountResponseDto>
{
  public DeleteMyAccountResponse(bool success, DeleteMyAccountResponseDto? data, string? error = null, ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo) { }
}
