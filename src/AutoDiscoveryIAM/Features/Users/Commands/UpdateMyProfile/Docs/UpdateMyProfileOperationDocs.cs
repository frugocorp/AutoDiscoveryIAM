// -----------------------------------------------------------------------
// <copyright file="UpdateMyProfileOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile.Docs;

public sealed class UpdateMyProfileOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new UpdateMyProfileResponse(
    true,
    new UpdateMyProfileResponseDto(
        Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
        "+521234567890",
        DateTime.UtcNow));

  protected override object RequestExample { get; } = new UpdateMyProfileRequestDto(PhoneNumber: "+521234567890");

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
    [ErrorInfo.Status409Conflict.StatusCode] = ErrorInfo.Status409Conflict,
  };
}
