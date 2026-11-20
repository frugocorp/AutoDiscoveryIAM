// -----------------------------------------------------------------------
// <copyright file="CreateUserEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.CreateUser;

public static class CreateUserEndPoint
{
  public static void MapCreateUserEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/users", async (
        CreateUserRequestDto dto,
        CreateUserHandler handler,
        CreateUserCommandValidator validator,
        CancellationToken cancellationToken) =>
    {
      var command = new CreateUserCommand(
        dto.Username,
        dto.Email,
        dto.Password);
      var validation = await validator.ValidateAsync(command, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        var response = new CreateUserResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
        return Results.BadRequest(response);
      }

      var result = await handler.Handle(command, cancellationToken);

      if (result.Success)
      {
        var response = new CreateUserResponse(true, result.Data);
        return Results.Ok(response);
      }
      else
      {
        var response = new CreateUserResponse(false, null, result.Error, result.ErrorInfo);
        return Results.Json(
                response,
                statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
      }
    })
    .WithName("CreateUser")
    .WithTags("Users")
    .WithSummary("Registra un nuevo usuario (cuenta local).")
    .WithDescription("Crea una cuenta local con contraseña y dispara el flujo de activación por correo. No requiere autenticación: es el endpoint público de registro.")
    .Accepts<CreateUserRequestDto>("application/json")
    .Produces<CreateUserResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status424Dependency.StatusCode, "application/json")
    .WithMetadata(
        new PermissionsNotRequiredAttribute("Endpoint de registro de usuarios: Permite la creación de usuarios anónimos. No requiere permisos específicos."),
        new CreateUserOperationDocs());
  }
}

