using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetMyPermissions;

public static class GetMyPermissionsEndPoint
{
  public static void MapGetMyPermissionsEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapGet("/users/me/permissions", HandleGetMyPermissionsAsync)
    .WithName("GetMyPermissions")
    .WithTags("Users")
    .WithSummary("Obtiene los permisos efectivos del usuario autenticado.")
    .WithDescription("Resuelve los permisos efectivos (via roles asignados) del usuario actual, usando el PublicId obtenido de los claims del token.")
    .RequireAuthorization()
    .WithMetadata(new PermissionsNotRequiredAttribute("Endpoint self-service: requiere autenticacion, pero no permiso RBAC especifico para consulta propia"))
    .Produces<GetMyPermissionsResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
    .WithMetadata(new GetMyPermissionsOperationDocs());
  }

  public static async Task<IResult> HandleGetMyPermissionsAsync(
    GetMyPermissionsHandler handler,
    CancellationToken cancellationToken)
  {
    var result = await handler.Handle(new GetMyPermissionsQuery(), cancellationToken);
    if (result.Success)
    {
      return Results.Ok(new GetMyPermissionsResponse(true, result.Data));
    }

    return Results.Json(
      new GetMyPermissionsResponse(false, null, result.Error, result.ErrorInfo),
      statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
  }
}
