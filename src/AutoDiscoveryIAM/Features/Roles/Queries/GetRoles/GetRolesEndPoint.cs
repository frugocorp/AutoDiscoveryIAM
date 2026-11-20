// -----------------------------------------------------------------------
// <copyright file="GetRolesEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoles.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoles;

public static class GetRolesEndPoint
{
  public static void MapGetRolesEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapGet("/roles", async (
        GetRolesHandler handler,
        CancellationToken cancellationToken,
        int pageNumber = PaginationDefaults.DefaultPageNumber,
        int pageSize = PaginationDefaults.DefaultPageSize) =>
    {
      var query = new GetRolesQuery(pageNumber, pageSize);
      var result = await handler.Handle(query, cancellationToken);

      if (result.Success)
      {
        var response = new GetRolesResponse(true, result.Data);
        return Results.Ok(response);
      }
      else
      {
        var response = new GetRolesResponse(false, null, result.Error, result.ErrorInfo);
        return Results.Json(
                response,
                statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
      }
    })
    .WithName("GetRoles")
    .WithTags("Roles")
    .WithSummary("Lista los roles del sistema RBAC, paginado.")
    .WithDescription($"Devuelve los roles existentes (activos, inactivos y eliminados logicamente) en paginas de tamano configurable ({nameof(PaginationDefaults.DefaultPageSize)}={PaginationDefaults.DefaultPageSize}, maximo {PaginationDefaults.MaxPageSize}). Requiere el permiso RBAC del endpoint.")
    .RequireAuthorization()
    .Produces<GetRolesResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
    .WithMetadata(
        new RequirePermissionsAttribute("Consulta de roles - operación administrativa crítica"),
        new GetRolesOperationDocs());
  }
}
