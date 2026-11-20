using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;

public static class PasswordManagementEndPoint
{
    public static void MapPasswordManagementEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users/me/password", async (
          ChangePasswordRequestDto dto,
          ChangePasswordRequestValidator validator,
          IAuthContextService authContext,
          IPasswordManagementService service,
          CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(dto, cancellationToken);
            if (!validation.IsValid)
            {
                var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                return Results.BadRequest(new PasswordManagementResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
            }

            var userPublicId = authContext.GetCurrentUserPublicId();
            if (userPublicId == null)
            {
                return Results.Json(
                  new PasswordManagementResponse(false, null, "Usuario no autenticado.", ErrorInfo.Status401Unauthorized),
                  statusCode: ErrorInfo.Status401Unauthorized.StatusCode);
            }

            var result = await service.ChangePasswordAsync(userPublicId.Value, dto.CurrentPassword, dto.NewPassword, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new PasswordManagementResponse(true, result.Data));
            }

            return Results.Json(
              new PasswordManagementResponse(false, null, result.Error, result.ErrorInfo),
              statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .RequireAuthorization()
        .WithName("ChangeMyPassword")
        .WithTags("Users")
        .WithSummary("Cambia o establece la contrasena del usuario autenticado.")
        .WithDescription("Valida la contrasena actual cuando el usuario ya tiene una utilizable. Invalida los refresh tokens activos tras el cambio.")
        .WithMetadata(new PermissionsNotRequiredAttribute("Endpoint self-service: requiere autenticacion, pero no permiso RBAC especifico para cambio propio de contraseña"))
        .Produces<PasswordManagementResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .WithMetadata(new ChangeMyPasswordOperationDocs());

        app.MapPost("/users/password/reset/request", async (
          RequestPasswordResetRequestDto dto,
          RequestPasswordResetRequestValidator validator,
          IPasswordManagementService service,
          CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(dto, cancellationToken);
            if (!validation.IsValid)
            {
                var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                return Results.BadRequest(new PasswordManagementResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
            }

            var result = await service.RequestResetAsync(dto.Email, cancellationToken);
            return Results.Ok(new PasswordManagementResponse(true, result.Data));
        })
        .WithName("RequestPasswordReset")
        .WithTags("Users")
        .WithSummary("Solicita el reseteo de contrasena por correo con respuesta neutra.")
        .WithDescription("Siempre responde el mismo mensaje neutro, exista o no el correo, para evitar enumeracion de cuentas.")
        .WithMetadata(new PermissionsNotRequiredAttribute("Solicitud publica de reseteo de contraseña"))
        .Produces<PasswordManagementResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .WithMetadata(new RequestPasswordResetOperationDocs());

        app.MapPost("/users/password/reset/confirm", async (
          ConfirmPasswordResetRequestDto dto,
          ConfirmPasswordResetRequestValidator validator,
          IPasswordManagementService service,
          CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(dto, cancellationToken);
            if (!validation.IsValid)
            {
                var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                return Results.BadRequest(new PasswordManagementResponse(false, null, errorMsg, ErrorInfo.Status400Validation));
            }

            var result = await service.ConfirmResetAsync(dto.Token, dto.NewPassword, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new PasswordManagementResponse(true, result.Data));
            }

            return Results.Json(
              new PasswordManagementResponse(false, null, result.Error, result.ErrorInfo),
              statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .WithName("ConfirmPasswordReset")
        .WithTags("Users")
        .WithSummary("Confirma el reseteo de contrasena con token de un solo uso.")
        .WithDescription("Valida el token contra su expiracion, actualiza la contrasena e invalida los refresh tokens activos.")
        .WithMetadata(new PermissionsNotRequiredAttribute("Confirmacion publica de reseteo de contraseña"))
        .Produces<PasswordManagementResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
        .WithMetadata(new ConfirmPasswordResetOperationDocs());
    }
}
