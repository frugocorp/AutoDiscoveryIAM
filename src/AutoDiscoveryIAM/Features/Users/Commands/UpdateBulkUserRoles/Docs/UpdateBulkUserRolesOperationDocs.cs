// -----------------------------------------------------------------------
// <copyright file="UpdateBulkUserRolesOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateBulkUserRoles.Docs;

/// <summary>
/// Datos de documentación de <c>UpdateBulkUserRolesEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class UpdateBulkUserRolesOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Sincronización bulk ejecutada correctamente (o simulada en dry-run).";

  protected override object SuccessExample { get; } = BuildSuccessExample();

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.InsufficientPermissions.StatusCode] = ErrorInfo.InsufficientPermissions,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
  };

  protected override object RequestExample { get; } = new UpdateBulkUserRolesRequestDto(
    RolePublicIds:
    [
      Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
      Guid.Parse("550e8400-e29b-41d4-a716-446655440011"),
    ],
    DryRun: false);

  protected override string RequestBodyDescription =>
    "Cuerpo con `RolePublicIds` destino y bandera `DryRun` para simulación sin persistencia.";

  protected override IReadOnlyDictionary<string, ParameterDocInfo>? Parameters { get; } = new Dictionary<string, ParameterDocInfo>
  {
    ["userPublicId"] = new("PublicId del usuario cuyo conjunto de roles será reconciliado.", "550e8400-e29b-41d4-a716-446655440000"),
  };

  private static ApiResponse<UpdateBulkUserRolesResponseDto> BuildSuccessExample()
  {
    var requested = new[]
    {
      Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
      Guid.Parse("550e8400-e29b-41d4-a716-446655440011"),
    };

    return new ApiResponse<UpdateBulkUserRolesResponseDto>(
      true,
      new UpdateBulkUserRolesResponseDto(
        UserPublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
        DryRun: false,
        TotalRequested: 2,
        TotalAssignedNew: 1,
        TotalAssignedReactivated: 0,
        TotalUnassigned: 1,
        TotalUnchanged: 1,
        ExecutedAt: DateTime.Parse("2026-01-03T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal),
        RequestedPublicIds: requested,
        AssignedNewPublicIds: [requested[1]],
        AssignedReactivatedPublicIds: [],
        UnassignedPublicIds: [Guid.Parse("550e8400-e29b-41d4-a716-446655440012")],
        UnchangedPublicIds: [requested[0]]),
      null,
      null);
  }
}
