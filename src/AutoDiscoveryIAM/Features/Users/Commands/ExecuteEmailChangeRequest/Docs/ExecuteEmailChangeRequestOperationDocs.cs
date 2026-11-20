// -----------------------------------------------------------------------
// <copyright file="ExecuteEmailChangeRequestOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.ExecuteEmailChangeRequest.Docs;

/// <summary>
/// Datos de documentación de <c>ExecuteEmailChangeRequestEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class ExecuteEmailChangeRequestOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new ExecuteEmailChangeRequestResponse(
    true,
    new ExecuteEmailChangeRequestResponseDto(
      Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
      Guid.Parse("550e8400-e29b-41d4-a716-446655440020"),
      "nuevo-correo@ejemplo.com",
      EmailChangeRequestStatusCatalog.Executed,
      DateTime.UtcNow));

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.SuperAdminAccessRequired.StatusCode] = ErrorInfo.SuperAdminAccessRequired,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
    [ErrorInfo.Status409Conflict.StatusCode] = ErrorInfo.Status409Conflict,
    [ErrorInfo.Status424Dependency.StatusCode] = ErrorInfo.Status424Dependency,
  };
}
