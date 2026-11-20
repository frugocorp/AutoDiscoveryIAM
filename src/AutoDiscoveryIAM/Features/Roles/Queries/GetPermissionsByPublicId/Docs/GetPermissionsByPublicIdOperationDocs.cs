// -----------------------------------------------------------------------
// <copyright file="GetPermissionsByPublicIdOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetPermissionsByPublicId.Docs;

/// <summary>
/// Datos de documentación de <c>GetPermissionsByPublicIdEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class GetPermissionsByPublicIdOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override object SuccessExample { get; } = new ApiResponse<IEnumerable<GetPermissionsByPublicIdResponseDto>>(
    true,
    [
      new GetPermissionsByPublicIdResponseDto(
        PublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440020"),
        Key: "roles.assign_permissions",
        Description: "Permite asignar permisos a roles",
        Group: "Roles",
        AssignedAt: DateTime.Parse("2026-01-10T10:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)),
    ],
    null,
    null);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.InsufficientPermissions.StatusCode] = ErrorInfo.InsufficientPermissions,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
    [ErrorInfo.Status422Business.StatusCode] = ErrorInfo.Status422Business,
  };
}
