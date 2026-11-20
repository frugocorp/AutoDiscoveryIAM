using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Pagination;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetUsersByPublicId;

public static class GetUsersByPublicIdEndPoint
{
    public static void MapGetUsersByPublicIdEndPoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/roles/{rolePublicId:guid}/users", async (
          Guid rolePublicId,
          GetUsersByPublicIdHandler handler,
          CancellationToken cancellationToken,
          int pageNumber = PaginationDefaults.DefaultPageNumber,
          int pageSize = PaginationDefaults.DefaultPageSize) =>
        {
            var result = await handler.Handle(new GetUsersByPublicIdQuery(rolePublicId, pageNumber, pageSize), cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new GetUsersByPublicIdResponse(true, result.Data));
            }

            return Results.Json(new GetUsersByPublicIdResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .WithName("GetRoleUsersByPublicId")
        .WithTags("Roles")
        .WithSummary("Obtiene los usuarios asignados a un rol.")
        .WithDescription("Lista los usuarios activos asignados a un rol por PublicId. Requiere el permiso RBAC del endpoint.")
        .RequireAuthorization()
        .Produces<GetUsersByPublicIdResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
        .WithMetadata(
          new RequirePermissionsAttribute("Consulta administrativa de usuarios por rol"),
          new GetUsersByPublicIdOperationDocs());
    }
}
