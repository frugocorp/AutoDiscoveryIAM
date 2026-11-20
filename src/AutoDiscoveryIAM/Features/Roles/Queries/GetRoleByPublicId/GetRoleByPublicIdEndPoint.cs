// -----------------------------------------------------------------------
// <copyright file="GetRoleByPublicIdEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId.Docs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Queries.GetRoleByPublicId;

/// <summary>
/// Endpoint Minimal API para obtener un rol por PublicId.
/// Solo acepta PublicId, nunca el Id interno de la base de datos.
/// </summary>
public static class GetRoleByPublicIdEndPoint
{
    public static void MapGetRoleByPublicIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/roles/{publicId:guid}", async (
            [AsParameters] GetRoleByPublicIdRequestDto request,
            GetRoleByPublicIdHandler handler,
            GetRoleByPublicIdQueryValidator validator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetRoleByPublicIdQuery(request.PublicId);
            var validation = await validator.ValidateAsync(query, cancellationToken);
            if (!validation.IsValid)
            {
                var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                var response = new GetRoleByPublicIdResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
                return Results.BadRequest(response);
            }
            var result = await handler.Handle(query, cancellationToken);
            if (result.Success)
            {
                var response = new GetRoleByPublicIdResponse(true, result.Data);
                return Results.Ok(response);
            }
            else
            {
                var response = new GetRoleByPublicIdResponse(false, null, result.Error, result.ErrorInfo);
                return Results.Json(
                    response,
                    statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetRoleByPublicId")
        .WithTags("Roles")
        .WithSummary("Obtiene el detalle de un rol por su PublicId.")
        .WithDescription("Devuelve un rol según su PublicId (GUID), incluidos roles eliminados logicamente. Solo expone datos públicos, nunca el Id interno.")
        .RequireAuthorization()
        .Produces<GetRoleByPublicIdResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status403Forbidden.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .WithMetadata(new GetRoleByPublicIdOperationDocs());
    }
}
