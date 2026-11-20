// -----------------------------------------------------------------------
// <copyright file="UpdateBulkRolePermissionsOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.UpdateBulkRolePermissions.Docs;

/// <summary>
/// Datos de documentación de <c>UpdateBulkRolePermissionsEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class UpdateBulkRolePermissionsOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Sincronización bulk de permisos ejecutada correctamente (o simulada en dry-run).";

  protected override object SuccessExample { get; } = new ApiResponse<UpdateBulkRolePermissionsResponseDto>(
    true,
    new UpdateBulkRolePermissionsResponseDto(
      RolePublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
      DryRun: false,
      TotalRequested: 2,
      TotalAssignedNew: 1,
      TotalAssignedReactivated: 0,
      TotalUnassigned: 1,
      TotalUnchanged: 1,
      ExecutedAt: DateTime.Parse("2026-01-07T10:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal),
      RequestedPublicIds:
      [
        Guid.Parse("550e8400-e29b-41d4-a716-446655440020"),
        Guid.Parse("550e8400-e29b-41d4-a716-446655440021"),
      ],
      AssignedNewPublicIds: [Guid.Parse("550e8400-e29b-41d4-a716-446655440021")],
      AssignedReactivatedPublicIds: [],
      UnassignedPublicIds: [Guid.Parse("550e8400-e29b-41d4-a716-446655440022")],
      UnchangedPublicIds: [Guid.Parse("550e8400-e29b-41d4-a716-446655440020")]),
    null,
    null);

  protected override object RequestExample { get; } = new UpdateBulkRolePermissionsRequestDto(
    PermissionPublicIds:
    [
      Guid.Parse("550e8400-e29b-41d4-a716-446655440020"),
      Guid.Parse("550e8400-e29b-41d4-a716-446655440021"),
    ],
    DryRun: false);

  protected override string? RequestBodyDescription =>
    "Cuerpo con `PermissionPublicIds` destino y bandera `DryRun` para simulación sin persistencia.";

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.InsufficientPermissions.StatusCode] = ErrorInfo.InsufficientPermissions,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
  };
}
