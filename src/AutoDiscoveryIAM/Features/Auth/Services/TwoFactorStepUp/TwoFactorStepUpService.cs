// -----------------------------------------------------------------------
// <copyright file="TwoFactorStepUpService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security.SecurityEvents;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.Login;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.AuthContext;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Catalogs;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Services.TwoFactorStepUp;

/// <summary>
/// Verificación step-up: pide un código del Authenticator a un usuario que ya inició sesión, antes de
/// una acción sensible de la app consumidora (firmar, borrar, cambiar datos de pago...).
/// </summary>
/// <remarks>
/// Aplica las mismas protecciones que el segundo factor del login: un código ya usado (o uno
/// anterior) no vuelve a servir, y cada código incorrecto cuenta para el bloqueo de la cuenta
/// (<c>Lockout:MaxFailedAttempts</c>/<c>LockoutMinutes</c>, el mismo contador del login). El servicio
/// guarda en la base el paso usado y los intentos. ADIAM no expone un endpoint para esto: la
/// verificación tiene valor solo dentro del endpoint que hace la acción sensible.
/// </remarks>
public interface ITwoFactorStepUpService
{
  /// <summary>
  /// Verifica el código del Authenticator del usuario indicado.
  /// </summary>
  /// <param name="userPublicId">PublicId del usuario (claim <c>NameIdentifier</c> del JWT).</param>
  /// <param name="code">Código de 6 dígitos que capturó el usuario.</param>
  /// <param name="ipAddress">IP del cliente, para el registro de eventos de seguridad.</param>
  /// <param name="cancellationToken">Token de cancelación.</param>
  Task<TwoFactorStepUpResult> VerifyAsync(Guid userPublicId, string code, string? ipAddress, CancellationToken cancellationToken);

  /// <summary>
  /// Igual que <see cref="VerifyAsync"/>, para el usuario autenticado de la petición actual.
  /// </summary>
  Task<TwoFactorStepUpResult> VerifyCurrentUserAsync(string code, string? ipAddress, CancellationToken cancellationToken);
}

/// <summary>
/// Implementación de <see cref="ITwoFactorStepUpService"/>.
/// </summary>
public sealed class TwoFactorStepUpService(
  AutoDiscoveryIAMDbContext dbContext,
  IAutoDiscoveryIAMUnitOfWork unitOfWork,
  ITotpService totpService,
  IAuthContextService authContextService,
  IAuthLogger logger,
  ISecurityEventLogger securityEventLogger,
  IOptions<LockoutSettings> lockoutSettings) : ITwoFactorStepUpService
{
  private const string EventSource = "2fa-step-up";

  private readonly AutoDiscoveryIAMDbContext _dbContext = dbContext;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;
  private readonly ITotpService _totpService = totpService;
  private readonly IAuthContextService _authContextService = authContextService;
  private readonly IAuthLogger _logger = logger;
  private readonly ISecurityEventLogger _securityEventLogger = securityEventLogger;
  private readonly LockoutSettings _lockoutSettings = lockoutSettings.Value;

  /// <inheritdoc />
  public Task<TwoFactorStepUpResult> VerifyCurrentUserAsync(string code, string? ipAddress, CancellationToken cancellationToken)
  {
    var publicId = _authContextService.GetCurrentUserPublicId();
    return publicId is { } id
      ? VerifyAsync(id, code, ipAddress, cancellationToken)
      : Task.FromResult(new TwoFactorStepUpResult(TwoFactorStepUpStatus.UserNotFound));
  }

  /// <inheritdoc />
  public async Task<TwoFactorStepUpResult> VerifyAsync(Guid userPublicId, string code, string? ipAddress, CancellationToken cancellationToken)
  {
    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.PublicId == userPublicId && u.IsActive, cancellationToken);
    if (user == null)
    {
      return new TwoFactorStepUpResult(TwoFactorStepUpStatus.UserNotFound);
    }

    if (user.LockoutEnd is { } lockoutEnd && lockoutEnd > TimeHelpers.GetCurrentMexicoTime())
    {
      _logger.LogUserBlocked(user.Email);
      _securityEventLogger.LogSecurityEvent(SecurityEventType.AccountLockedOut, user.Email, ipAddress, EventSource, "Verificacion step-up contra cuenta ya bloqueada.");
      return new TwoFactorStepUpResult(TwoFactorStepUpStatus.LockedOut, lockoutEnd);
    }

    if (!user.TwoFactorEnabled || user.TwoFactorMethod != TwoFactorMethodCatalog.Authenticator || string.IsNullOrEmpty(user.TwoFactorSecret))
    {
      return new TwoFactorStepUpResult(TwoFactorStepUpStatus.NotEnrolled);
    }

    var timeStep = _totpService.ValidateCode(user.TwoFactorSecret, code, user.TwoFactorLastUsedTimeStep);
    if (timeStep is null)
    {
      return await RegisterFailedAttemptAsync(user, ipAddress, cancellationToken);
    }

    // Igual que en el login: el paso queda guardado y el mismo código no vuelve a servir. Dos
    // verificaciones simultáneas con el mismo código chocan en el RowVersion del usuario.
    user.TwoFactorLastUsedTimeStep = timeStep;
    user.FailedLoginAttempts = 0;
    try
    {
      await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
      await _dbContext.Entry(user).ReloadAsync(cancellationToken);
      _securityEventLogger.LogSecurityEvent(SecurityEventType.TwoFactorFailed, user.Email, ipAddress, EventSource, "Codigo step-up usado al mismo tiempo por otra peticion.");
      return new TwoFactorStepUpResult(TwoFactorStepUpStatus.InvalidCode);
    }

    return new TwoFactorStepUpResult(TwoFactorStepUpStatus.Verified);
  }

  private async Task<TwoFactorStepUpResult> RegisterFailedAttemptAsync(User user, string? ipAddress, CancellationToken cancellationToken)
  {
    _securityEventLogger.LogSecurityEvent(SecurityEventType.TwoFactorFailed, user.Email, ipAddress, EventSource, "Codigo step-up invalido o ya usado.");
    var lockedOut = await FailedLoginAttemptRecorder.RecordAsync(_dbContext, _unitOfWork, user, _lockoutSettings, cancellationToken);
    if (!lockedOut)
    {
      return new TwoFactorStepUpResult(TwoFactorStepUpStatus.InvalidCode);
    }

    _logger.LogUserBlocked(user.Email);
    _securityEventLogger.LogSecurityEvent(SecurityEventType.AccountLockedOut, user.Email, ipAddress, EventSource, $"Cuenta bloqueada tras {_lockoutSettings.MaxFailedAttempts} intentos fallidos de verificacion step-up.");
    return new TwoFactorStepUpResult(TwoFactorStepUpStatus.LockedOut, user.LockoutEnd);
  }
}
