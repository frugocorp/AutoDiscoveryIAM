// -----------------------------------------------------------------------
// <copyright file="RefreshTokenResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

/// <summary>
/// DTO de respuesta para la operación RefreshToken.
/// </summary>
/// <param name="AccessToken">Nuevo JWT de acceso generado.</param>
/// <param name="RefreshToken">Nuevo refresh token (rotación automática). <c>null</c> cuando se entrega solo en cookie (<c>RefreshTokenSettings:Delivery = Cookie</c>).</param>
/// <param name="TokenType">Tipo de token, típicamente "Bearer".</param>
/// <param name="ExpiresIn">Tiempo en segundos hasta que expire el access token.</param>
/// <param name="ExpiresAt">Fecha y hora exacta de expiración del access token.</param>
public record RefreshTokenResponseDto(
  string AccessToken,
  string? RefreshToken,
  string TokenType,
  int ExpiresIn,
  DateTime ExpiresAt);
