// -----------------------------------------------------------------------
// <copyright file="ClearBulkRoleHiddenRelationsOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations.Docs;

/// <summary>
/// Datos de documentación de <c>ClearBulkRoleHiddenRelationsEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class ClearBulkRoleHiddenRelationsOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Limpieza administrativa de relaciones ocultas ejecutada correctamente.";

  protected override object SuccessExample { get; } = new ApiResponse<ClearBulkRoleHiddenRelationsResponseDto>(
    true,
    new ClearBulkRoleHiddenRelationsResponseDto(
      RolePublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
      ClearedUserRelations: 2,
      ClearedPermissionRelations: 1,
      TotalCleared: 3,
      ExecutedAt: DateTime.Parse("2026-01-08T10:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)),
    null,
    null);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.SuperAdminAccessRequired.StatusCode] = ErrorInfo.SuperAdminAccessRequired,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
  };
}
