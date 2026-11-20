// -----------------------------------------------------------------------
// <copyright file="UpdateBulkUserRolesByRolePublicIdOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRolesByRolePublicId.Docs;

/// <summary>
/// Datos de documentación de <c>UpdateBulkUserRolesByRolePublicIdEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class UpdateBulkUserRolesByRolePublicIdOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Sincronización bulk por rol ejecutada correctamente (o simulada en dry-run).";

  protected override object SuccessExample { get; } = BuildSuccessExample();

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.InsufficientPermissions.StatusCode] = ErrorInfo.InsufficientPermissions,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
  };

  protected override object RequestExample { get; } = new UpdateBulkUserRolesByRolePublicIdRequestDto(
    UserPublicIds:
    [
      Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
      Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
    ],
    DryRun: false);

  protected override string RequestBodyDescription =>
    "Cuerpo con `UserPublicIds` destino y bandera `DryRun` para simulación sin persistencia.";

  protected override IReadOnlyDictionary<string, ParameterDocInfo>? Parameters { get; } = new Dictionary<string, ParameterDocInfo>
  {
    ["rolePublicId"] = new("PublicId del rol cuyo conjunto de usuarios será reconciliado.", "3fa85f64-5717-4562-b3fc-2c963f66afa6"),
  };

  private static ApiResponse<UpdateBulkUserRolesByRolePublicIdResponseDto> BuildSuccessExample()
  {
    var requested = new[]
    {
      Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
      Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
    };

    return new ApiResponse<UpdateBulkUserRolesByRolePublicIdResponseDto>(
      true,
      new UpdateBulkUserRolesByRolePublicIdResponseDto(
        RolePublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
        DryRun: false,
        TotalRequested: 2,
        TotalAssignedNew: 1,
        TotalAssignedReactivated: 0,
        TotalUnassigned: 1,
        TotalUnchanged: 1,
        ExecutedAt: DateTime.Parse("2026-01-04T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal),
        RequestedPublicIds: requested,
        AssignedNewPublicIds: [requested[1]],
        AssignedReactivatedPublicIds: [],
        UnassignedPublicIds: [Guid.Parse("550e8400-e29b-41d4-a716-446655440003")],
        UnchangedPublicIds: [requested[0]]),
      null,
      null);
  }
}
