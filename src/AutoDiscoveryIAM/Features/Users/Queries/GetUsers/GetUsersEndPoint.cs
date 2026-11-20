// -----------------------------------------------------------------------
// <copyright file="GetUsersEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUsers;

public static class GetUsersEndPoint
{
  public static void MapGetUsersEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapGet("/users", async (
        GetUsersHandler handler,
        CancellationToken cancellationToken,
        int pageNumber = PaginationDefaults.DefaultPageNumber,
        int pageSize = PaginationDefaults.DefaultPageSize) =>
    {
      var query = new GetUsersQuery(pageNumber, pageSize);
      var result = await handler.Handle(query, cancellationToken);

      if (result.Success)
      {
        var response = new GetUsersResponse(true, result.Data);
        return Results.Ok(response);
      }

      return Results.Json(
          new GetUsersResponse(false, null, result.Error, result.ErrorInfo),
          statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .WithName("GetUsers")
    .WithTags("Users")
    .WithSummary("Lista el catálogo de usuarios, paginado.")
    .WithDescription($"Devuelve los usuarios del sistema en paginas de tamano configurable ({nameof(PaginationDefaults.DefaultPageSize)}={PaginationDefaults.DefaultPageSize}, maximo {PaginationDefaults.MaxPageSize}) para pantallas administrativas y asignaciones RBAC. Requiere el permiso RBAC del endpoint.")
    .RequireAuthorization()
    .Produces<GetUsersResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
    .WithMetadata(
        new RequirePermissionsAttribute("Catálogo administrativo de usuarios para asignaciones RBAC"),
        new GetUsersOperationDocs());
  }
}
