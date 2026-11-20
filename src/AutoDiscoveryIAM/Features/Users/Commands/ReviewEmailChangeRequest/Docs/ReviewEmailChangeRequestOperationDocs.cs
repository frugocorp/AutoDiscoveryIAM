// -----------------------------------------------------------------------
// <copyright file="ReviewEmailChangeRequestOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ReviewEmailChangeRequest.Docs;

/// <summary>
/// Datos de documentación de <c>ReviewEmailChangeRequestEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class ReviewEmailChangeRequestOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new ReviewEmailChangeRequestResponse(
    true,
    new ReviewEmailChangeRequestResponseDto(
      Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
      EmailChangeRequestStatusCatalog.ApprovedForExecution,
      DateTime.UtcNow,
      null));

  protected override object RequestExample { get; } = new ReviewEmailChangeRequestBodyDto(Approve: true, RejectionReason: null);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status403Forbidden.StatusCode] = ErrorInfo.Status403Forbidden,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
    [ErrorInfo.Status409Conflict.StatusCode] = ErrorInfo.Status409Conflict,
  };
}
