// -----------------------------------------------------------------------
// <copyright file="UpdateUserOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser.Docs;

public sealed class UpdateUserOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new ApiResponse<UpdateUserResponseDto>(
    true,
    new UpdateUserResponseDto(
      PublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
      Username: "john.doe.updated",
      Email: "john.doe.updated@example.com",
      UpdatedAt: DateTime.UtcNow,
      Notes: "Actualización de información personal del usuario"),
    null,
    null);

  protected override object RequestExample { get; } = new UpdateUserBodyDto(
    Username: "john.doe.updated",
    Email: "john.doe.updated@example.com",
    Notes: "Actualización de información personal del usuario");

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status403Forbidden.StatusCode] = ErrorInfo.Status403Forbidden,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
    [ErrorInfo.Status409Conflict.StatusCode] = ErrorInfo.Status409Conflict,
  };

  protected override IReadOnlyDictionary<string, ParameterDocInfo>? Parameters { get; } = new Dictionary<string, ParameterDocInfo>
  {
    ["publicId"] = new("PublicId (GUID) del usuario a actualizar.", "550e8400-e29b-41d4-a716-446655440000"),
  };
}
