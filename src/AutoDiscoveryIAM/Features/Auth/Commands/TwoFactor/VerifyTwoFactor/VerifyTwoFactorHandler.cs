// -----------------------------------------------------------------------
// <copyright file="VerifyTwoFactorHandler.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor.VerifyTwoFactor;

/// <summary>
/// Completa el login validando el segundo factor de un desafío pendiente iniciado por <c>LoginHandler</c>.
/// </summary>
public class VerifyTwoFactorHandler(
  AutoDiscoveryIAMDbContext dbContext,
  ITotpService totpService,
  IJwtClaimsFactory jwtClaimsFactory,
  IRefreshTokenService refreshTokenService,
  IRefreshTokenRepository refreshTokenRepository,
  IAutoDiscoveryIAMUnitOfWork unitOfWork,
  IAuthLogger logger,
  ISecurityEventLogger securityEventLogger,
  IOptions<LockoutSettings> lockoutSettings)
{
  private readonly AutoDiscoveryIAMDbContext _dbContext = dbContext;
  private readonly ITotpService _totpService = totpService;
  private readonly IJwtClaimsFactory _jwtClaimsFactory = jwtClaimsFactory;
  private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
  private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;
  private readonly IAuthLogger _logger = logger;
  private readonly ISecurityEventLogger _securityEventLogger = securityEventLogger;
  private readonly LockoutSettings _lockoutSettings = lockoutSettings.Value;

  public async Task<OperationResult<LoginResponseDto>> Handle(VerifyTwoFactorCommand command, CancellationToken cancellationToken)
  {
    var user = await GetUserByChallengeTokenAsync(command.ChallengeToken, cancellationToken);
    if (user == null)
    {
      _securityEventLogger.LogSecurityEvent(SecurityEventType.TwoFactorFailed, null, command.IpAddress, "/auth/2fa/verify", "Token de desafío inválido o expirado.");
      return OperationResult.Fail<LoginResponseDto>(ErrorInfo.Status409Conflict, "Token de desafio invalido o expirado.");
    }

    var now = TimeHelpers.GetCurrentMexicoTime();
    if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > now)
    {
      _logger.LogUserBlocked(user.Email);
      _securityEventLogger.LogSecurityEvent(SecurityEventType.AccountLockedOut, user.Email, command.IpAddress, "/auth/2fa/verify", "Intento de verificacion 2FA contra cuenta ya bloqueada.");
      return OperationResult.Fail<LoginResponseDto>(
        ErrorInfo.AccountLockedOut,
        "Cuenta bloqueada temporalmente por multiples intentos fallidos. Intenta de nuevo mas tarde o restablece tu contrasena.");
    }

    if (!IsCodeValid(user, command.Code))
    {
      await RegisterFailedAttemptAsync(user, command.IpAddress, cancellationToken);
      _logger.LogLoginFailed(user.Email);
      _securityEventLogger.LogSecurityEvent(SecurityEventType.TwoFactorFailed, user.Email, command.IpAddress, "/auth/2fa/verify", "Código de verificación inválido.");
      return OperationResult.Fail<LoginResponseDto>(ErrorInfo.Status401Unauthorized, "Codigo de verificacion invalido.");
    }

    ResetLockoutState(user);
    ClearChallenge(user);
    var loginResponse = await GenerateJwtTokenAsync(user, command, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    _logger.LogLoginSuccess(user.Email);
    _logger.LogTokenGenerated(user.Email);

    return OperationResult.Ok(loginResponse);
  }

  private async Task RegisterFailedAttemptAsync(User user, string? ipAddress, CancellationToken cancellationToken)
  {
    var lockedOut = await FailedLoginAttemptRecorder.RecordAsync(_dbContext, _unitOfWork, user, _lockoutSettings, cancellationToken);
    if (lockedOut)
    {
      _logger.LogUserBlocked(user.Email);
      _securityEventLogger.LogSecurityEvent(SecurityEventType.AccountLockedOut, user.Email, ipAddress, "/auth/2fa/verify", $"Cuenta bloqueada tras {_lockoutSettings.MaxFailedAttempts} intentos fallidos de verificacion 2FA.");
    }
  }

  private static void ResetLockoutState(User user)
  {
    user.FailedLoginAttempts = 0;
    user.LockoutEnd = null;
  }

  private async Task<User?> GetUserByChallengeTokenAsync(string challengeToken, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(challengeToken))
    {
      return null;
    }

    var now = TimeHelpers.GetCurrentMexicoTime();
    var challengeTokenHash = OneTimeTokenHasher.Hash(challengeToken);

    return await _dbContext.Users
      .Include(u => u.UserRoles)
      .ThenInclude(ur => ur.Role)
      .FirstOrDefaultAsync(
        u => u.TwoFactorChallengeToken == challengeTokenHash
          && u.IsActive
          && u.TwoFactorChallengeTokenExpiration.HasValue
          && u.TwoFactorChallengeTokenExpiration.Value >= now,
        cancellationToken);
  }

  private bool IsCodeValid(User user, string code)
  {
    if (user.TwoFactorMethod == TwoFactorMethodCatalog.Authenticator)
    {
      var timeStep = _totpService.ValidateCode(user.TwoFactorSecret, code, user.TwoFactorLastUsedTimeStep);
      if (timeStep is null)
      {
        return false;
      }

      // Se guarda junto con el resto del login exitoso: el mismo código no vuelve a servir. Dos
      // verificaciones simultáneas con el mismo código chocan en el RowVersion del usuario.
      user.TwoFactorLastUsedTimeStep = timeStep;
      return true;
    }

    if (user.TwoFactorMethod == TwoFactorMethodCatalog.Email)
    {
      var now = TimeHelpers.GetCurrentMexicoTime();
      return !string.IsNullOrWhiteSpace(user.TwoFactorCode)
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(user.TwoFactorCode), Encoding.UTF8.GetBytes(code ?? string.Empty))
        && user.TwoFactorCodeExpiration.HasValue
        && user.TwoFactorCodeExpiration.Value >= now;
    }

    return false;
  }

  private static void ClearChallenge(User user)
  {
    user.TwoFactorChallengeToken = null;
    user.TwoFactorChallengeTokenExpiration = null;
    user.TwoFactorCode = null;
    user.TwoFactorCodeExpiration = null;
  }

  private async Task<LoginResponseDto> GenerateJwtTokenAsync(User user, VerifyTwoFactorCommand command, CancellationToken cancellationToken)
  {
    var (tokenString, expiresAt) = _jwtClaimsFactory.BuildAccessToken(user);

    var refreshTokenEntity = _refreshTokenService.GenerateRefreshToken(user, command.IpAddress, command.UserAgent);
    await _refreshTokenRepository.CreateAsync(refreshTokenEntity, cancellationToken);

    return new LoginResponseDto(tokenString, expiresAt, refreshTokenEntity.PlaintextToken!, PasswordChangeRequired: user.MustChangePassword);
  }
}
