// -----------------------------------------------------------------------
// <copyright file="LogoutResponseDto.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;

/// <summary>
/// DTO de respuesta para la operación Logout.
/// </summary>
/// <param name="Message">Mensaje de confirmación de logout.</param>
/// <param name="TokensRevoked">Número de tokens revocados.</param>
/// <param name="LoggedOutAt">Fecha y hora del logout.</param>
public record LogoutResponseDto(
  string Message,
  int TokensRevoked,
  DateTime LoggedOutAt);
