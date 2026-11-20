// -----------------------------------------------------------------------
// <copyright file="GetRolesOperationDocs.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.OpenApi;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoles.Docs;

/// <summary>
/// Datos de documentación de <c>GetRolesEndPoint</c> para <see cref="EndpointOperationDocs"/>.
/// </summary>
public sealed class GetRolesOperationDocs : EndpointOperationDocs
{
  protected override int SuccessStatusCode => StatusCodes.Status200OK;

  protected override string SuccessDescription => "Roles recuperados correctamente (respuesta paginada).";

  protected override object SuccessExample { get; } = new ApiResponse<PagedResultDto<GetRolesResponseDto>>(
    true,
    new PagedResultDto<GetRolesResponseDto>(
      [
        new GetRolesResponseDto(
          PublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
          Name: "AdminAutoDiscoveryIAM",
          Description: "Rol con permisos completos del package",
          IsActive: true,
          CreatedAt: DateTime.Parse("2025-01-01T10:00:00Z", CultureInfo.InvariantCulture),
          UpdatedAt: null,
          DeletedAt: null,
          IsDeleted: false),
        new GetRolesResponseDto(
          PublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
          Name: "Moderador",
          Description: "Rol con permisos limitados de administración",
          IsActive: true,
          CreatedAt: DateTime.Parse("2025-01-02T14:30:00Z", CultureInfo.InvariantCulture),
          UpdatedAt: DateTime.Parse("2025-01-03T09:15:00Z", CultureInfo.InvariantCulture),
          DeletedAt: null,
          IsDeleted: false),
        new GetRolesResponseDto(
          PublicId: Guid.Parse("550e8400-e29b-41d4-a716-446655440003"),
          Name: "Usuario",
          Description: "Rol básico para usuarios del sistema",
          IsActive: true,
          CreatedAt: DateTime.Parse("2025-01-01T10:00:00Z", CultureInfo.InvariantCulture),
          UpdatedAt: null,
          DeletedAt: null,
          IsDeleted: false),
      ],
      PageNumber: 1,
      PageSize: 20,
      TotalCount: 3),
    null,
    null);

  protected override IReadOnlyDictionary<int, ErrorInfo> ErrorResponses { get; } = new Dictionary<int, ErrorInfo>
  {
    [ErrorInfo.Status401Unauthorized.StatusCode] = ErrorInfo.Status401Unauthorized,
    [ErrorInfo.InsufficientPermissions.StatusCode] = ErrorInfo.InsufficientPermissions,
  };
}
