// -----------------------------------------------------------------------
// <copyright file="LogoutHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Logout;

/// <summary>
/// Handler para el logout e invalidación de refresh tokens.
/// </summary>
public class LogoutHandler(
  IRefreshTokenRepository refreshTokenRepository,
  IAuthLogger logger,
  IJwtBlacklistService jwtBlacklistService)
{
  private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
  private readonly IAuthLogger _logger = logger;
  private readonly IJwtBlacklistService _jwtBlacklistService = jwtBlacklistService;

  /// <summary>
  /// Maneja el proceso de logout invalidando tokens.
  /// </summary>
  public async Task<OperationResult<LogoutResponseDto>> Handle(LogoutCommand command, CancellationToken cancellationToken)
  {
    var (tokensRevoked, userEmail) = await ProcessRevocationAsync(command, cancellationToken);

    // Invalida también el access token vigente: sin esto, seguiría siendo válido hasta expirar
    // por sí solo aunque el usuario ya haya cerrado sesión.
    if (!string.IsNullOrEmpty(command.AccessTokenJti) && command.AccessTokenExpiresAtUtc.HasValue)
    {
      await _jwtBlacklistService.BlacklistAsync(command.AccessTokenJti, command.AccessTokenExpiresAtUtc.Value, cancellationToken);
    }

    var loggedOutAt = TimeHelpers.GetCurrentMexicoTime();
    var message = tokensRevoked > 0
      ? $"Logout exitoso. {tokensRevoked} token(s) invalidado(s)."
      : "Logout completado.";

    var responseDto = new LogoutResponseDto(message, tokensRevoked, loggedOutAt);

    _logger.LogLogout(userEmail);
    return OperationResult.Ok(responseDto);
  }

  private async Task<(int tokensRevoked, string userEmail)> ProcessRevocationAsync(LogoutCommand command, CancellationToken cancellationToken)
  {
    int tokensRevoked = 0;
    string userEmail = "unknown";

    // Si se proporciona un refresh token específico
    if (!string.IsNullOrEmpty(command.RefreshToken))
    {
      var token = await _refreshTokenRepository.GetByTokenAsync(command.RefreshToken, cancellationToken);

      // Solo se revoca un refresh token del propio usuario autenticado: con uno ajeno (y
      // RevokeAllTokens) se podría cerrar la sesión de otra persona. Se responde igual que si no
      // existiera, para no confirmar de quién es.
      if (token != null && !token.IsRevoked && token.UserId == command.UserId)
      {
        token.IsRevoked = true;
        token.RevokedAt = TimeHelpers.GetCurrentMexicoTime();
        token.RevocationReason = "User logout";

        await _refreshTokenRepository.UpdateAsync(token, cancellationToken);
        tokensRevoked++;
        userEmail = token.User.Email;

        // Si se solicita revocar todos los tokens del usuario
        if (command.RevokeAllTokens)
        {
          await _refreshTokenRepository.RevokeUserTokensAsync(token.UserId, "User logout - all tokens", cancellationToken);
          // Note: RevokeUserTokensAsync ya maneja tokens activos, pero el token específico ya fue revocado arriba
        }
      }
    }
    // Si solo se tiene UserId (desde JWT) y se quiere revocar todos
    else if (command.UserId.HasValue && command.RevokeAllTokens)
    {
      await _refreshTokenRepository.RevokeUserTokensAsync(command.UserId.Value, "User logout - all tokens", cancellationToken);
      tokensRevoked = 1; // Estimado, no tenemos el conteo exacto
      userEmail = "authenticated_user";
    }

    return (tokensRevoked, userEmail);
  }
}
