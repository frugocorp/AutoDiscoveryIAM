using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Roles.Commands.ClearBulkRoleHiddenRelations;

public static class ClearBulkRoleHiddenRelationsEndPoint
{
    public static void MapClearBulkRoleHiddenRelationsEndPoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/roles/{rolePublicId:guid}/hidden-relations", async (
          Guid rolePublicId,
          ClearBulkRoleHiddenRelationsHandler handler,
                    ClearBulkRoleHiddenRelationsCommandValidator validator,
          CancellationToken cancellationToken) =>
        {
            var command = new ClearBulkRoleHiddenRelationsCommand(rolePublicId);
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                var errorMsg = string.Join("; ", validation.Errors.Select(error => error.ErrorMessage));
                return Results.BadRequest(new ClearBulkRoleHiddenRelationsResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
            }

            var result = await handler.Handle(command, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new ClearBulkRoleHiddenRelationsResponse(true, result.Data));
            }

            return Results.Json(new ClearBulkRoleHiddenRelationsResponse(false, null, result.Error, result.ErrorInfo), statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .WithName("ClearBulkRoleHiddenRelations")
        .WithTags("Roles")
        .WithSummary("Limpia relaciones ocultas (usuario/permiso) de un rol.")
        .WithDescription("Marca como eliminadas logicamente todas las relaciones usuario-rol y rol-permiso de un rol que quedaron ocultas (vinculadas a un usuario o permiso ya inactivo) sin haberse marcado como eliminadas. Operacion de mantenimiento, separada del bulk normal de sincronizacion.")
        .RequireAuthorization()
        .Produces<ClearBulkRoleHiddenRelationsResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.SuperAdminAccessRequired.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .WithMetadata(
          new RequiresSuAdminAccessAttribute("Limpieza administrativa de relaciones ocultas de rol"),
          new ClearBulkRoleHiddenRelationsOperationDocs());
    }
}
