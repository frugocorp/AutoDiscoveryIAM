// -----------------------------------------------------------------------
// <copyright file="LogoutCommand.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;

/// <summary>
/// Comando para realizar logout e invalidar refresh tokens.
/// </summary>
/// <param name="RefreshToken">Token de renovación a invalidar.</param>
/// <param name="UserId">ID del usuario (opcional, extraído del JWT).</param>
/// <param name="RevokeAllTokens">Si debe revocar todos los tokens del usuario.</param>
/// <param name="IpAddress">Dirección IP del cliente para auditoría.</param>
/// <param name="UserAgent">User-Agent del cliente para auditoría.</param>
/// <param name="AccessTokenJti">Claim <c>jti</c> del access token con el que se autenticó esta petición, para invalidarlo antes de su expiración natural.</param>
/// <param name="AccessTokenExpiresAtUtc">Expiración natural (UTC) del access token, tomada de su claim <c>exp</c>.</param>
public record LogoutCommand(
  string? RefreshToken = null,
  long? UserId = null,
  bool RevokeAllTokens = false,
  string? IpAddress = null,
  string? UserAgent = null,
  string? AccessTokenJti = null,
  DateTime? AccessTokenExpiresAtUtc = null);
