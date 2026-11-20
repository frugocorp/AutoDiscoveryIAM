// -----------------------------------------------------------------------
// <copyright file="GetPermissionsEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetPermissions.Docs;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Queries.GetPermissions;

public static class GetPermissionsEndPoint
{
  public static void MapGetPermissionsEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapGet("/permissions", async (
        GetPermissionsHandler handler,
        CancellationToken cancellationToken,
        int pageNumber = PaginationDefaults.DefaultPageNumber,
        int pageSize = PaginationDefaults.DefaultPageSize) =>
    {
      var query = new GetPermissionsQuery(pageNumber, pageSize);
      var result = await handler.Handle(query, cancellationToken);

      if (result.Success)
      {
        var response = new GetPermissionsResponse(true, result.Data);
        return Results.Ok(response);
      }
      else
      {
        var response = new GetPermissionsResponse(false, null, result.Error, result.ErrorInfo);
        return Results.Json(
                response,
                statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
      }
    })
    .WithName("GetPermissions")
    .WithTags("Permissions")
    .WithSummary("Lista los permisos del sistema RBAC, paginado.")
    .WithDescription($"Devuelve los permisos existentes, incluidos los marcados como obsoletos (IsActive=false) tras una sincronización, en paginas de tamano configurable ({nameof(PaginationDefaults.DefaultPageSize)}={PaginationDefaults.DefaultPageSize}, maximo {PaginationDefaults.MaxPageSize}).")
    .RequireAuthorization()
    .Produces<GetPermissionsResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status403Forbidden.StatusCode, "application/json")
    .WithMetadata(new GetPermissionsOperationDocs());
  }
}
