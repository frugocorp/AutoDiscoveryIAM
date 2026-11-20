// -----------------------------------------------------------------------
// <copyright file="CreateUserResponse.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;

public record CreateUserResponse : ApiResponse<CreateUserResponseDto>
{
  public CreateUserResponse(
      bool success,
      CreateUserResponseDto? data,
      string? error = null,
      ErrorInfo? errorInfo = null)
      : base(success, data, error, errorInfo) { }
}
