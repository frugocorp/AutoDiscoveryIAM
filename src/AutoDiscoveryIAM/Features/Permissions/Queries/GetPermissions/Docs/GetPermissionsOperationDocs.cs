// -----------------------------------------------------------------------
// <copyright file="GetPermissionsOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetPermissions.Docs;

/// <summary>
/// Datos de documentación de <c>GetPermissionsEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class GetPermissionsOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Permisos recuperados correctamente (respuesta paginada).";

  protected override object SuccessExample { get; } = new ApiResponse<PagedResultDto<GetPermissionsResponseDto>>(
    true,
    new PagedResultDto<GetPermissionsResponseDto>(
      new List<GetPermissionsResponseDto>
      {
        new GetPermissionsResponseDto(
          PublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655441001"),
          Key: "Permissions.Get",
          Description: "Get all permissions",
          Group: "Permissions",
          IsActive: true,
          Route: "/permissions",
          HttpMethod: "GET",
          CreatedAt: DateTime.UtcNow,
          UpdatedAt: DateTime.UtcNow),
        new GetPermissionsResponseDto(
          PublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655441002"),
          Key: "Roles.Create",
          Description: "Create a new role",
          Group: "Roles",
          IsActive: true,
          Route: "/roles",
          HttpMethod: "POST",
          CreatedAt: DateTime.UtcNow,
          UpdatedAt: DateTime.UtcNow),
      },
      PageNumber: 1,
      PageSize: 20,
      TotalCount: 2),
    null,
    null);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.Status403Forbidden.StatusCode] = ErrorInfo.Status403Forbidden,
  };
}
