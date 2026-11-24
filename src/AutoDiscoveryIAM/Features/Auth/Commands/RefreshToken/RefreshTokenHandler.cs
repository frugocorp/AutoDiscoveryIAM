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
/// <param name="graceStore">Par de tokens de la última rotación, para la ventana de gracia.</param>
/// <param name="refreshTokenSettings">Configuración de refresh tokens (ventana de gracia).</param>
public class RefreshTokenHandler(
  IRefreshTokenRepository refreshTokenRepository,
  IRefreshTokenService refreshTokenService,
  IAuthLogger logger,
  IJwtClaimsFactory jwtClaimsFactory,
  IOptions<JwtSettings> jwtSettings,
  ISecurityEventLogger securityEventLogger,
  IRefreshTokenGraceStore graceStore,
  IOptions<RefreshTokenSettings> refreshTokenSettings)
{
  /// <summary>
  /// Razón con la que se revoca un token al rotarlo.
  /// </summary>
  internal const string RotatedReason = "Token rotated";

  private const string InvalidTokenMessage = "Token de renovación inválido.";

  private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
  private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
  private readonly IAuthLogger _logger = logger;
  private readonly IJwtClaimsFactory _jwtClaimsFactory = jwtClaimsFactory;
  private readonly JwtSettings _jwtSettings = jwtSettings.Value;
  private readonly ISecurityEventLogger _securityEventLogger = securityEventLogger;
  private readonly IRefreshTokenGraceStore _graceStore = graceStore;
  private readonly TimeSpan _gracePeriod = TimeSpan.FromSeconds(Math.Clamp(refreshTokenSettings.Value.ReuseGracePeriodSeconds, 0, RefreshTokenSettings.MaxReuseGracePeriodSeconds));

  /// <summary>
  /// Maneja el proceso de renovación de token con rotación automática.
  /// </summary>
  public async Task<OperationResult<RefreshTokenResponseDto>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
  {
    var storedToken = await _refreshTokenRepository.GetByTokenAsync(command.RefreshToken, cancellationToken);

    if (storedToken is { IsRevoked: true } && IsWithinGracePeriod(storedToken))
    {
      return await ResolveRecentRotationAsync(storedToken, command, cancellationToken);
    }

    var validationResult = await ValidateRefreshTokenAsync(storedToken, command, cancellationToken);
    if (!validationResult.Success)
      return OperationResult.Fail<RefreshTokenResponseDto>(validationResult.ErrorInfo ?? ErrorInfo.Status401Unauthorized, validationResult.Error ?? InvalidTokenMessage);

    storedToken = validationResult.Data!;

    RefreshTokenResponseDto tokens;
    try
    {
      tokens = await RotateTokensAsync(storedToken, command, cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
      return await HandleConcurrentRotationAsync(storedToken, command, cancellationToken);
    }

    _logger.LogLoginSuccess(storedToken.User.Email);

    return OperationResult.Ok(tokens);
  }

  /// <summary>
  /// Otra petición con el mismo token lo rotó entre la validación y el guardado: no necesariamente un
  /// ataque (pudo ser un doble envío legítimo), por eso no se revoca toda la sesión. Con ventana de
  /// gracia, se devuelve el par que entregó esa otra petición.
  /// </summary>
  private async Task<OperationResult<RefreshTokenResponseDto>> HandleConcurrentRotationAsync(RefreshTokenEntity storedToken, RefreshTokenCommand command, CancellationToken cancellationToken)
  {
    if (_gracePeriod > TimeSpan.Zero)
    {
      await _refreshTokenRepository.ReloadAsync(storedToken, cancellationToken);
      if (storedToken.IsRevoked && IsWithinGracePeriod(storedToken))
      {
        return await ResolveRecentRotationAsync(storedToken, command, cancellationToken);
      }
    }

    _logger.LogLoginFailed(storedToken.User.Email);
    _securityEventLogger.LogSecurityEvent(
      SecurityEventType.RefreshTokenReuseDetected,
      storedToken.User.Email,
      command.IpAddress,
      "/auth/refresh",
      "Conflicto de concurrencia al rotar el refresh token: otra peticion simultanea con el mismo token ya lo roto primero.");
    return OperationResult.Fail<RefreshTokenResponseDto>(ErrorInfo.Status401Unauthorized, InvalidTokenMessage);
  }

  /// <summary>
  /// Un token rotado hace menos de la ventana de gracia: el cliente reintenta o lanzó dos refresh a
  /// la vez. Se devuelve el mismo par que entregó la rotación si el token que lo sustituyó sigue
  /// vigente y la petición viene del mismo cliente que hizo la rotación (misma IP y mismo
  /// User-Agent). Si viene de otro cliente, alguien más tiene el token: se trata como reuso y se
  /// revoca toda la sesión. En cualquier otro caso, 401 sin revocar.
  /// </summary>
  private async Task<OperationResult<RefreshTokenResponseDto>> ResolveRecentRotationAsync(RefreshTokenEntity storedToken, RefreshTokenCommand command, CancellationToken cancellationToken)
  {
    if (storedToken.User.IsActive && storedToken.ReplacedByToken != null)
    {
      var successor = await _refreshTokenRepository.GetByHashAsync(storedToken.ReplacedByToken, cancellationToken);
      if (successor is { IsActive: true } && !IsSameClient(successor, command))
      {
        await RevokeSessionForReuseAsync(
          storedToken,
          command,
          $"Reuso de refresh token recien rotado desde otro cliente (IP o User-Agent distintos a los de la rotacion), dentro de la ventana de gracia. Se revoco toda la sesion del usuario {storedToken.UserId}.",
          cancellationToken);
        return OperationResult.Fail<RefreshTokenResponseDto>(ErrorInfo.Status401Unauthorized, InvalidTokenMessage);
      }

      if (successor is { IsActive: true })
      {
        var cached = await _graceStore.GetAsync(storedToken.ReplacedByToken, cancellationToken);
        if (cached != null)
        {
          _logger.LogLoginSuccess(storedToken.User.Email);
          return OperationResult.Ok(cached);
        }
      }
    }

    _logger.LogLoginFailed(storedToken.User.Email);
    return OperationResult.Fail<RefreshTokenResponseDto>(ErrorInfo.Status401Unauthorized, InvalidTokenMessage);
  }

  /// <summary>
  /// La rotación guardó en el token nuevo la IP y el User-Agent de quien la hizo: un reintento
  /// legítimo llega desde el mismo cliente.
  /// </summary>
  private static bool IsSameClient(RefreshTokenEntity successor, RefreshTokenCommand command)
  {
    return string.Equals(successor.IpAddress, command.IpAddress, StringComparison.Ordinal)
      && string.Equals(successor.UserAgent ?? string.Empty, command.UserAgent ?? string.Empty, StringComparison.Ordinal);
  }

  /// <summary>
  /// Reuso de un token revocado: alguien más lo tiene. Se corta toda la sesión del usuario, no solo
  /// esta petición, y queda el evento de seguridad.
  /// </summary>
  private async Task RevokeSessionForReuseAsync(RefreshTokenEntity storedToken, RefreshTokenCommand command, string detail, CancellationToken cancellationToken)
  {
    await _refreshTokenRepository.RevokeUserTokensAsync(storedToken.UserId, "refresh_token_reuse_detected", cancellationToken);
    _logger.LogLoginFailed(storedToken.User.Email);
    _securityEventLogger.LogSecurityEvent(
      SecurityEventType.RefreshTokenReuseDetected,
      storedToken.User.Email,
      command.IpAddress,
      "/auth/refresh",
      detail);
  }

  private bool IsWithinGracePeriod(RefreshTokenEntity token)
  {
    return _gracePeriod > TimeSpan.Zero
      && string.Equals(token.RevocationReason, RotatedReason, StringComparison.Ordinal)
      && token.RevokedAt is { } revokedAt
      && TimeHelpers.GetCurrentMexicoTime() - revokedAt <= _gracePeriod;
  }

  private async Task<OperationResult<RefreshTokenEntity>> ValidateRefreshTokenAsync(RefreshTokenEntity? storedToken, RefreshTokenCommand command, CancellationToken cancellationToken)
  {
    if (storedToken == null)
    {
      _logger.LogLoginFailed("unknown");
      return OperationResult.Fail<RefreshTokenEntity>(
        ErrorInfo.Status401Unauthorized,
        InvalidTokenMessage);
    }

    if (storedToken.IsRevoked)
    {
      // Un token ya revocado (rotado o invalidado) que vuelve a usarse fuera de la ventana de gracia
      // es la señal clásica de reuso.
      await RevokeSessionForReuseAsync(
        storedToken,
        command,
        $"Reuso de refresh token revocado (razón original: {storedToken.RevocationReason ?? "desconocida"}). Se revocó toda la sesión del usuario {storedToken.UserId}.",
        cancellationToken);
      return OperationResult.Fail<RefreshTokenEntity>(
        ErrorInfo.Status401Unauthorized,
        InvalidTokenMessage);
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
    storedToken.RevocationReason = RotatedReason;

    // Generar nuevo refresh token
    var newRefreshToken = _refreshTokenService.GenerateRefreshToken(
      storedToken.User,
      command.IpAddress,
      command.UserAgent);

    // Enlazar tokens para trazabilidad (se enlaza por hash, igual que la columna Token)
    storedToken.ReplacedByToken = newRefreshToken.Token;

    // Generar nuevo JWT
    var (token, expiresAt) = _jwtClaimsFactory.BuildAccessToken(storedToken.User);
    var response = new RefreshTokenResponseDto(
      token,
      newRefreshToken.PlaintextToken!,
      "Bearer",
      _jwtSettings.ExpiryMinutes * 60,
      expiresAt);

    if (_gracePeriod > TimeSpan.Zero)
    {
      // Se guarda antes de la base: una petición simultánea que vea el token ya rotado encuentra el
      // par. Se identifica por el token nuevo, así el par de una rotación que pierde la carrera nunca
      // se entrega (la base enlaza solo el token de la que ganó).
      await _graceStore.StoreAsync(newRefreshToken.Token, response, _gracePeriod, cancellationToken);
    }

    // Guardar cambios
    await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);
    await _refreshTokenRepository.CreateAsync(newRefreshToken, cancellationToken);

    return response;
  }
}
