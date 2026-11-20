// -----------------------------------------------------------------------
// <copyright file="LogoutEndpoint.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.ApiResponse;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout.Docs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Middleware.Authorization;
using Microsoft.EntityFrameworkCore;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;

/// <summary>
/// Endpoint para logout e invalidación de refresh tokens.
/// </summary>
public static class LogoutEndpoint
{
  public static void MapLogoutEndpoint(this IEndpointRouteBuilder app)
  {
    app.MapPost("/auth/logout", HandleLogoutAsync)
    .RequireAuthorization()
    .WithName("Logout")
    .WithTags("Auth")
    .WithSummary("Cierra la sesión e invalida refresh tokens.")
    .WithDescription("Revoca un refresh token específico, o todos los del usuario autenticado si RevokeAllTokens es true. Siempre responde 200, incluso si el token indicado ya estaba revocado o no existe.")
    .Produces<LogoutResponse>(StatusCodes.Status200OK, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status400Validation.StatusCode, "application/json")
    .Produces<ApiResponse<object>>(ErrorInfo.Status401Unauthorized.StatusCode, "application/json")
    .WithMetadata(
      new PermissionsNotRequiredAttribute("Logout requires authentication but no specific permissions"),
      new LogoutOperationDocs());
  }

  private static async Task<IResult> HandleLogoutAsync(
    LogoutRequestDto requestDto,
    LogoutHandler handler,
    LogoutCommandValidator validator,
    HttpContext context,
    CancellationToken cancellationToken)
  {
    // Extraer UserId del JWT si está presente
    long? userId = null;
    if (context.User?.Identity?.IsAuthenticated == true)
    {
      var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (Guid.TryParse(userIdClaim, out var publicId))
      {
        var dbContext = context.RequestServices.GetRequiredService<AutoDiscoveryIAMDbContext>();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.PublicId == publicId && !u.IsDeleted, cancellationToken);
        userId = user?.Id;
      }
    }

    // Convertir DTO a Command
    var command = requestDto.ToCommand(context, userId);

    // Validar comando
    var validation = await validator.ValidateAsync(command, cancellationToken);

    if (!validation.IsValid)
    {
      var errorMsg = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
      var response = new LogoutResponse(false, null, errorMsg, ErrorInfo.Status400Validation);
      return Results.BadRequest(response);
    }

    // Ejecutar comando
    var result = await handler.Handle(command, cancellationToken);

    if (result.Success)
    {
      var response = new LogoutResponse(true, result.Data);
      return Results.Ok(response);
    }
    else
    {
      var response = new LogoutResponse(false, null, result.Error, result.ErrorInfo);
      return Results.Json(response, statusCode: result.ErrorInfo?.Status ?? 500);
    }
  }
}
