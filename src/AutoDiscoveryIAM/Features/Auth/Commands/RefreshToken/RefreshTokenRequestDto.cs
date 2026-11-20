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
/// </param>
public record RefreshTokenRequestDto(
  [Required(ErrorMessage = "El refresh token es requerido")]
  string RefreshToken)
{
  /// <summary>
  /// Conversion implícita de DTO a Command.
  /// </summary>
  public static implicit operator RefreshTokenCommand(RefreshTokenRequestDto dto)
  {
    return new RefreshTokenCommand(dto.RefreshToken);
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
  public static RefreshTokenCommand ToCommand(this RefreshTokenRequestDto dto, HttpContext context)
  {
    var ipAddress = context.Connection.RemoteIpAddress?.ToString();
    var userAgent = context.Request.Headers.UserAgent.ToString();

    return new RefreshTokenCommand(dto.RefreshToken, ipAddress, userAgent);
  }
}
