// -----------------------------------------------------------------------
// <copyright file="CreateRoleEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole.Docs;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.CreateRole;

public static class CreateRoleEndPoint
{
  public static void MapCreateRoleEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/roles", async (
        CreateRoleRequestDto dto,
        CreateRoleHandler handler,
        CreateRoleCommandValidator validator,
        CancellationToken cancellationToken) =>
    {
      var command = new CreateRoleCommand(dto.Name, dto.Description, dto.IsActive);
      var validation = await validator.ValidateAsync(command, cancellationToken);
      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        var response = new CreateRoleResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
        return Results.BadRequest(response);
      }

      var result = await handler.Handle(command, cancellationToken);

      if (result.Success && result.Data != null)
      {
        var response = new CreateRoleResponse(true, result.Data);
        return Results.Created($"/roles/{result.Data.PublicId}", response);
      }
      else
      {
        var response = new CreateRoleResponse(false, null, result.Error, result.ErrorInfo);
        return Results.Json(
                response,
                statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
      }
    })
    .RequireAuthorization()
    .WithName("CreateRole")
    .WithTags("Roles")
    .WithSummary("Crea un nuevo rol.")
    .WithDescription("Crea un rol RBAC con nombre, descripción y estado activo/inactivo. El nombre debe ser único.")
    .Accepts<CreateRoleRequestDto>("application/json")
    .Produces<CreateRoleResponse>(StatusCodes.Status201Created, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status403Forbidden.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status422Business.StatusCode, "application/json")
    .WithMetadata(new CreateRoleOperationDocs());
  }
}
