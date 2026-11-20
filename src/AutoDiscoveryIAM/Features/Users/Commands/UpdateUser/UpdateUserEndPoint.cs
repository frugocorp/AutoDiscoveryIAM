// -----------------------------------------------------------------------
// <copyright file="UpdateUserEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser.Docs;
using Microsoft.AspNetCore.Mvc;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateUser;

public static class UpdateUserEndPoint
{
  public static void MapUpdateUserEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPut("/users/{publicId:guid}", async (
        [AsParameters] UpdateUserRequestDto request,
        UpdateUserHandler handler,
        UpdateUserCommandValidator validator,
        CancellationToken cancellationToken) =>
    {
      var command = new UpdateUserCommand(request.PublicId, request.Body);
      var validation = await validator.ValidateAsync(command, cancellationToken);

      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        var response = new UpdateUserResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
        return Results.BadRequest(response);
      }

      var result = await handler.Handle(command, cancellationToken);

      if (result.Success)
      {
        var response = new UpdateUserResponse(true, result.Data);
        return Results.Ok(response);
      }
      else
      {
        var response = new UpdateUserResponse(false, null, result.Error, result.ErrorInfo);
        // Devuelve el status code correcto según el ErrorInfo.
        return Results.Json(
                response,
                statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
      }
    })
    .RequireAuthorization()
    .WithName("UpdateUser")
    .WithTags("Users")
    .WithSummary("Actualiza username, email y notas de un usuario.")
    .WithDescription("Actualiza los campos provistos (username, email, notas) de un usuario existente por PublicId. Los campos omitidos no se modifican. Falla con 409 si el nuevo username o email ya está en uso por otro usuario.")
    .Accepts<UpdateUserBodyDto>("application/json")
    .Produces<UpdateUserResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status403Forbidden.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
    .WithMetadata(new UpdateUserOperationDocs());
  }
}
