// -----------------------------------------------------------------------
// <copyright file="CreateRolePermissionsOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRolePermissions.Docs;

/// <summary>
/// Datos de documentación de <c>CreateRolePermissionsEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class CreateRolePermissionsOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Permiso asignado o reactivado correctamente para el rol.";

  protected override object SuccessExample { get; } = new ApiResponse<CreateRolePermissionsResponseDto>(
    true,
    new CreateRolePermissionsResponseDto(
      RolePublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440010"),
      PermissionPublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440020"),
      AssignedAt: DateTime.Parse("2026-01-05T10:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)),
    null,
    null);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status400Validation.StatusCode] = ErrorInfo.Status400Validation,
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.InsufficientPermissions.StatusCode] = ErrorInfo.InsufficientPermissions,
    [ErrorInfo.Status404NotFound.StatusCode] = ErrorInfo.Status404NotFound,
    [ErrorInfo.Status422Business.StatusCode] = ErrorInfo.Status422Business,
  };

  protected override IReadOnlyDictionary<string, ParameterDocInfo>? Parameters { get; } = new Dictionary<string, ParameterDocInfo>
  {
    ["rolePublicId"] = new("PublicId del rol al cual se asignará el permiso.", "3fa85f64-5717-4562-b3fc-2c963f66afa6"),
    ["permissionPublicId"] = new("PublicId del permiso a asignar al rol.", "550e8400-e29b-41d4-a716-446655440000"),
  };
}
