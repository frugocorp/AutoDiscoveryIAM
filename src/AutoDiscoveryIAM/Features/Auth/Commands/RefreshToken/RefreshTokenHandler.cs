// -----------------------------------------------------------------------
// <copyright file="RefreshTokenHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities;
using RefreshTokenEntity = FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Models.Entities.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Manejador del comando RefreshToken para la renovación de tokens de acceso.
/// Implementa rotación automática de refresh tokens por seguridad.
/// </summary>
/// <param name="refreshTokenRepository">Repositorio de tokens de renovación.</param>
/// <param name="refreshTokenService">Servicio para operaciones con refresh tokens.</param>
/// <param name="logger">Logger para auditoría de autenticación.</param>
/// <param name="jwtClaimsFactory">Servicio único de generación de claims/access token JWT.</param>
/// <param name="jwtSettings">Configuración JWT del sistema.</param>
/// <param name="securityEventLogger">Registro centralizado de eventos de seguridad (reuso de refresh token, etc.).</param>
public class RefreshTokenHandler(
  IRefreshTokenRepository refreshTokenRepository,
  IRefreshTokenService refreshTokenService,
  IAuthLogger logger,
  IJwtClaimsFactory jwtClaimsFactory,
  IOptions<JwtSettings> jwtSettings,
  ISecurityEventLogger securityEventLogger)
{
  private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
  private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
  private readonly IAuthLogger _logger = logger;
  private readonly IJwtClaimsFactory _jwtClaimsFactory = jwtClaimsFactory;
  private readonly JwtSettings _jwtSettings = jwtSettings.Value;
  private readonly ISecurityEventLogger _securityEventLogger = securityEventLogger;

  /// <summary>
  /// Maneja el proceso de renovación de token con rotación automática.
  /// </summary>
  public async Task<OperationResult<RefreshTokenResponseDto>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
  {
    var validationResult = await ValidateRefreshTokenAsync(command, cancellationToken);
    if (!validationResult.Success)
      return OperationResult.Fail<RefreshTokenResponseDto>(validationResult.ErrorInfo ?? ErrorInfo.Status401Unauthorized, validationResult.Error ?? "Token de renovación inválido");

    var storedToken = validationResult.Data!;

    RefreshTokenResponseDto tokens;
    try
    {
      tokens = await RotateTokensAsync(storedToken, command, cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
      // Otra petición concurrente ya rotó este mismo token entre la validación y el guardado
      // — no necesariamente un ataque de reuso (pudo ser un doble-submit legítimo),
      // por eso no se revoca toda la sesión aquí como sí ocurre en el caso de reuso ya confirmado.
      _logger.LogLoginFailed(storedToken.User.Email);
      _securityEventLogger.LogSecurityEvent(
        SecurityEventType.RefreshTokenReuseDetected,
        storedToken.User.Email,
        command.IpAddress,
        "/auth/refresh",
        "Conflicto de concurrencia al rotar el refresh token: otra peticion simultanea con el mismo token ya lo roto primero.");
      return OperationResult.Fail<RefreshTokenResponseDto>(
        ErrorInfo.Status401Unauthorized,
        "Token de renovación inválido.");
    }

    _logger.LogLoginSuccess(storedToken.User.Email);

    return OperationResult.Ok(tokens);
  }

  private async Task<OperationResult<RefreshTokenEntity>> ValidateRefreshTokenAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
  {
    var storedToken = await _refreshTokenRepository.GetByTokenAsync(command.RefreshToken, cancellationToken);

    if (storedToken == null)
    {
      _logger.LogLoginFailed("unknown");
      return OperationResult.Fail<RefreshTokenEntity>(
        ErrorInfo.Status401Unauthorized,
        "Token de renovación inválido.");
    }

    if (storedToken.IsRevoked)
    {
      // Un token ya revocado (rotado o invalidado) que vuelve a usarse es la señal clásica de
      // reuso: alguien más lo tiene. Se corta toda la sesión del usuario, no solo esta petición.
      await _refreshTokenRepository.RevokeUserTokensAsync(storedToken.UserId, "refresh_token_reuse_detected", cancellationToken);
      _logger.LogLoginFailed(storedToken.User.Email);
      _securityEventLogger.LogSecurityEvent(
        SecurityEventType.RefreshTokenReuseDetected,
        storedToken.User.Email,
        command.IpAddress,
        "/auth/refresh",
        $"Reuso de refresh token revocado (razón original: {storedToken.RevocationReason ?? "desconocida"}). Se revocó toda la sesión del usuario {storedToken.UserId}.");
      return OperationResult.Fail<RefreshTokenEntity>(
        ErrorInfo.Status401Unauthorized,
        "Token de renovación inválido.");
    }

    if (!_refreshTokenService.ValidateRefreshToken(storedToken))
    {
      _logger.LogLoginFailed(storedToken.User.Email);
      return OperationResult.Fail<RefreshTokenEntity>(
        ErrorInfo.Status401Unauthorized,
        "Token de renovación expirado.");
    }

    if (!storedToken.User.IsActive)
    {
      _logger.LogLoginFailed(storedToken.User.Email);
      return OperationResult.Fail<RefreshTokenEntity>(
        ErrorInfo.Status401Unauthorized,
        "Usuario inactivo.");
    }

    return OperationResult.Ok(storedToken);
  }

  private async Task<RefreshTokenResponseDto> RotateTokensAsync(RefreshTokenEntity storedToken, RefreshTokenCommand command, CancellationToken cancellationToken)
  {
    // Revocar token actual
    storedToken.IsRevoked = true;
    storedToken.RevokedAt = TimeHelpers.GetCurrentMexicoTime();
    storedToken.RevocationReason = "Token rotated";

    // Generar nuevo refresh token
    var newRefreshToken = _refreshTokenService.GenerateRefreshToken(
      storedToken.User,
      command.IpAddress,
      command.UserAgent);

    // Enlazar tokens para trazabilidad (se enlaza por hash, igual que la columna Token)
    storedToken.ReplacedByToken = newRefreshToken.Token;

    // Guardar cambios
    await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);
    await _refreshTokenRepository.CreateAsync(newRefreshToken, cancellationToken);

    // Generar nuevo JWT
    var (token, expiresAt) = _jwtClaimsFactory.BuildAccessToken(storedToken.User);

    return new RefreshTokenResponseDto(
      token,
      newRefreshToken.PlaintextToken!,
      "Bearer",
      _jwtSettings.ExpiryMinutes * 60,
      expiresAt);
  }
}
