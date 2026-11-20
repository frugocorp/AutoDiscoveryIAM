// -----------------------------------------------------------------------
// <copyright file="GetUserByPublicIdOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId.Docs;

public sealed class GetUserByPublicIdOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Usuario recuperado correctamente.";

  protected override object SuccessExample { get; } = new GetUserByPublicIdResponse(
    true,
    new GetUserByPublicIdResponseDto(
        PublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
        Username: "admin",
        Email: "admin@example.com",
        IsActive: true,
        IsAdmin: true,
        IsDevelopment: false,
        IsSuAdmin: false),
    null,
    null);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status403Forbidden.StatusCode] = ErrorInfo.Status403Forbidden,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
  };

  protected override IReadOnlyDictionary<string, ParameterDocInfo>? Parameters { get; } = new Dictionary<string, ParameterDocInfo>
  {
    ["publicId"] = new("PublicId (GUID) del usuario a consultar."),
  };
}
