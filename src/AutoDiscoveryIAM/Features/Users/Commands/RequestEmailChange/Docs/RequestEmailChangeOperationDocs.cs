// -----------------------------------------------------------------------
// <copyright file="RequestEmailChangeOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.RequestEmailChange.Docs;

/// <summary>
/// Datos de documentación de <c>RequestEmailChangeEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class RequestEmailChangeOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new RequestEmailChangeResponse(
    true,
    new RequestEmailChangeResponseDto(
      Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
      "nuevo-correo@ejemplo.com",
      EmailChangeRequestStatusCatalog.Pending,
      DateTime.UtcNow));

  protected override object RequestExample { get; } = new RequestEmailChangeRequestDto(NewEmail: "nuevo-correo@ejemplo.com");

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
    [ErrorInfo.Status409Conflict.StatusCode] = ErrorInfo.Status409Conflict,
    [ErrorInfo.Status422Business.StatusCode] = ErrorInfo.Status422Business,
  };
}
