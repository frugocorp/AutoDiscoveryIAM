// -----------------------------------------------------------------------
// <copyright file="RefreshTokenCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Comando para renovar un JWT usando un refresh token.
/// </summary>
/// <param name="RefreshToken">Token de renovación proporcionado por el cliente.</param>
/// <param name="IpAddress">Dirección IP del cliente para trazabilidad.</param>
/// <param name="UserAgent">User-Agent del cliente para trazabilidad.</param>
public record RefreshTokenCommand(
  string RefreshToken,
  string? IpAddress = null,
  string? UserAgent = null);
