// -----------------------------------------------------------------------
// <copyright file="UpdateMyProfileEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.UpdateMyProfile;

/// <summary>
/// Autoservicio: el propio usuario autenticado actualiza su PhoneNumber. Para editar los datos de
/// otro usuario, ver <c>UpdateUserEndPoint</c> (administrativo, por PublicId).
/// </summary>
public static class UpdateMyProfileEndPoint
{
  public static void MapUpdateMyProfileEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPut("/users/me", async (
        UpdateMyProfileRequestDto dto,
        IAuthContextService authContext,
        UpdateMyProfileHandler handler,
        UpdateMyProfileCommandValidator validator,
        CancellationToken cancellationToken) =>
    {
      var userPublicId = authContext.GetCurrentUserPublicId();
      if (userPublicId is null)
      {
        return Results.Json(
            new UpdateMyProfileResponse(false, null, "No autenticado.", ErrorInfo.Status401Unauthorized),
            statusCode: ErrorInfo.Status401Unauthorized.StatusCode);
      }

      var command = new UpdateMyProfileCommand(userPublicId.Value, dto.PhoneNumber);
      var validation = await validator.ValidateAsync(command, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        return Results.BadRequest(new UpdateMyProfileResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
      }

      var result = await handler.Handle(command, cancellationToken);
      if (result.Success)
      {
        return Results.Ok(new UpdateMyProfileResponse(true, result.Data));
      }

      return Results.Json(
          new UpdateMyProfileResponse(false, null, result.Error, result.ErrorInfo),
          statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
    })
    .RequireAuthorization()
    .WithName("UpdateMyProfile")
    .WithTags("Users")
    .WithSummary("Actualiza el número de teléfono del usuario autenticado.")
    .WithDescription("Autoservicio: el propio usuario actualiza su PhoneNumber. Para editar los datos de otro usuario, un Admin usa PUT /users/{publicId}.")
    .WithMetadata(new PermissionsNotRequiredAttribute("Endpoint self-service: cualquier usuario autenticado puede actualizar sus propios datos"))
    .Accepts<UpdateMyProfileRequestDto>("application/json")
    .Produces<UpdateMyProfileResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
    .WithMetadata(new UpdateMyProfileOperationDocs());
  }
}
