// -----------------------------------------------------------------------
// <copyright file="SyncPermissionsEndPoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using FluentValidation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Permissions.Commands.SyncPermissions;

public static class SyncPermissionsEndPoint
{
  public static void MapSyncPermissionsEndPoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/permissions/sync", async (
        SyncPermissionsRequestDto dto,
        SyncPermissionsHandler handler,
        SyncPermissionsCommandValidator validator,
        CancellationToken cancellationToken) =>
    {
      var command = new SyncPermissionsCommand(dto);
      var validation = await validator.ValidateAsync(command, cancellationToken);

      if (!validation.IsValid)
      {
        var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        var response = new SyncPermissionsCommandResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
        return Results.BadRequest(response);
      }

      var result = await handler.Handle(command, cancellationToken);

      if (result.Success)
      {
        var response = new SyncPermissionsCommandResponse(true, result.Data);
        return Results.Ok(response);
      }
      else
      {
        var response = new SyncPermissionsCommandResponse(false, null, result.Error, result.ErrorInfo);
        // Devuelve el status code correcto según el ErrorInfo.
        return Results.Json(
                response,
                statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
      }
    })
    .RequireAuthorization()
    .WithName("SyncPermissions")
    .WithTags("Permissions")
    .WithSummary("Sincroniza los permisos con los endpoints reales de la aplicación.")
    .WithDescription("Escanea todos los Map*Endpoint del proyecto y crea o actualiza un Permission por endpoint (formato {feature}.{accion}). Nunca elimina permisos existentes: los que ya no tienen endpoint asociado quedan marcados IsActive=false, para no romper la integridad referencial de RolePermission. Con DryRun=true calcula el resultado sin persistir. Requiere acceso Development.")
    .Accepts<SyncPermissionsRequestDto>("application/json")
    .Produces<SyncPermissionsCommandResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.DevelopmentAccessRequired.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status500Unknown.StatusCode, "application/json")
    .WithMetadata(
        new RequiresDevelopmentAccessAttribute("Este Endpoint es crítico y solo accesible para administradores del sistema, debido a que sincroniza permisos RBAC y los persiste en la base de datos para después ser utilizados por el sistema."),
        new SyncPermissionsOperationDocs());
  }
}
