using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Security;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Data.UoW;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.RefreshToken;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Logging;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Infrastructure.Validation;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;

public interface IPasswordManagementService
{
    Task<OperationResult<PasswordManagementStatusDto>> ChangePasswordAsync(Guid userPublicId, string? currentPassword, string newPassword, CancellationToken cancellationToken);
    Task<OperationResult<PasswordManagementStatusDto>> RequestResetAsync(string email, CancellationToken cancellationToken);
    Task<OperationResult<PasswordManagementStatusDto>> ConfirmResetAsync(string token, string newPassword, CancellationToken cancellationToken);
}

public sealed class PasswordManagementService(
  IPasswordManagementRepository repository,
  IPasswordHasher passwordHasher,
  IPasswordResetTokenService tokenService,
  IPasswordResetEmailService emailService,
  IRefreshTokenRepository refreshTokenRepository,
  IAutoDiscoveryIAMUnitOfWork unitOfWork,
  IOptions<PasswordResetSettings> resetOptions,
  IUserLogger logger) : IPasswordManagementService
{
  private readonly IPasswordManagementRepository _repository = repository;
  private readonly IPasswordHasher _passwordHasher = passwordHasher;
  private readonly IPasswordResetTokenService _tokenService = tokenService;
  private readonly IPasswordResetEmailService _emailService = emailService;
  private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
  private readonly IAutoDiscoveryIAMUnitOfWork _unitOfWork = unitOfWork;
  private readonly PasswordResetSettings _settings = resetOptions.Value;
  private readonly IUserLogger _logger = logger;

  public async Task<OperationResult<PasswordManagementStatusDto>> ChangePasswordAsync(Guid userPublicId, string? currentPassword, string newPassword, CancellationToken cancellationToken)
  {
    var user = await _repository.GetByPublicIdAsync(userPublicId, cancellationToken);
    if (user == null)
    {
      return OperationResult.Fail<PasswordManagementStatusDto>(ErrorInfo.Status404NotFound, "Usuario no encontrado.");
    }

    if (!string.IsNullOrWhiteSpace(user.PasswordHash))
    {
      if (string.IsNullOrWhiteSpace(currentPassword) || !_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash))
      {
        _logger.LogPasswordChanged(user.Id, false);
        return OperationResult.Fail<PasswordManagementStatusDto>(ErrorInfo.Status401Unauthorized, UserValidationTypes.CurrentPasswordInvalid.Message);
      }
    }

    user.PasswordHash = _passwordHasher.HashPassword(newPassword);
    // Invalida de inmediato cualquier access token ya emitido (en cualquier dispositivo), no solo
    // los refresh tokens: cualquier JWT con un tvf anterior a este valor deja de ser válido.
    user.TokenValidFrom = TimeHelpers.GetCurrentMexicoTime();

    await _refreshTokenRepository.RevokeUserTokensAsync(user.Id, "password_changed", cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    _logger.LogPasswordChanged(user.Id, true);

    return OperationResult.Ok(new PasswordManagementStatusDto("Contraseña actualizada correctamente."));
  }

  /// <summary>
  /// Respuesta neutra: igual exista o no la cuenta, y con una duración mínima
  /// (<see cref="PasswordResetSettings.MinimumResponseMilliseconds"/>) en todos los casos, para que el
  /// tiempo de respuesta tampoco delate si se envió un correo.
  /// </summary>
  public async Task<OperationResult<PasswordManagementStatusDto>> RequestResetAsync(string email, CancellationToken cancellationToken)
  {
    var startTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
    var result = await RequestResetCoreAsync(email, cancellationToken);
    await NeutralResponseTiming.PadAsync(startTimestamp, _settings.MinimumResponseMilliseconds, cancellationToken);
    return result;
  }

  private async Task<OperationResult<PasswordManagementStatusDto>> RequestResetCoreAsync(string email, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(email))
    {
      return OperationResult.Ok(NeutralResetResponse());
    }

    var user = await _repository.GetByEmailAsync(email.Trim(), cancellationToken);
    if (user == null || user.IsDeleted || !user.IsActive)
    {
      _logger.LogPasswordResetRequested(email, false);
      return OperationResult.Ok(NeutralResetResponse());
    }

    var now = TimeHelpers.GetCurrentMexicoTime();
    // En la base de datos solo queda el hash: el token en claro existe únicamente en el correo.
    var resetToken = _tokenService.GenerateToken();
    user.PasswordResetToken = OneTimeTokenHasher.Hash(resetToken);
    user.PasswordResetTokenExpiration = now.AddMinutes(Math.Max(_settings.TokenExpiryMinutes, 1));

    var sendResult = await _emailService.SendPasswordResetEmailAsync(user, resetToken, cancellationToken, fakeMail: false);
    if (!sendResult.Success)
    {
      _logger.LogPasswordResetRequested(email, false);
      return OperationResult.Ok(NeutralResetResponse());
    }

    await _unitOfWork.SaveChangesAsync(cancellationToken);
    _logger.LogPasswordResetRequested(email, true);

    return OperationResult.Ok(NeutralResetResponse());
  }

  public async Task<OperationResult<PasswordManagementStatusDto>> ConfirmResetAsync(string token, string newPassword, CancellationToken cancellationToken)
  {
    if (string.IsNullOrWhiteSpace(token))
    {
      return OperationResult.Fail<PasswordManagementStatusDto>(ErrorInfo.Status400Validation, UserValidationTypes.ResetTokenRequired.Message);
    }

    var user = await _repository.GetByResetTokenHashAsync(OneTimeTokenHasher.Hash(token), cancellationToken);
    if (user == null || !IsTokenValid(user, token))
    {
      return OperationResult.Fail<PasswordManagementStatusDto>(ErrorInfo.Status409Conflict, "Token de reseteo invalido o expirado.");
    }

    user.PasswordHash = _passwordHasher.HashPassword(newPassword);
    user.PasswordResetToken = null;
    user.PasswordResetTokenExpiration = null;
    user.FailedLoginAttempts = 0;
    user.LockoutEnd = null;
    // Invalida de inmediato cualquier access token ya emitido (en cualquier dispositivo), no solo
    // los refresh tokens: cualquier JWT con un tvf anterior a este valor deja de ser válido.
    user.TokenValidFrom = TimeHelpers.GetCurrentMexicoTime();

    await _refreshTokenRepository.RevokeUserTokensAsync(user.Id, "password_reset", cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);

    _logger.LogPasswordResetConfirmed(user.Id, true);

    return OperationResult.Ok(new PasswordManagementStatusDto("Contraseña restablecida correctamente."));
  }

  private static bool IsTokenValid(User user, string token)
  {
    if (!OneTimeTokenHasher.Matches(user.PasswordResetToken, token))
    {
      return false;
    }

    if (!user.PasswordResetTokenExpiration.HasValue)
    {
      return false;
    }

    return user.PasswordResetTokenExpiration.Value >= TimeHelpers.GetCurrentMexicoTime();
  }

  private static PasswordManagementStatusDto NeutralResetResponse()
    => new("Si el correo existe, se enviara un enlace para restablecer la contraseña.");
}
