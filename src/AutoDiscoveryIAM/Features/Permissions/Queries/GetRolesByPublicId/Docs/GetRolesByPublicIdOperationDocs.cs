// -----------------------------------------------------------------------
// <copyright file="GetRolesByPublicIdOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetRolesByPublicId.Docs;

/// <summary>
/// Datos de documentación de <c>GetRolesByPublicIdEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class GetRolesByPublicIdOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new GetRolesByPublicIdResponse(
    true,
    [
      new GetRolesByPublicIdResponseDto(
        PublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
        Name: "Admin",
        Description: "Rol administrativo",
        AssignedAt: DateTime.Parse("2026-01-12T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal)),
    ]);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.InsufficientPermissions.StatusCode] = ErrorInfo.InsufficientPermissions,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
    [ErrorInfo.Status422Business.StatusCode] = ErrorInfo.Status422Business,
  };
}
