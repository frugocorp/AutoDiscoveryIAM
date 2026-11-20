// -----------------------------------------------------------------------
// <copyright file="DeleteRoleByPublicIdEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId.Docs;
using Microsoft.AspNetCore.Mvc;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.DeleteRoleByPublicId;

public static class DeleteRoleByPublicIdEndPoint
{
    public static void MapDeleteRoleByPublicIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/roles/{publicId:guid}", async (
            [AsParameters] DeleteRoleByPublicIdRequestDto request,
            DeleteRoleByPublicIdHandler handler,
            DeleteRoleByPublicIdCommandValidator validator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteRoleByPublicIdCommand(request.PublicId);
            var validation = await validator.ValidateAsync(command, cancellationToken);

            if (!validation.IsValid)
            {
                var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                var response = new DeleteRoleByPublicIdResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
                return Results.BadRequest(response);
            }

            var result = await handler.Handle(command, cancellationToken);

            if (result.Success)
            {
                var response = new DeleteRoleByPublicIdResponse(true, result.Data);
                return Results.Ok(response);
            }
            else
            {
                var response = new DeleteRoleByPublicIdResponse(false, null, result.Error, result.ErrorInfo);
                // Devuelve el status code correcto según el ErrorInfo.
                return Results.Json(
              response,
              statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
            }
        })
        .RequireAuthorization()
        .WithName("DeleteRoleByPublicId")
        .WithTags("Roles")
        .WithSummary("Elimina (borrado lógico) un rol.")
        .WithDescription("Marca un rol como eliminado. Falla con 409 si el rol todavía tiene usuarios asignados.")
        .Produces<DeleteRoleByPublicIdResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status403Forbidden.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
        .WithMetadata(new DeleteRoleByPublicIdOperationDocs());
    }
}