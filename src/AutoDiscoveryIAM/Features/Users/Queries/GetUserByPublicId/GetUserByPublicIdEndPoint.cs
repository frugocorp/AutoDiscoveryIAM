// -----------------------------------------------------------------------
// <copyright file="GetUserByPublicIdEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId.Docs;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Queries.GetUserByPublicId;

/// <summary>
/// Endpoint administrativo para consultar un usuario por PublicId.
/// </summary>
public static class GetUserByPublicIdEndPoint
{
  public static void MapGetUserByPublicIdEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapGet("/users/{publicId:guid}", async (
        Guid publicId,
        GetUserByPublicIdHandler handler,
        GetUserByPublicIdQueryValidator validator,
        CancellationToken cancellationToken) =>
    {
      var query = new GetUserByPublicIdQuery(publicId);
      var validation = await validator.ValidateAsync(query, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        var response = new GetUserByPublicIdResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
        return Results.BadRequest(response);
      }

      var result = await handler.Handle(query, cancellationToken);

      if (result.Success)
      {
        var response = new GetUserByPublicIdResponse(true, result.Data);
        return Results.Ok(response);
      }

      return Results.Json(
          new GetUserByPublicIdResponse(false, null, result.Error, result.ErrorInfo),
          statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .WithName("GetUserByPublicId")
    .WithTags("Users")
    .WithSummary("Obtiene el detalle de un usuario por su PublicId.")
    .WithDescription("Devuelve un usuario activo según su PublicId (GUID), para pantallas administrativas. Solo expone datos públicos, nunca el Id interno.")
    .RequireAuthorization()
    .Produces<GetUserByPublicIdResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status403Forbidden.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .WithMetadata(new GetUserByPublicIdOperationDocs());
  }
}
