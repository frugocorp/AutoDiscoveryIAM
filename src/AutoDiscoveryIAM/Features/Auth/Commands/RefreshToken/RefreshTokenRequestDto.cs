// -----------------------------------------------------------------------
// <copyright file="RefreshTokenRequestDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Validation;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

/// <summary>
/// DTO para los datos requeridos en RefreshToken.
/// </summary>
/// <param name="RefreshToken">
/// Token de renovación proporcionado durante el login. Validado por <see cref="RefreshTokenCommandValidator"/> y las reglas de <see cref="AuthValidationRules"/>.
/// Opcional cuando el refresh token viaja en cookie (<c>RefreshTokenSettings:Delivery</c>).
/// </param>
public record RefreshTokenRequestDto(
  string? RefreshToken = null)
{
  /// <summary>
  /// Conversion implícita de DTO a Command.
  /// </summary>
  public static implicit operator RefreshTokenCommand(RefreshTokenRequestDto dto)
  {
    return new RefreshTokenCommand(dto.RefreshToken ?? string.Empty);
  }
}

/// <summary>
/// Extensiones para facilitar conversiones y validaciones de RefreshTokenRequestDto.
/// </summary>
public static class RefreshTokenRequestDtoExtensions
{
  /// <summary>
  /// Convierte el DTO a Command incluyendo información de contexto HTTP.
  /// </summary>
  public static RefreshTokenCommand ToCommand(this RefreshTokenRequestDto? dto, HttpContext context, string? cookieToken = null)
  {
    var ipAddress = context.Connection.RemoteIpAddress?.ToString();
    var userAgent = context.Request.Headers.UserAgent.ToString();

    // El cuerpo manda; la cookie solo se usa si el cuerpo no trae el token.
    var refreshToken = string.IsNullOrEmpty(dto?.RefreshToken) ? cookieToken : dto.RefreshToken;

    return new RefreshTokenCommand(refreshToken ?? string.Empty, ipAddress, userAgent);
  }
}
