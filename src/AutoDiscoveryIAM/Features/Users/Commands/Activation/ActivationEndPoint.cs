using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;

public static class ActivationEndPoint
{
    public static void MapActivationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapConfirmGetEndpoint();
        app.MapConfirmPostEndpoint();
        app.MapResendEndpoint();
        app.MapApproveEndpoint();
        app.MapRejectEndpoint();
        app.MapResendAdminEndpoint();
        app.MapTestEmailEndpoint();
    }

    private static void MapConfirmGetEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/users/activation/confirm", async (
          [FromQuery] string token,
          IActivationService service,
          CancellationToken cancellationToken) =>
        {
            var result = await service.ConfirmByTokenAsync(token, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new ActivationResponse(true, result.Data));
            }

            return Results.Json(
          new ActivationResponse(false, null, result.Error, result.ErrorInfo),
          statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .WithName("UsersActivationConfirm")
        .WithTags("Users")
        .WithSummary("Confirma la activación de cuenta (link de correo).")
        .WithDescription("Activa la cuenta usando el token recibido por correo. Pensado para el link que el usuario abre desde el correo de activación.")
        .Produces<ActivationResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
        .WithMetadata(
          new PermissionsNotRequiredAttribute("Confirmacion de activacion publica"),
          new UsersActivationConfirmOperationDocs());
    }

    private static void MapConfirmPostEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users/activation/confirm", async (
          ActivationConfirmRequestDto dto,
          IActivationService service,
          CancellationToken cancellationToken) =>
        {
            var result = await service.ConfirmByTokenAsync(dto.Token, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new ActivationResponse(true, result.Data));
            }

            return Results.Json(
          new ActivationResponse(false, null, result.Error, result.ErrorInfo),
          statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .WithName("UsersActivationConfirmPost")
        .WithTags("Users")
        .WithSummary("Confirma la activación de cuenta (API/cliente propio).")
        .WithDescription("Igual que la variante GET, pero recibiendo el token en el cuerpo de la solicitud. Pensado para clientes propios (app/frontend), no para el link de correo.")
        .Produces<ActivationResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
        .WithMetadata(
          new PermissionsNotRequiredAttribute("Confirmacion de activacion publica"),
          new UsersActivationConfirmPostOperationDocs());
    }

    private static void MapResendEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users/activation/resend", async (
          ActivationResendRequestDto dto,
          IActivationService service,
          CancellationToken cancellationToken) =>
        {
            var result = await service.ResendPublicAsync(dto.Email, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new ActivationResponse(true, result.Data));
            }

            return Results.Json(
          new ActivationResponse(false, null, result.Error, result.ErrorInfo),
          statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .WithName("UsersActivationResend")
        .WithTags("Users")
        .WithSummary("Solicita el reenvío del correo de activación.")
        .WithDescription("Siempre responde 200 con un mensaje neutro, exista o no el correo, esté o no ya activo, para evitar enumeración de cuentas. Sujeta a cooldown y límite por hora, aplicados en silencio.")
        .Produces<ActivationResponse>(StatusCodes.Status200OK, "application/json")
        .WithMetadata(
          new PermissionsNotRequiredAttribute("Reenvio publico de activacion"),
          new UsersActivationResendOperationDocs());
    }

    private static void MapApproveEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users/{publicId:guid}/approve", async (
          Guid publicId,
          IActivationService service,
          CancellationToken cancellationToken) =>
        {
            var result = await service.ApproveByPublicIdAsync(publicId, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new ActivationAdminResponse(true, result.Data));
            }

            return Results.Json(
          new ActivationAdminResponse(false, null, result.Error, result.ErrorInfo),
          statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .RequireAuthorization()
        .WithName("UsersApprove")
        .WithTags("Users")
        .WithSummary("Aprueba y activa un usuario pendiente (administrativo).")
        .WithDescription("Activa la cuenta directamente, sin token de correo. Requiere el permiso RBAC del endpoint; una cuenta con flags solo la administra quien tiene un nivel igual o superior.")
        .Produces<ActivationAdminResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .WithMetadata(
          new RequirePermissionsAttribute("Aprobacion administrativa de usuarios"),
          new UsersApproveOperationDocs());
    }

    private static void MapRejectEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users/{publicId:guid}/reject", async (
          Guid publicId,
          IActivationService service,
          CancellationToken cancellationToken) =>
        {
            var result = await service.RejectByPublicIdAsync(publicId, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new ActivationAdminResponse(true, result.Data));
            }

            return Results.Json(
          new ActivationAdminResponse(false, null, result.Error, result.ErrorInfo),
          statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .RequireAuthorization()
        .WithName("UsersReject")
        .WithTags("Users")
        .WithSummary("Rechaza un usuario pendiente (administrativo).")
        .WithDescription("Marca la cuenta como inactiva sin activarla. Requiere el permiso RBAC del endpoint; una cuenta con flags solo la administra quien tiene un nivel igual o superior.")
        .Produces<ActivationAdminResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .WithMetadata(
          new RequirePermissionsAttribute("Rechazo administrativo de usuarios"),
          new UsersRejectOperationDocs());
    }

    private static void MapResendAdminEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users/{publicId:guid}/activation/resend", async (
          Guid publicId,
          IActivationService service,
          CancellationToken cancellationToken) =>
        {
            var result = await service.ResendAdminByPublicIdAsync(publicId, cancellationToken);
            if (result.Success)
            {
                return Results.Ok(new ActivationAdminResponse(true, result.Data));
            }

            return Results.Json(
          new ActivationAdminResponse(false, null, result.Error, result.ErrorInfo),
          statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .RequireAuthorization()
        .WithName("UsersActivationResendAdmin")
        .WithTags("Users")
        .WithSummary("Reenvía el correo de activación (administrativo).")
        .WithDescription("A diferencia del reenvío público, informa si el usuario no existe, ya está activo, o si el envío de correo falló. Requiere el permiso RBAC del endpoint.")
        .Produces<ActivationAdminResponse>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.InsufficientPermissions.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status404NotFound.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status409Conflict.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status424Dependency.StatusCode, "application/json")
        .WithMetadata(
          new RequirePermissionsAttribute("Reenvio administrativo de activacion"),
          new UsersActivationResendAdminOperationDocs());
    }

    private static void MapTestEmailEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users/activation/test-email", async (
          ActivationTestEmailRequestDto dto,
          IActivationService service,
          CancellationToken cancellationToken) =>
        {
            var result = await service.SendTestEmailAsync(
              dto.Subject,
              dto.Body,
              cancellationToken,
              fakeMail: dto.FakeMail);
            if (result.Success)
            {
                return Results.Ok(new ApiResponse<ActivationTestEmailResponseDto>(
              true,
              new ActivationTestEmailResponseDto("Correo de prueba enviado.")));
            }

            return Results.Json(
          new ApiResponse<ActivationTestEmailResponseDto>(false, null, result.Error, result.ErrorInfo),
          statusCode: result.ErrorInfo?.StatusCode ?? StatusCodes.Status500InternalServerError);
        })
        .RequireAuthorization()
        .WithName("UsersActivationTestEmail")
        .WithTags("Users")
        .WithSummary("Envía un correo de prueba para diagnosticar el SMTP.")
        .WithDescription("Herramienta de diagnóstico: envía (o simula, con fakeMail=true) un correo de prueba al remitente configurado. Requiere acceso SuAdmin/Development.")
        .Produces<ApiResponse<ActivationTestEmailResponseDto>>(StatusCodes.Status200OK, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.SuperAdminAccessRequired.StatusCode, "application/json")
        .Produces<ApiResponse<object>>(ErrorInfo.Status424Dependency.StatusCode, "application/json")
        .WithMetadata(
          new RequiresSuAdminAccessAttribute("Prueba operativa de SMTP"),
          new UsersActivationTestEmailOperationDocs());
    }
}
